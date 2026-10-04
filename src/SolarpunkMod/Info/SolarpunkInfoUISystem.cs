using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Routes;
using Game.Tools;
using Game.UI;
using Game.Vehicles;
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

        private ComponentLookup<CurrentVehicle> m_CurrentVehicleLookup;
        private ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneLookup;
        private ComponentLookup<TaxiStand> m_TaxiStandLookup;
        private ComponentLookup<Bicycle> m_BicycleLookup;
        private ComponentLookup<PersonalCar> m_PersonalCarLookup;
        private ComponentLookup<Taxi> m_TaxiLookup;
        private ComponentLookup<PublicTransport> m_PublicTransportLookup;

        private ValueBinding<float> m_CarShareBinding;
        private ValueBinding<float> m_BicycleShareBinding;
        private ValueBinding<float> m_PublicTransportShareBinding;
        private ValueBinding<float> m_WalkingShareBinding;
        private ValueBinding<int> m_PersonalCarCountBinding;
        private ValueBinding<int> m_DeliveryTruckCountBinding;
        private ValueBinding<int> m_GarbageTruckCountBinding;
        private ValueBinding<int> m_CargoTrainCountBinding;
        private ValueBinding<float> m_RenewableElectricityShareBinding;

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

            m_CurrentVehicleLookup = GetComponentLookup<CurrentVehicle>(true);
            m_HumanCurrentLaneLookup = GetComponentLookup<HumanCurrentLane>(true);
            m_TaxiStandLookup = GetComponentLookup<TaxiStand>(true);
            m_BicycleLookup = GetComponentLookup<Bicycle>(true);
            m_PersonalCarLookup = GetComponentLookup<PersonalCar>(true);
            m_TaxiLookup = GetComponentLookup<Taxi>(true);
            m_PublicTransportLookup = GetComponentLookup<PublicTransport>(true);

            m_CarShareBinding = Bind("carShare", 0f);
            m_BicycleShareBinding = Bind("bicycleShare", 0f);
            m_PublicTransportShareBinding = Bind("publicTransportShare", 0f);
            m_WalkingShareBinding = Bind("walkingShare", 0f);
            m_PersonalCarCountBinding = Bind("personalCarCount", 0);
            m_DeliveryTruckCountBinding = Bind("deliveryTruckCount", 0);
            m_GarbageTruckCountBinding = Bind("garbageTruckCount", 0);
            m_CargoTrainCountBinding = Bind("cargoTrainCount", 0);
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
            m_DeliveryTruckCountBinding.Update(Count(m_DeliveryTruckQuery, (DeliveryTruck truck) => (truck.m_State & DeliveryTruckFlags.DummyTraffic) == 0));
            m_GarbageTruckCountBinding.Update(m_GarbageTruckQuery.CalculateEntityCount());
            m_CargoTrainCountBinding.Update(CountCargoTrains());
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

            var travellerCount = total.TravellerCount;
            m_CarShareBinding.Update(Share(total.Car, travellerCount));
            m_BicycleShareBinding.Update(Share(total.Bicycle, travellerCount));
            m_PublicTransportShareBinding.Update(Share(total.PublicTransport, travellerCount));
            m_WalkingShareBinding.Update(Share(total.Walking, travellerCount));
        }

        private ModalCounts SampleModalCounts()
        {
            m_CurrentVehicleLookup.Update(this);
            m_HumanCurrentLaneLookup.Update(this);
            m_TaxiStandLookup.Update(this);
            m_BicycleLookup.Update(this);
            m_PersonalCarLookup.Update(this);
            m_TaxiLookup.Update(this);
            m_PublicTransportLookup.Update(this);

            var counts = new ModalCounts();
            using var entities = m_ResidentQuery.ToEntityArray(Allocator.Temp);
            using var residents = m_ResidentQuery.ToComponentDataArray<Resident>(Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
            {
                var flags = residents[i].m_Flags;
                if ((flags & (ResidentFlags.DummyTraffic | ResidentFlags.Hangaround)) != 0)
                    continue;

                if (!m_CurrentVehicleLookup.TryGetComponent(entities[i], out var currentVehicle))
                {
                    if ((flags & ResidentFlags.WaitingTransport) == 0)
                        counts.Walking++;
                    else if (IsWaitingForTaxi(entities[i]))
                        counts.Car++;
                    else
                        counts.PublicTransport++;
                    continue;
                }

                var vehicle = currentVehicle.m_Vehicle;
                if (m_BicycleLookup.HasComponent(vehicle))
                    counts.Bicycle++;
                else if (m_PersonalCarLookup.HasComponent(vehicle) || m_TaxiLookup.HasComponent(vehicle))
                    counts.Car++;
                else if (m_PublicTransportLookup.HasComponent(vehicle))
                    counts.PublicTransport++;
            }

            return counts;
        }

        private bool IsWaitingForTaxi(Entity resident)
        {
            return m_HumanCurrentLaneLookup.TryGetComponent(resident, out var currentLane)
                && m_TaxiStandLookup.HasComponent(currentLane.m_QueueEntity);
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

        private int CountCargoTrains()
        {
            using var entities = m_CargoTrainQuery.ToEntityArray(Allocator.Temp);
            using var controllers = m_CargoTrainQuery.ToComponentDataArray<Controller>(Allocator.Temp);
            using var cargoTransports = m_CargoTrainQuery.ToComponentDataArray<CargoTransport>(Allocator.Temp);
            var count = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                var isLeadingVehicle = controllers[i].m_Controller == entities[i];
                var isThroughTraffic = (cargoTransports[i].m_State & CargoTransportFlags.DummyTraffic) != 0;
                if (isLeadingVehicle && !isThroughTraffic)
                    count++;
            }
            return count;
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

        private struct ModalCounts
        {
            public int Car;
            public int Bicycle;
            public int PublicTransport;
            public int Walking;

            public int TravellerCount => Car + Bicycle + PublicTransport + Walking;

            public void Add(ModalCounts other)
            {
                Car += other.Car;
                Bicycle += other.Bicycle;
                PublicTransport += other.PublicTransport;
                Walking += other.Walking;
            }
        }
    }
}
