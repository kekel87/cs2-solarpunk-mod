using System.Collections.Generic;
using Colossal.Entities;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SolarpunkMod.Hub
{
    /// <summary>
    /// Shops of a district holding a Local Hub buy from the hub while it has the goods. The game would
    /// not take a cargo station as a shop's seller (ResourceBuyerSystem only accepts renters), so this
    /// system makes the sale itself, the way ResourceBuyerSystem does with a storage seller, before the
    /// game starts looking for a seller: the shop pays, then fetches the goods with a real vehicle.
    /// Near the hub, small vehicles replace the game's truck: closest, the hub sends the goods on cargo
    /// bikes, several to an order if needed (what they cannot carry is not sold: the shop orders it
    /// again later, smaller and more often); a bit farther, the shop's own van fetches them.
    /// Anything else (no hub, goods out of stock) is left to the game.
    /// </summary>
    public partial class HubSaleSystem : GameSystemBase
    {
        private const int TripPriority = 128;

        private LocalHubBuildingSystem m_BuildingSystem;
        private ResourceSystem m_ResourceSystem;
        private SimulationSystem m_SimulationSystem;
        private HubDeliveryVehicles m_DeliveryVehicles;
        private EntityQuery m_HubQuery;
        private EntityQuery m_BuyerQuery;

        private ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckLookup;
        private BufferLookup<GuestVehicle> m_GuestVehicleLookup;
        private BufferLookup<LayoutElement> m_LayoutElementLookup;
        private ComponentLookup<ResourceData> m_ResourceDataLookup;
        private ComponentLookup<PrefabRef> m_PrefabRefLookup;
        private ComponentLookup<StorageCompanyData> m_StorageCompanyDataLookup;

        private readonly Dictionary<Entity, List<Entity>> m_HubsByDistrict = new Dictionary<Entity, List<Entity>>();
        // Goods already sold this pass, not yet loaded on a vehicle the game can count.
        private readonly Dictionary<(Entity hub, Resource resource), int> m_SoldThisPass = new Dictionary<(Entity, Resource), int>();
        private readonly List<(DeliveryTruckSelectItem bike, int load)> m_CargoBikePlan = new List<(DeliveryTruckSelectItem, int)>();
        private int m_SalesSinceLog;
        private int m_SmallVehicleSalesSinceLog;

        public HubDeliveryVehicles DeliveryVehicles => m_DeliveryVehicles;

        // Ordered just before ResourceBuyerSystem, it inherits its pass: it sees every new buyer the game would.
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_BuildingSystem = World.GetOrCreateSystemManaged<LocalHubBuildingSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_DeliveryVehicles = new HubDeliveryVehicles(this);
            m_HubQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            // Shops that just decided to buy: the game has not started looking for a seller yet.
            m_BuyerQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ResourceBuyer>(),
                    ComponentType.ReadOnly<TripNeeded>(),
                    ComponentType.ReadOnly<ServiceAvailable>(),
                    ComponentType.ReadOnly<BuyingCompany>(),
                    ComponentType.ReadOnly<PropertyRenter>(),
                    ComponentType.ReadOnly<Game.Economy.Resources>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<PathInformation>(),
                    ComponentType.ReadOnly<TravelPurpose>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                },
            });
            m_DeliveryTruckLookup = GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
            m_GuestVehicleLookup = GetBufferLookup<GuestVehicle>(true);
            m_LayoutElementLookup = GetBufferLookup<LayoutElement>(true);
            m_ResourceDataLookup = GetComponentLookup<ResourceData>(true);
            m_PrefabRefLookup = GetComponentLookup<PrefabRef>(true);
            m_StorageCompanyDataLookup = GetComponentLookup<StorageCompanyData>(true);
            RequireForUpdate(m_HubQuery);
            RequireForUpdate(m_BuyerQuery);
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode.IsGame())
                m_DeliveryVehicles.Refresh();
        }

        protected override void OnUpdate()
        {
            if (!m_BuildingSystem.AnyHub || !FindHubs())
                return;

            // Stock, trucks and money below are written by simulation jobs.
            CompleteDependency();
            m_DeliveryTruckLookup.Update(this);
            m_GuestVehicleLookup.Update(this);
            m_LayoutElementLookup.Update(this);
            m_ResourceDataLookup.Update(this);
            m_PrefabRefLookup.Update(this);
            m_StorageCompanyDataLookup.Update(this);
            m_SoldThisPass.Clear();

            var resourcePrefabs = m_ResourceSystem.GetPrefabs();
            var smallVehicleRadius = Mod.Settings.HubSmallVehicleRadius;
            var cargoBikeRadius = math.min(Mod.Settings.HubCargoBikeRadius, smallVehicleRadius);
            var maxCargoBikes = Mod.Settings.HubMaxCargoBikesPerOrder;
            var random = new Unity.Mathematics.Random(math.max(1u, m_SimulationSystem.frameIndex));
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            using var buyers = m_BuyerQuery.ToEntityArray(Allocator.Temp);
            foreach (var buyer in buyers)
            {
                var request = EntityManager.GetComponentData<ResourceBuyer>(buyer);
                var resource = request.m_ResourceNeeded;
                // A station only sells imports, and weightless goods never travel.
                if ((request.m_Flags & SetupTargetFlags.Import) == 0)
                    continue;
                var weight = EconomyUtils.GetWeight(resource, resourcePrefabs, ref m_ResourceDataLookup);
                if (weight == 0f)
                    continue;

                // The game charges the payer, which is the shop itself for a company purchase.
                var payer = request.m_Payer != Entity.Null ? request.m_Payer : buyer;
                if (!EntityManager.HasBuffer<Game.Economy.Resources>(payer))
                    continue;

                var property = EntityManager.GetComponentData<PropertyRenter>(buyer).m_Property;
                if (!EntityManager.TryGetComponent<CurrentDistrict>(property, out var district)
                    || !m_HubsByDistrict.TryGetValue(district.m_District, out var hubs)
                    || !EntityManager.TryGetComponent<Game.Objects.Transform>(property, out var shopTransform)
                    || !TryPickHub(hubs, resource, request.m_AmountNeeded, shopTransform.m_Position, out var hub, out var available, out var distance))
                    continue;

                var amount = math.min(request.m_AmountNeeded, available);
                if (distance <= cargoBikeRadius && m_DeliveryVehicles.AnyCargoBike)
                {
                    var carried = m_DeliveryVehicles.PlanCargoBikes(resource, amount, maxCargoBikes, m_CargoBikePlan);
                    if (carried > 0)
                    {
                        SellByCargoBikes(commandBuffer, ref random, buyer, payer, hub, resource, carried, weight, distance, resourcePrefabs);
                        continue;
                    }
                }

                var van = default(DeliveryTruckSelectItem);
                var useVan = distance <= smallVehicleRadius && m_DeliveryVehicles.TryPickVan(resource, amount, out van);
                if (useVan)
                    amount = math.min(amount, van.m_Capacity);

                Sell(payer, hub, resource, amount, weight, distance, resourcePrefabs);
                // A vehicle loads the goods at the hub later: until then they only count as sold this pass.
                m_SoldThisPass.TryGetValue((hub, resource), out var sold);
                m_SoldThisPass[(hub, resource)] = sold + amount;

                commandBuffer.RemoveComponent<ResourceBuyer>(buyer);
                AddCurrentTrading(commandBuffer, buyer).Add(Trading(resource, amount));

                if (!EntityManager.HasComponent<Target>(buyer))
                    commandBuffer.AddComponent(buyer, new Target { m_Target = hub });

                if (useVan)
                {
                    m_DeliveryVehicles.SendToHub(commandBuffer, ref random, van, resource, amount, shopTransform, property, buyer, hub);
                    m_SmallVehicleSalesSinceLog++;
                }
                else
                {
                    commandBuffer.AppendToBuffer(buyer, new TripNeeded
                    {
                        m_TargetAgent = hub,
                        m_Purpose = Game.Citizens.Purpose.CompanyShopping,
                        m_Data = amount,
                        m_Resource = resource,
                        m_Priority = TripPriority,
                    });
                }
                m_SalesSinceLog++;
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            if (m_SalesSinceLog >= 20)
            {
                Mod.Log.Info($"Local Hub: {m_SalesSinceLog} sales to shops, {m_SmallVehicleSalesSinceLog} fetched by small vehicle");
                m_SalesSinceLog = 0;
                m_SmallVehicleSalesSinceLog = 0;
            }
        }

        /// <summary>
        /// One sale, delivered by the planned cargo bikes: the goods leave the hub now, and each bike
        /// gets its own trading line, which the game clears when that bike delivers (matched by amount).
        /// </summary>
        private void SellByCargoBikes(EntityCommandBuffer commandBuffer, ref Unity.Mathematics.Random random, Entity buyer, Entity payer,
            Entity hub, Resource resource, int amount, float weight, float distance, ResourcePrefabs resourcePrefabs)
        {
            Sell(payer, hub, resource, amount, weight, distance, resourcePrefabs);
            EconomyUtils.AddResources(resource, -amount, EntityManager.GetBuffer<Game.Economy.Resources>(hub));

            commandBuffer.RemoveComponent<ResourceBuyer>(buyer);
            var trading = AddCurrentTrading(commandBuffer, buyer);
            var hubTransform = EntityManager.GetComponentData<Game.Objects.Transform>(hub);
            foreach (var (bike, load) in m_CargoBikePlan)
            {
                trading.Add(Trading(resource, load));
                m_DeliveryVehicles.SendFromHub(commandBuffer, ref random, bike, resource, load, hubTransform, hub, buyer);
            }
            m_SmallVehicleSalesSinceLog++;
            m_SalesSinceLog++;
        }

        /// <summary>Replaces the shop's trading lines, as ResourceBuyerSystem does when it sets up a purchase.</summary>
        private DynamicBuffer<CurrentTrading> AddCurrentTrading(EntityCommandBuffer commandBuffer, Entity buyer) =>
            EntityManager.HasBuffer<CurrentTrading>(buyer)
                ? commandBuffer.SetBuffer<CurrentTrading>(buyer)
                : commandBuffer.AddBuffer<CurrentTrading>(buyer);

        private CurrentTrading Trading(Resource resource, int amount) => new CurrentTrading
        {
            m_TradingResource = resource,
            m_TradingResourceAmount = amount,
            m_OutsideConnectionType = OutsideConnectionTransferType.None,
            m_TradingStartFrameIndex = m_SimulationSystem.frameIndex,
        };

        /// <summary>Lists are kept from pass to pass, emptied rather than reallocated.</summary>
        private bool FindHubs()
        {
            foreach (var hubs in m_HubsByDistrict.Values)
                hubs.Clear();
            var found = false;
            using var buildings = m_HubQuery.ToEntityArray(Allocator.Temp);
            foreach (var building in buildings)
            {
                if (!m_BuildingSystem.IsHub(EntityManager.GetComponentData<PrefabRef>(building).m_Prefab))
                    continue;
                var district = EntityManager.GetComponentData<CurrentDistrict>(building).m_District;
                if (district == Entity.Null)
                    continue;
                if (!m_HubsByDistrict.TryGetValue(district, out var hubs))
                    m_HubsByDistrict[district] = hubs = new List<Entity>();
                hubs.Add(building);
                found = true;
            }
            return found;
        }

        /// <summary>
        /// The nearest hub of the district that can serve at least half the request, as the game asks
        /// of any seller (ResourceBuyerSystem.ProcessResourceBuyer), net of goods already promised to
        /// vehicles on their way.
        /// </summary>
        private bool TryPickHub(List<Entity> hubs, Resource resource, int amountNeeded, float3 shopPosition,
            out Entity hub, out int available, out float distance)
        {
            hub = Entity.Null;
            available = 0;
            distance = float.MaxValue;
            foreach (var candidate in hubs)
            {
                if (!Stores(candidate, resource))
                    continue;

                m_SoldThisPass.TryGetValue((candidate, resource), out var sold);
                var stock = EconomyUtils.GetResources(resource, EntityManager.GetBuffer<Game.Economy.Resources>(candidate, true))
                    - VehicleUtils.GetAllBuyingResourcesTrucks(candidate, resource, ref m_DeliveryTruckLookup, ref m_GuestVehicleLookup, ref m_LayoutElementLookup)
                    - sold;
                if (stock < math.max(amountNeeded / 2, 1))
                    continue;

                var candidateDistance = math.distance(shopPosition, EntityManager.GetComponentData<Game.Objects.Transform>(candidate).m_Position);
                if (candidateDistance < distance)
                {
                    hub = candidate;
                    available = stock;
                    distance = candidateDistance;
                }
            }
            return hub != Entity.Null;
        }

        private bool Stores(Entity hub, Resource resource)
        {
            var data = m_StorageCompanyDataLookup[m_PrefabRefLookup[hub].m_Prefab];
            if (EntityManager.HasBuffer<InstalledUpgrade>(hub))
                UpgradeUtils.CombineStats(ref data, EntityManager.GetBuffer<InstalledUpgrade>(hub, true), ref m_PrefabRefLookup, ref m_StorageCompanyDataLookup);
            return (data.m_StoredResources & resource) != Resource.NoResource;
        }

        /// <summary>
        /// The money side of a sale by a storage seller, as ResourceBuyerSystem.BuyJob does it: the shop
        /// pays the industrial price plus the hub's trade cost, both trade costs are updated, and the
        /// goods themselves leave the hub only when the vehicle loads them.
        /// </summary>
        private void Sell(Entity buyer, Entity hub, Resource resource, int amount, float weight, float distance, ResourcePrefabs resourcePrefabs)
        {
            var price = EconomyUtils.GetIndustrialPrice(resource, resourcePrefabs, ref m_ResourceDataLookup) * amount;
            if (EntityManager.TryGetBuffer<TradeCost>(hub, false, out var hubCosts))
            {
                var hubCost = EconomyUtils.GetTradeCost(resource, hubCosts);
                price += amount * hubCost.m_BuyCost;
                var transportCost = (float)EconomyUtils.GetTransportCost(distance, resource, amount, weight) / (1f + amount);
                var buyerHasCosts = EntityManager.TryGetBuffer<TradeCost>(buyer, false, out var buyerCosts);
                var buyerCost = buyerHasCosts ? EconomyUtils.GetTradeCost(resource, buyerCosts) : default;

                hubCost.m_SellCost = math.lerp(hubCost.m_SellCost, transportCost + buyerCost.m_SellCost, 0.5f);
                EconomyUtils.SetTradeCost(resource, hubCost, hubCosts, keepLastTime: true);

                if (buyerHasCosts)
                {
                    var deliveredCost = transportCost + hubCost.m_BuyCost;
                    buyerCost.m_BuyCost = deliveredCost < buyerCost.m_BuyCost
                        ? deliveredCost
                        : math.lerp(buyerCost.m_BuyCost, deliveredCost, 0.5f);
                    EconomyUtils.SetTradeCost(resource, buyerCost, buyerCosts, keepLastTime: true);
                }
            }

            EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt(price), EntityManager.GetBuffer<Game.Economy.Resources>(buyer));

            if (EntityManager.TryGetComponent<BuyingCompany>(buyer, out var buyingCompany))
            {
                buyingCompany.m_LastTradePartner = hub;
                EntityManager.SetComponentData(buyer, buyingCompany);
            }

            if (EntityManager.TryGetComponent<CompanyStatisticData>(hub, out var hubStatistics))
            {
                hubStatistics.m_CurrentNumberOfCustomers++;
                EntityManager.SetComponentData(hub, hubStatistics);
            }
            if (EntityManager.TryGetComponent<CompanyStatisticData>(buyer, out var buyerStatistics))
            {
                buyerStatistics.m_CurrentCostOfBuyingResources += math.abs((int)price);
                EntityManager.SetComponentData(buyer, buyerStatistics);
            }
        }
    }
}
