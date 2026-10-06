using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SolarpunkMod.Hub
{
    /// <summary>
    /// The mod's own last-mile vehicles (assets/vehicles/, imported as delivery trucks from the
    /// EU_DeliveryVan01 template), when the player has them loaded. Their game data is set here rather
    /// than in the asset: electric, small, slow, quiet, with their own capacity and rider seat. They are
    /// kept locked so that only the hub sends them; the game's own deliveries never pick them.
    /// The bikes (ridden, seat Biking) serve shops near the hub, several to an order; the vans (driven)
    /// serve the shops farther away, one to an order.
    /// Prefab data only, set again on every game load: nothing reaches a save.
    /// </summary>
    public class HubVehicleModels
    {
        private const float KilometresPerHourToMetresPerSecond = 1f / 3.6f;

        /// <summary>From the smallest load to the largest; the seat is the rider anchor of each build.py, in game axes.</summary>
        private static readonly Model[] Models =
        {
            new Model("SolarpunkCargoBikeBox01", 500, 20f, ActivityType.Biking, new float3(0f, 1.05f, -0.62f)),
            new Model("SolarpunkCargoBikePallet01", 1000, 18f, ActivityType.Biking, new float3(0f, 1.07f, -0.20f)),
            new Model("SolarpunkCoveredCargoBike01", 1500, 20f, ActivityType.Biking, new float3(0f, 0.95f, 0.28f)),
            new Model("SolarpunkElectricTrike01", 2000, 45f, ActivityType.Driving, new float3(0f, 0.95f, 0.45f)),
            new Model("SolarpunkKeiTruck01", 4000, 60f, ActivityType.Driving, new float3(0.30f, 0.95f, 1.15f)),
        };

        private readonly SystemBase m_System;
        private readonly PrefabSystem m_PrefabSystem;
        private readonly EntityQuery m_DeliveryTruckPrefabQuery;
        // Each sorted by capacity, from the smallest.
        private readonly List<DeliveryTruckSelectItem> m_CargoBikes = new List<DeliveryTruckSelectItem>();
        private readonly List<DeliveryTruckSelectItem> m_Vans = new List<DeliveryTruckSelectItem>();

        public HubVehicleModels(SystemBase system)
        {
            m_System = system;
            m_PrefabSystem = system.World.GetOrCreateSystemManaged<PrefabSystem>();
            m_DeliveryTruckPrefabQuery = system.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DeliveryTruckData>(),
                ComponentType.ReadOnly<CarData>(),
                ComponentType.ReadOnly<PrefabData>());
        }

        public bool Contains(Entity prefab) => IndexOf(m_CargoBikes, prefab) >= 0 || IndexOf(m_Vans, prefab) >= 0;

        public bool AnyCargoBike => m_CargoBikes.Count > 0;

        /// <summary>The seat a delivery vehicle's driver or rider takes.</summary>
        internal static bool IsDriverSeat(ActivityLocationElement seat) =>
            (seat.m_ActivityMask.m_Mask & (new ActivityMask(ActivityType.Driving).m_Mask | new ActivityMask(ActivityType.Biking).m_Mask)) != 0;

        internal static DeliveryTruckSelectItem SelectItem(Entity prefab, DeliveryTruckData data) => new DeliveryTruckSelectItem
        {
            m_Capacity = data.m_CargoCapacity,
            m_Cost = data.m_CostToDrive,
            m_Resources = data.m_TransportedResources,
            m_Prefab1 = prefab,
        };

        /// <summary>
        /// Finds the models among the loaded prefabs, by name: an imported asset's PrefabID carries its
        /// asset hash, so a PrefabID built from type and name alone would not match.
        /// </summary>
        public void Refresh()
        {
            m_CargoBikes.Clear();
            m_Vans.Clear();
            using var prefabs = m_DeliveryTruckPrefabQuery.ToEntityArray(Allocator.Temp);
            foreach (var prefab in prefabs)
            {
                var name = m_PrefabSystem.GetPrefabName(prefab);
                foreach (var model in Models)
                {
                    if (model.Name != name)
                        continue;
                    Configure(prefab, model);
                    var item = SelectItem(prefab, m_System.EntityManager.GetComponentData<DeliveryTruckData>(prefab));
                    (model.Seat == ActivityType.Biking ? m_CargoBikes : m_Vans).Add(item);
                }
            }
            m_CargoBikes.Sort((a, b) => a.m_Capacity.CompareTo(b.m_Capacity));
            m_Vans.Sort((a, b) => a.m_Capacity.CompareTo(b.m_Capacity));

            var loaded = m_CargoBikes.Count + m_Vans.Count;
            if (loaded > 0)
                RebuildGameDeliveryVehicleList();
            Mod.Log.Info(loaded > 0
                ? $"Local Hub: {loaded} own delivery vehicles loaded ({m_CargoBikes.Count} cargo bikes)"
                : "Local Hub: own delivery vehicles not loaded, the game's smallest delivery vehicle is used");
        }

        /// <summary>
        /// Splits an order between cargo bikes, at most maxBikes of them: each time the smallest bike
        /// that takes what is left, else the largest. Returns the amount the bikes carry, which may be
        /// less than asked: the shop then orders the rest later.
        /// </summary>
        public int PlanCargoBikes(Resource resource, int amount, int maxBikes, List<(DeliveryTruckSelectItem bike, int load)> plan)
        {
            plan.Clear();
            var left = amount;
            while (left > 0 && plan.Count < maxBikes && TryPick(m_CargoBikes, resource, left, out var bike))
            {
                var load = math.min(left, bike.m_Capacity);
                plan.Add((bike, load));
                left -= load;
            }
            return amount - left;
        }

        /// <summary>The smallest van that takes the whole amount, else the largest that carries the resource.</summary>
        public bool TryPickVan(Resource resource, int amount, out DeliveryTruckSelectItem item) => TryPick(m_Vans, resource, amount, out item);

        private static bool TryPick(List<DeliveryTruckSelectItem> models, Resource resource, int amount, out DeliveryTruckSelectItem item)
        {
            item = default;
            foreach (var model in models)
            {
                if ((model.m_Resources & resource) == Resource.NoResource)
                    continue;
                item = model;
                if (model.m_Capacity >= amount)
                    break;
            }
            return item.m_Prefab1 != Entity.Null;
        }

        private static int IndexOf(List<DeliveryTruckSelectItem> models, Entity prefab) => models.FindIndex(model => model.m_Prefab1 == prefab);

        /// <summary>
        /// The game builds its delivery vehicle list once after a load's deserialisation, which may come
        /// before our lock: asking for a rebuild the way PostDeserialize does keeps our models out of it.
        /// </summary>
        private void RebuildGameDeliveryVehicleList()
        {
            m_System.World.GetOrCreateSystemManaged<VehicleCapacitySystem>()
                .PostDeserialize(new Context(Purpose.LoadGame, Game.Version.current, default));
        }

        private void Configure(Entity prefab, Model model)
        {
            var entityManager = m_System.EntityManager;

            var delivery = entityManager.GetComponentData<DeliveryTruckData>(prefab);
            delivery.m_CargoCapacity = model.Capacity;
            entityManager.SetComponentData(prefab, delivery);

            var car = entityManager.GetComponentData<CarData>(prefab);
            car.m_EnergyType = Game.Vehicles.EnergyTypes.Electricity;
            car.m_SizeClass = Game.Vehicles.SizeClass.Small;
            car.m_MaxSpeed = model.MaxSpeedKilometresPerHour * KilometresPerHourToMetresPerSecond;
            entityManager.SetComponentData(prefab, car);

            // Road wear, noise, air pollution (x, y, z): light and electric. Absolute values, as this
            // runs again on every load and prefab data outlives a game.
            if (entityManager.HasComponent<VehicleSideEffectData>(prefab))
            {
                entityManager.SetComponentData(prefab, new VehicleSideEffectData
                {
                    m_Min = new float3(0.2f, 1f, 0f),
                    m_Max = new float3(0.4f, 4f, 0f),
                });
            }

            // The copied van seat becomes the rider's: Biking puts a resident in the cycling pose.
            if (entityManager.HasBuffer<ActivityLocationElement>(prefab))
            {
                var seats = entityManager.GetBuffer<ActivityLocationElement>(prefab);
                for (var i = 0; i < seats.Length; i++)
                {
                    var seat = seats[i];
                    if (!IsDriverSeat(seat))
                        continue;
                    seat.m_ActivityMask = new ActivityMask(model.Seat);
                    seat.m_Position = model.SeatPosition;
                    seats[i] = seat;
                    break;
                }
            }

            Lock(prefab);
        }

        /// <summary>
        /// The game's delivery vehicle list skips locked prefabs (VehicleCapacitySystem). A prefab with
        /// unlock requirements also requires itself, so the game's UnlockSystem never opens it.
        /// </summary>
        private void Lock(Entity prefab)
        {
            var entityManager = m_System.EntityManager;
            if (!entityManager.HasComponent<Locked>(prefab))
                entityManager.AddComponent<Locked>(prefab);
            entityManager.SetComponentEnabled<Locked>(prefab, true);

            if (!entityManager.HasBuffer<UnlockRequirement>(prefab))
                return;
            var requirements = entityManager.GetBuffer<UnlockRequirement>(prefab);
            foreach (var requirement in requirements)
            {
                if (requirement.m_Prefab == prefab)
                    return;
            }
            requirements.Add(new UnlockRequirement(prefab, UnlockFlags.RequireAll));
        }

        private readonly struct Model
        {
            public readonly string Name;
            public readonly int Capacity;
            public readonly float MaxSpeedKilometresPerHour;
            public readonly ActivityType Seat;
            public readonly float3 SeatPosition;

            public Model(string name, int capacity, float maxSpeedKilometresPerHour, ActivityType seat, float3 seatPosition)
            {
                Name = name;
                Capacity = capacity;
                MaxSpeedKilometresPerHour = maxSpeedKilometresPerHour;
                Seat = seat;
                SeatPosition = seatPosition;
            }
        }
    }
}
