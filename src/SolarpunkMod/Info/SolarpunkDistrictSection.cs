using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Simulation;
using Game.Tools;
using Game.UI.InGame;
using Game.Vehicles;
using SolarpunkMod.Hub;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Info
{
    /// <summary>
    /// "Solarpunk" section of a selected district's info panel: traffic present in the district right
    /// now (a border road counts for both its districts), rated green / orange / red, with a short
    /// history. Sampled each time the game refreshes the panel; kept in memory only, reset when
    /// another district is selected.
    /// </summary>
    public partial class SolarpunkDistrictSection : InfoSectionBase
    {
        private const int SmoothingSampleCount = 10;
        // Same pace as the city panel.
        private const uint MinimumFramesBetweenSamples = 128;
        private const int HistoryLength = 60;
        private const int MinimumTravellersPerSample = 30;
        private const float HysteresisMargin = 0.1f;

        private const float CarShareGreenMax = 0.2f;
        private const float CarShareOrangeMax = 0.4f;
        // Starting points, to calibrate in game: the log prints the measured value.
        private const float TrucksPerKmGreenMax = 0.1f;
        private const float TrucksPerKmOrangeMax = 0.5f;

        private EntityQuery m_ResidentQuery;
        private EntityQuery m_PersonalCarQuery;
        private EntityQuery m_DeliveryTruckQuery;
        private EntityQuery m_GarbageTruckQuery;
        private EntityQuery m_DistrictEdgeQuery;
        private TravelModeClassifier m_TravelModeClassifier;
        private DistrictLocator m_DistrictLocator;
        private HubSaleSystem m_HubSaleSystem;
        private SimulationSystem m_SimulationSystem;
        private uint m_LastSampleFrame;

        private Entity m_District;
        private readonly Queue<ModalCounts> m_ModalSamples = new Queue<ModalCounts>();
        private readonly Queue<int> m_DeliveryTruckSamples = new Queue<int>();
        private readonly Queue<float> m_CarShareHistory = new Queue<float>();
        private readonly Queue<float> m_DeliveryTruckHistory = new Queue<float>();
        private Rating m_CarShareRating = Rating.Unknown;
        private Rating m_DeliveryTruckRating = Rating.Unknown;
        private int m_SamplesSinceLog;

        private ModalCounts m_Modal;
        private int m_PersonalCarCount;
        private int m_DeliveryTruckCount;
        private int m_SmallDeliveryVehicleCount;
        private int m_GarbageTruckCount;

        private enum Rating
        {
            Unknown,
            Good,
            Warning,
            Bad,
        }

        // Prefixed like any binding group of the mod. The UI finds the section by its full type name
        // (UI/src/district/district-section.tsx): renaming or moving this class breaks it silently.
        protected override string group => $"{Mod.Id}.{nameof(SolarpunkDistrictSection)}";

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ResidentQuery = LiveQuery(ComponentType.ReadOnly<Resident>());
            m_PersonalCarQuery = LiveQuery(ComponentType.ReadOnly<PersonalCar>(), ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.Exclude<Bicycle>(), ComponentType.Exclude<ParkedCar>());
            m_DeliveryTruckQuery = LiveQuery(ComponentType.ReadOnly<DeliveryTruck>(), ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<Game.Prefabs.PrefabRef>(), ComponentType.Exclude<ParkedCar>());
            m_GarbageTruckQuery = LiveQuery(ComponentType.ReadOnly<GarbageTruck>(), ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.Exclude<ParkedCar>());
            m_DistrictEdgeQuery = LiveQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<BorderDistrict>(), ComponentType.ReadOnly<Curve>());
            m_TravelModeClassifier = new TravelModeClassifier(this);
            m_DistrictLocator = new DistrictLocator(this);
            m_HubSaleSystem = World.GetOrCreateSystemManaged<HubSaleSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        }

        private EntityQuery LiveQuery(params ComponentType[] components)
        {
            var allComponents = new List<ComponentType>(components) { ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>() };
            return GetEntityQuery(allComponents.ToArray());
        }

        protected override void OnUpdate()
        {
            visible = EntityManager.HasComponent<District>(selectedEntity) && EntityManager.HasComponent<Area>(selectedEntity);
            // Anything else selected in between: the next look at a district starts a fresh history.
            if (!visible)
                m_District = Entity.Null;
        }

        protected override void Reset()
        {
        }

        protected override void OnProcess()
        {
            if (selectedEntity != m_District)
                StartOver(selectedEntity);
            // The panel also refreshes while paused or on any edit of the district: sample on simulation time only.
            else if (m_SimulationSystem.frameIndex - m_LastSampleFrame < MinimumFramesBetweenSamples)
                return;
            m_LastSampleFrame = m_SimulationSystem.frameIndex;

            // Lookups below read components that simulation jobs may still be writing.
            CompleteDependency();
            m_TravelModeClassifier.Update(this);
            m_DistrictLocator.Update(this, m_District);

            Push(m_ModalSamples, SampleModalCounts(), SmoothingSampleCount);
            CountDeliveryVehicles();
            Push(m_DeliveryTruckSamples, m_DeliveryTruckCount, SmoothingSampleCount);
            m_PersonalCarCount = CountVehicles<PersonalCar>(m_PersonalCarQuery, car => (car.m_State & PersonalCarFlags.DummyTraffic) == 0);
            m_GarbageTruckCount = CountVehicles<GarbageTruck>(m_GarbageTruckQuery, _ => true);
            var roadLengthKm = MeasureRoadLengthKm();

            m_Modal = new ModalCounts();
            foreach (var sample in m_ModalSamples)
                m_Modal.Add(sample);
            var deliveryTruckSum = 0;
            foreach (var sample in m_DeliveryTruckSamples)
                deliveryTruckSum += sample;
            var averageDeliveryTrucks = (float)deliveryTruckSum / m_DeliveryTruckSamples.Count;
            var deliveryTrucksPerKm = roadLengthKm > 0f ? averageDeliveryTrucks / roadLengthKm : 0f;

            var carShare = m_Modal.ShareOf(m_Modal.Car);
            var enoughTravellers = m_Modal.TravellerCount >= MinimumTravellersPerSample * m_ModalSamples.Count;
            m_CarShareRating = enoughTravellers ? Rate(carShare, CarShareGreenMax, CarShareOrangeMax, m_CarShareRating) : Rating.Unknown;
            m_DeliveryTruckRating = roadLengthKm > 0f
                ? Rate(deliveryTrucksPerKm, TrucksPerKmGreenMax, TrucksPerKmOrangeMax, m_DeliveryTruckRating)
                : Rating.Unknown;

            Push(m_CarShareHistory, carShare, HistoryLength);
            Push(m_DeliveryTruckHistory, averageDeliveryTrucks, HistoryLength);

            // Calibration of the truck thresholds: to remove once calibrated in game.
            if (++m_SamplesSinceLog >= SmoothingSampleCount)
            {
                m_SamplesSinceLog = 0;
                Mod.Log.Info($"District {m_District}: car share {carShare:P0} over {m_Modal.TravellerCount / m_ModalSamples.Count} travellers, " +
                    $"{averageDeliveryTrucks:F1} delivery trucks on {roadLengthKm:F2} km = {deliveryTrucksPerKm:F2}/km");
            }
        }

        private void StartOver(Entity district)
        {
            m_District = district;
            m_LastSampleFrame = m_SimulationSystem.frameIndex;
            m_ModalSamples.Clear();
            m_DeliveryTruckSamples.Clear();
            m_CarShareHistory.Clear();
            m_DeliveryTruckHistory.Clear();
            m_CarShareRating = Rating.Unknown;
            m_DeliveryTruckRating = Rating.Unknown;
            m_SamplesSinceLog = 0;
        }

        private ModalCounts SampleModalCounts()
        {
            var counts = new ModalCounts();
            using var entities = m_ResidentQuery.ToEntityArray(Allocator.Temp);
            using var residents = m_ResidentQuery.ToComponentDataArray<Resident>(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var mode = m_TravelModeClassifier.Classify(entities[i], residents[i].m_Flags, out var vehicle);
                if (mode != TravelMode.None && m_DistrictLocator.IsPersonIn(entities[i], vehicle))
                    counts.Count(mode);
            }
            return counts;
        }

        private void CountDeliveryVehicles()
        {
            var count = DeliveryVehicleCount.Of(m_DeliveryTruckQuery, m_HubSaleSystem.DeliveryVehicles, m_DistrictLocator.IsLaneIn);
            m_DeliveryTruckCount = count.Trucks;
            m_SmallDeliveryVehicleCount = count.SmallVehicles;
        }

        private int CountVehicles<T>(EntityQuery query, Func<T, bool> isCounted) where T : unmanaged, IComponentData
        {
            using var vehicles = query.ToComponentDataArray<T>(Allocator.Temp);
            using var lanes = query.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);
            var count = 0;
            for (var i = 0; i < vehicles.Length; i++)
            {
                if (isCounted(vehicles[i]) && m_DistrictLocator.IsLaneIn(lanes[i].m_Lane))
                    count++;
            }
            return count;
        }

        private float MeasureRoadLengthKm()
        {
            using var borders = m_DistrictEdgeQuery.ToComponentDataArray<BorderDistrict>(Allocator.Temp);
            using var curves = m_DistrictEdgeQuery.ToComponentDataArray<Curve>(Allocator.Temp);
            var metres = 0f;
            for (var i = 0; i < borders.Length; i++)
            {
                if (borders[i].m_Left == m_District || borders[i].m_Right == m_District)
                    metres += curves[i].m_Length;
            }
            return metres / 1000f;
        }

        /// <summary>
        /// Green up to greenMax, orange up to orangeMax, red above. Leaving the current rating takes a
        /// 10 % overshoot, so a value hovering on a threshold does not blink.
        /// </summary>
        private static Rating Rate(float value, float greenMax, float orangeMax, Rating current)
        {
            var green = current == Rating.Good ? greenMax * (1f + HysteresisMargin)
                : current == Rating.Unknown ? greenMax : greenMax * (1f - HysteresisMargin);
            var orange = current == Rating.Bad ? orangeMax * (1f - HysteresisMargin)
                : current == Rating.Warning ? orangeMax * (1f + HysteresisMargin) : orangeMax;
            if (value <= green)
                return Rating.Good;
            return value <= orange ? Rating.Warning : Rating.Bad;
        }

        private static void Push<T>(Queue<T> queue, T value, int capacity)
        {
            queue.Enqueue(value);
            while (queue.Count > capacity)
                queue.Dequeue();
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("carShare");
            writer.Write(m_Modal.ShareOf(m_Modal.Car));
            writer.PropertyName("bicycleShare");
            writer.Write(m_Modal.ShareOf(m_Modal.Bicycle));
            writer.PropertyName("publicTransportShare");
            writer.Write(m_Modal.ShareOf(m_Modal.PublicTransport));
            writer.PropertyName("walkingShare");
            writer.Write(m_Modal.ShareOf(m_Modal.Walking));
            writer.PropertyName("carShareRating");
            writer.Write(m_CarShareRating.ToString());
            writer.PropertyName("personalCarCount");
            writer.Write(m_PersonalCarCount);
            writer.PropertyName("deliveryTruckCount");
            writer.Write(m_DeliveryTruckCount);
            writer.PropertyName("deliveryTruckRating");
            writer.Write(m_DeliveryTruckRating.ToString());
            writer.PropertyName("smallDeliveryVehicleCount");
            writer.Write(m_SmallDeliveryVehicleCount);
            writer.PropertyName("garbageTruckCount");
            writer.Write(m_GarbageTruckCount);
            WriteHistory(writer, "carShareHistory", m_CarShareHistory);
            WriteHistory(writer, "deliveryTruckHistory", m_DeliveryTruckHistory);
        }

        private static void WriteHistory(IJsonWriter writer, string name, Queue<float> history)
        {
            writer.PropertyName(name);
            writer.ArrayBegin(history.Count);
            foreach (var value in history)
                writer.Write(value);
            writer.ArrayEnd();
        }
    }
}
