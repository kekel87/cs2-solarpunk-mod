using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Economy;
using Game.Tools;
using Game.UI;
using Game.Vehicles;
using SolarpunkMod.Hub;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Info
{
    /// <summary>
    /// Read-only city snapshot for the Solarpunk info panel. Recomputed every few simulation seconds
    /// while the panel is open, pushed to the UI through one value binding per figure.
    /// </summary>
    public partial class SolarpunkInfoUISystem : UISystemBase
    {
        private const int UpdateIntervalInSimulationFrames = 128;
        private const int ModalShareSampleCount = 10;

        private UIUpdateState m_UpdateState;
        private readonly List<EventBindingBase> m_ValueBindings = new List<EventBindingBase>();

        private EntityQuery m_ResidentQuery;
        private EntityQuery m_PersonalCarQuery;
        private EntityQuery m_DeliveryTruckQuery;
        private EntityQuery m_GarbageTruckQuery;
        private EntityQuery m_CargoTrainQuery;
        private EntityQuery m_ElectricityProducerQuery;
        private EntityQuery m_RenewableElectricityProducerQuery;

        private TravelModeClassifier m_TravelModeClassifier;
        private HubSaleSystem m_HubSaleSystem;

        private ValueBinding<float> m_CarShareBinding;
        private ValueBinding<float> m_BicycleShareBinding;
        private ValueBinding<float> m_PublicTransportShareBinding;
        private ValueBinding<float> m_WalkingShareBinding;
        private ValueBinding<int> m_PersonalCarCountBinding;
        private ValueBinding<int> m_DeliveryTruckCountBinding;
        private ValueBinding<int> m_SmallDeliveryVehicleCountBinding;
        private ValueBinding<int> m_GarbageTruckCountBinding;
        private ValueBinding<int> m_CargoTrainCountBinding;
        private ValueBinding<int> m_GarbageTrainCountBinding;
        private ValueBinding<int> m_GarbageOnTrainsBinding;
        private ValueBinding<float> m_RenewableElectricityShareBinding;

        private BufferLookup<LayoutElement> m_LayoutLookup;
        private BufferLookup<Resources> m_ResourcesLookup;

        private readonly ModalCounts[] m_ModalSamples = new ModalCounts[ModalShareSampleCount];
        private int m_ModalSampleIndex;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_UpdateState = UIUpdateState.Create(World, UpdateIntervalInSimulationFrames);

            m_ResidentQuery = LiveQuery(ComponentType.ReadOnly<Resident>());
            m_PersonalCarQuery = LiveQuery(
                ComponentType.ReadOnly<PersonalCar>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.Exclude<Bicycle>(),
                ComponentType.Exclude<ParkedCar>());
            m_DeliveryTruckQuery = LiveQuery(
                ComponentType.ReadOnly<DeliveryTruck>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<Game.Prefabs.PrefabRef>(),
                ComponentType.Exclude<ParkedCar>());
            m_GarbageTruckQuery = LiveQuery(
                ComponentType.ReadOnly<GarbageTruck>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.Exclude<ParkedCar>());
            m_CargoTrainQuery = LiveQuery(
                ComponentType.ReadOnly<CargoTransport>(),
                ComponentType.ReadOnly<Train>(),
                ComponentType.ReadOnly<TrainCurrentLane>(),
                ComponentType.ReadOnly<Controller>(),
                ComponentType.Exclude<ParkedTrain>());
            m_ElectricityProducerQuery = LiveQuery(ComponentType.ReadOnly<ElectricityProducer>());
            m_RenewableElectricityProducerQuery = LiveQuery(
                ComponentType.ReadOnly<ElectricityProducer>(),
                ComponentType.ReadOnly<RenewableElectricityProduction>());

            m_TravelModeClassifier = new TravelModeClassifier(this);
            m_HubSaleSystem = World.GetOrCreateSystemManaged<HubSaleSystem>();
            m_LayoutLookup = GetBufferLookup<LayoutElement>(true);
            m_ResourcesLookup = GetBufferLookup<Resources>(true);

            m_CarShareBinding = Bind("carShare", 0f);
            m_BicycleShareBinding = Bind("bicycleShare", 0f);
            m_PublicTransportShareBinding = Bind("publicTransportShare", 0f);
            m_WalkingShareBinding = Bind("walkingShare", 0f);
            m_PersonalCarCountBinding = Bind("personalCarCount", 0);
            m_DeliveryTruckCountBinding = Bind("deliveryTruckCount", 0);
            m_SmallDeliveryVehicleCountBinding = Bind("smallDeliveryVehicleCount", 0);
            m_GarbageTruckCountBinding = Bind("garbageTruckCount", 0);
            m_CargoTrainCountBinding = Bind("cargoTrainCount", 0);
            m_GarbageTrainCountBinding = Bind("garbageTrainCount", 0);
            m_GarbageOnTrainsBinding = Bind("garbageOnTrains", 0);
            m_RenewableElectricityShareBinding = Bind("renewableElectricityShare", 0f);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!IsPanelOpen())
            {
                Array.Clear(m_ModalSamples, 0, m_ModalSamples.Length);
                m_UpdateState.ForceUpdate();
                return;
            }
            if (!m_UpdateState.Advance())
                return;

            // Lookups below read components that simulation jobs may still be writing.
            CompleteDependency();

            UpdateModalShare();
            m_PersonalCarCountBinding.Update(Count(m_PersonalCarQuery, (PersonalCar car) => (car.m_State & PersonalCarFlags.DummyTraffic) == 0));
            var deliveryVehicles = DeliveryVehicleCount.Of(m_DeliveryTruckQuery, m_HubSaleSystem.DeliveryVehicles, null);
            m_DeliveryTruckCountBinding.Update(deliveryVehicles.Trucks);
            m_SmallDeliveryVehicleCountBinding.Update(deliveryVehicles.SmallVehicles);
            m_GarbageTruckCountBinding.Update(m_GarbageTruckQuery.CalculateEntityCount());
            UpdateCargoTrains();
            m_RenewableElectricityShareBinding.Update(Share(SumCapacity(m_RenewableElectricityProducerQuery), SumCapacity(m_ElectricityProducerQuery)));
        }

        private EntityQuery LiveQuery(params ComponentType[] components)
        {
            var allComponents = new List<ComponentType>(components)
            {
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>(),
            };
            return GetEntityQuery(allComponents.ToArray());
        }

        private ValueBinding<T> Bind<T>(string name, T initialValue)
        {
            var binding = new ValueBinding<T>(Mod.Id, name, initialValue);
            AddBinding(binding);
            m_ValueBindings.Add(binding);
            return binding;
        }

        private bool IsPanelOpen()
        {
            foreach (var binding in m_ValueBindings)
            {
                if (binding.active)
                    return true;
            }
            return false;
        }

        private void UpdateModalShare()
        {
            m_ModalSamples[m_ModalSampleIndex] = SampleModalCounts();
            m_ModalSampleIndex = (m_ModalSampleIndex + 1) % ModalShareSampleCount;

            var total = new ModalCounts();
            foreach (var sample in m_ModalSamples)
                total.Add(sample);

            m_CarShareBinding.Update(total.ShareOf(total.Car));
            m_BicycleShareBinding.Update(total.ShareOf(total.Bicycle));
            m_PublicTransportShareBinding.Update(total.ShareOf(total.PublicTransport));
            m_WalkingShareBinding.Update(total.ShareOf(total.Walking));
        }

        private ModalCounts SampleModalCounts()
        {
            m_TravelModeClassifier.Update(this);

            var counts = new ModalCounts();
            using var entities = m_ResidentQuery.ToEntityArray(Allocator.Temp);
            using var residents = m_ResidentQuery.ToComponentDataArray<Resident>(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
                counts.Count(m_TravelModeClassifier.Classify(entities[i], residents[i].m_Flags, out _));
            return counts;
        }

        private static int Count<T>(EntityQuery query, Func<T, bool> isCounted) where T : unmanaged, IComponentData
        {
            using var components = query.ToComponentDataArray<T>(Allocator.Temp);
            var count = 0;
            foreach (var component in components)
            {
                if (isCounted(component))
                    count++;
            }
            return count;
        }

        /// <summary>Cargo trains on the move (leading vehicle, no through traffic), and the garbage aboard.</summary>
        private void UpdateCargoTrains()
        {
            m_LayoutLookup.Update(this);
            m_ResourcesLookup.Update(this);

            using var entities = m_CargoTrainQuery.ToEntityArray(Allocator.Temp);
            using var controllers = m_CargoTrainQuery.ToComponentDataArray<Controller>(Allocator.Temp);
            using var cargoTransports = m_CargoTrainQuery.ToComponentDataArray<CargoTransport>(Allocator.Temp);
            int trainCount = 0, garbageTrainCount = 0, garbageOnTrains = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                var isLeadingVehicle = controllers[i].m_Controller == entities[i];
                var isThroughTraffic = (cargoTransports[i].m_State & CargoTransportFlags.DummyTraffic) != 0;
                if (!isLeadingVehicle || isThroughTraffic)
                    continue;

                trainCount++;
                var garbage = GarbageAboard(entities[i]);
                if (garbage > 0)
                {
                    garbageTrainCount++;
                    garbageOnTrains += garbage;
                }
            }
            m_CargoTrainCountBinding.Update(trainCount);
            m_GarbageTrainCountBinding.Update(garbageTrainCount);
            m_GarbageOnTrainsBinding.Update(garbageOnTrains);
        }

        /// <summary>Garbage carried by every car of the train: each car keeps its own load.</summary>
        private int GarbageAboard(Entity train)
        {
            if (!m_LayoutLookup.TryGetBuffer(train, out var layout) || layout.Length == 0)
                return GarbageIn(train);

            var amount = 0;
            foreach (var car in layout)
                amount += GarbageIn(car.m_Vehicle);
            return amount;
        }

        private int GarbageIn(Entity vehicle)
        {
            if (!m_ResourcesLookup.TryGetBuffer(vehicle, out var resources))
                return 0;

            var amount = 0;
            foreach (var resource in resources)
            {
                if (resource.m_Resource == Resource.Garbage)
                    amount += resource.m_Amount;
            }
            return amount;
        }

        private static int SumCapacity(EntityQuery producerQuery)
        {
            using var producers = producerQuery.ToComponentDataArray<ElectricityProducer>(Allocator.Temp);
            var capacity = 0;
            foreach (var producer in producers)
                capacity += producer.m_Capacity;
            return capacity;
        }

        private static float Share(int part, int total)
        {
            return total > 0 ? (float)part / total : 0f;
        }
    }
}
