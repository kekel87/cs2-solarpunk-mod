using System.Collections.Generic;
using Game.City;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SolarpunkMod.Hub
{
    /// <summary>
    /// The small delivery vehicles between a hub and a nearby shop: the mod's own models when loaded
    /// (HubVehicleModels), else for each resource the game's delivery vehicle with the smallest
    /// capacity that carries it (the delivery motorbike for light goods).
    /// Spawned the way TripNeededSystem.SpawnDeliveryTruck does, with a resident of the game at the
    /// handlebars. A shop's van fetches the goods at the hub like any game truck; cargo bikes are sent
    /// by the hub, loaded, and ride to the shop like bicycles (CargoBikeSystem).
    /// </summary>
    public class HubDeliveryVehicles
    {
        private readonly SystemBase m_System;
        private readonly CityConfigurationSystem m_CityConfigurationSystem;
        private readonly EntityQuery m_VehiclePrefabQuery;
        private readonly EntityQuery m_ResidentPrefabQuery;
        private readonly ComponentTypeSet m_CurrentLaneTypesRelative;
        private readonly Dictionary<Resource, DeliveryTruckSelectItem> m_SmallestByResource = new Dictionary<Resource, DeliveryTruckSelectItem>();
        private readonly HubVehicleModels m_OwnModels;

        private ComponentLookup<DeliveryTruckData> m_DeliveryTruckDataLookup;
        private ComponentLookup<ObjectData> m_ObjectDataLookup;
        private EntityTypeHandle m_EntityType;
        private ComponentTypeHandle<CreatureData> m_CreatureDataType;
        private ComponentTypeHandle<ResidentData> m_ResidentDataType;

        public HubDeliveryVehicles(SystemBase system)
        {
            m_System = system;
            m_CityConfigurationSystem = system.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            m_VehiclePrefabQuery = system.EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<DeliveryTruckData>(),
                    ComponentType.ReadOnly<CarData>(),
                    ComponentType.ReadOnly<ObjectData>(),
                    ComponentType.ReadOnly<PrefabData>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Locked>(),
                    ComponentType.ReadOnly<CarTractorData>(),
                    ComponentType.ReadOnly<CarTrailerData>(),
                },
            });
            m_ResidentPrefabQuery = system.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ObjectData>(),
                ComponentType.ReadOnly<HumanData>(),
                ComponentType.ReadOnly<ResidentData>(),
                ComponentType.ReadOnly<PrefabData>());
            m_CurrentLaneTypesRelative = new ComponentTypeSet(new[]
            {
                ComponentType.ReadWrite<Moving>(),
                ComponentType.ReadWrite<TransformFrame>(),
                ComponentType.ReadWrite<HumanNavigation>(),
                ComponentType.ReadWrite<HumanCurrentLane>(),
                ComponentType.ReadWrite<Blocker>(),
            });
            m_DeliveryTruckDataLookup = system.GetComponentLookup<DeliveryTruckData>(true);
            m_ObjectDataLookup = system.GetComponentLookup<ObjectData>(true);
            m_EntityType = system.GetEntityTypeHandle();
            m_CreatureDataType = system.GetComponentTypeHandle<CreatureData>(true);
            m_ResidentDataType = system.GetComponentTypeHandle<ResidentData>(true);
            m_OwnModels = new HubVehicleModels(system);
        }

        /// <summary>
        /// Picks again the smallest vehicle for every resource: unlocks and the city theme change with
        /// the loaded game.
        /// </summary>
        public void Refresh()
        {
            // First, so the own models are locked out of the game's list below.
            m_OwnModels.Refresh();
            m_SmallestByResource.Clear();
            using var prefabs = m_VehiclePrefabQuery.ToEntityArray(Allocator.Temp);
            using var datas = m_VehiclePrefabQuery.ToComponentDataArray<DeliveryTruckData>(Allocator.Temp);
            var iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                var smallest = FindSmallest(iterator.resource, prefabs, datas);
                if (smallest.m_Prefab1 != Entity.Null)
                    m_SmallestByResource[iterator.resource] = smallest;
            }
        }

        /// <summary>
        /// The mod's own models, for the panels to count apart. The game's smallest vehicle, sent when
        /// the models are not loaded, also runs the game's ordinary deliveries: it is not told apart.
        /// </summary>
        public bool IsSmallVehicle(Entity prefab) => m_OwnModels.Contains(prefab);

        public bool AnyCargoBike => m_OwnModels.AnyCargoBike;

        /// <inheritdoc cref="HubVehicleModels.PlanCargoBikes"/>
        public int PlanCargoBikes(Resource resource, int amount, int maxBikes, List<(DeliveryTruckSelectItem bike, int load)> plan) =>
            m_OwnModels.PlanCargoBikes(resource, amount, maxBikes, plan);

        /// <summary>The van for this load: an own model sized to the amount, else the game's smallest vehicle.</summary>
        public bool TryPickVan(Resource resource, int amount, out DeliveryTruckSelectItem item)
        {
            return m_OwnModels.TryPickVan(resource, amount, out item) || m_SmallestByResource.TryGetValue(resource, out item);
        }

        private static DeliveryTruckSelectItem FindSmallest(Resource resource, NativeArray<Entity> prefabs, NativeArray<DeliveryTruckData> datas)
        {
            var smallest = default(DeliveryTruckSelectItem);
            for (var i = 0; i < prefabs.Length; i++)
            {
                var data = datas[i];
                if ((data.m_TransportedResources & resource) == Resource.NoResource || data.m_CargoCapacity <= 0)
                    continue;
                if (smallest.m_Prefab1 == Entity.Null || data.m_CargoCapacity < smallest.m_Capacity)
                    smallest = HubVehicleModels.SelectItem(prefabs[i], data);
            }
            return smallest;
        }

        /// <summary>Sends the vehicle from the shop's building to fetch the goods it bought at the hub.</summary>
        public void SendToHub(EntityCommandBuffer commandBuffer, ref Random random, DeliveryTruckSelectItem item, Resource resource,
            int amount, Transform shopTransform, Entity shopBuilding, Entity shop, Entity hub)
        {
            var state = DeliveryTruckFlags.Buying | DeliveryTruckFlags.UpdateSellerQuantity | DeliveryTruckFlags.UpdateOwnerQuantity;
            var vehicle = Create(commandBuffer, ref random, item, resource, amount, shopTransform, shopBuilding, state);
            commandBuffer.SetComponent(vehicle, new Target(hub));
            commandBuffer.AddComponent(vehicle, new Owner(shop));
        }

        /// <summary>
        /// Sends a cargo bike from the hub, loaded with goods the shop already paid for. The shop owns it,
        /// so the game's delivery payment nets out (DeliveryTruckAISystem pays the owner the price it
        /// charges the target), and Buying makes the shop count its load as on the way
        /// (VehicleUtils.GetBuyingTrucksLoad), so it does not order the same goods again; Buying changes
        /// nothing else for a loaded truck. The Bicycle tag makes it spawn, route and ride like a bicycle.
        /// </summary>
        public void SendFromHub(EntityCommandBuffer commandBuffer, ref Random random, DeliveryTruckSelectItem item, Resource resource,
            int amount, Transform hubTransform, Entity hub, Entity shop)
        {
            var vehicle = Create(commandBuffer, ref random, item, resource, amount, hubTransform, hub,
                DeliveryTruckFlags.Loaded | DeliveryTruckFlags.Delivering | DeliveryTruckFlags.Buying);
            commandBuffer.SetComponent(vehicle, new Target(shop));
            commandBuffer.AddComponent(vehicle, new Owner(shop));
            commandBuffer.AddComponent(vehicle, default(Bicycle));
        }

        private Entity Create(EntityCommandBuffer commandBuffer, ref Random random, DeliveryTruckSelectItem item, Resource resource,
            int amount, Transform transform, Entity source, DeliveryTruckFlags state)
        {
            m_DeliveryTruckDataLookup.Update(m_System);
            m_ObjectDataLookup.Update(m_System);
            var returnAmount = 0;
            var vehicle = new DeliveryTruckSelectData(default).CreateVehicle(commandBuffer.AsParallelWriter(), 0, ref random,
                ref m_DeliveryTruckDataLookup, ref m_ObjectDataLookup, item, resource, Resource.NoResource,
                ref amount, ref returnAmount, transform, source, state);
            if (CreateDriver(commandBuffer, vehicle, item.m_Prefab1, transform, ref random))
                commandBuffer.AddBuffer<Passenger>(vehicle);
            return vehicle;
        }

        /// <summary>
        /// The driver seat of TripNeededSystem.CreatePassengers: the frontmost Driving or Biking spot on
        /// the driving side, taken by a dummy adult resident.
        /// </summary>
        private bool CreateDriver(EntityCommandBuffer commandBuffer, Entity vehicle, Entity vehiclePrefab, Transform transform, ref Random random)
        {
            if (!m_System.EntityManager.HasBuffer<ActivityLocationElement>(vehiclePrefab))
                return false;

            var leftHandTraffic = m_CityConfigurationSystem.leftHandTraffic;
            var seats = m_System.EntityManager.GetBuffer<ActivityLocationElement>(vehiclePrefab, true);
            var seat = -1;
            var frontmost = float.MinValue;
            for (var i = 0; i < seats.Length; i++)
            {
                var location = seats[i];
                if (!HubVehicleModels.IsDriverSeat(location))
                    continue;
                var mirrored = ((location.m_ActivityFlags & ActivityFlags.InvertLefthandTraffic) != 0 && leftHandTraffic)
                    || ((location.m_ActivityFlags & ActivityFlags.InvertRighthandTraffic) != 0 && !leftHandTraffic);
                location.m_Position.x = math.select(location.m_Position.x, -location.m_Position.x, mirrored);
                var onDrivingSide = math.abs(location.m_Position.x) < 0.5f || location.m_Position.x >= 0f != leftHandTraffic;
                if (onDrivingSide && location.m_Position.z > frontmost)
                {
                    seat = i;
                    frontmost = location.m_Position.z;
                }
            }
            if (seat == -1)
                return false;

            var citizen = default(Citizen);
            if (random.NextBool())
                citizen.m_State |= CitizenFlags.Male;
            citizen.SetAge(CitizenAge.Adult);
            citizen.m_PseudoRandom = (ushort)(random.NextUInt() % 65536);

            m_EntityType.Update(m_System);
            m_CreatureDataType.Update(m_System);
            m_ResidentDataType.Update(m_System);
            var humanChunks = m_ResidentPrefabQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var chunksReady);
            chunksReady.Complete();
            var residentPrefab = ObjectEmergeSystem.SelectResidentPrefab(citizen, humanChunks, m_EntityType,
                ref m_CreatureDataType, ref m_ResidentDataType, out _, out var randomSeed);
            humanChunks.Dispose();
            if (residentPrefab == Entity.Null)
                return false;

            var driver = commandBuffer.CreateEntity(m_ObjectDataLookup[residentPrefab].m_Archetype);
            commandBuffer.RemoveComponent(driver, m_CurrentLaneTypesRelative);
            commandBuffer.SetComponent(driver, transform);
            commandBuffer.SetComponent(driver, new PrefabRef { m_Prefab = residentPrefab });
            commandBuffer.SetComponent(driver, new Game.Creatures.Resident { m_Flags = ResidentFlags.InVehicle | ResidentFlags.DummyTraffic });
            commandBuffer.SetComponent(driver, randomSeed);
            commandBuffer.AddComponent(driver, new CurrentVehicle
            {
                m_Vehicle = vehicle,
                m_Flags = CreatureVehicleFlags.Ready | CreatureVehicleFlags.Leader | CreatureVehicleFlags.Driver,
            });
            commandBuffer.AddComponent(driver, new Relative
            {
                m_Position = seats[seat].m_Position,
                m_Rotation = seats[seat].m_Rotation,
                m_BoneIndex = new int3(0, -1, -1),
            });
            return true;
        }
    }
}
