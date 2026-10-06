using Game.Common;
using Game.Creatures;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace SolarpunkMod.Info
{
    internal enum TravelMode
    {
        None,
        Car,
        Bicycle,
        PublicTransport,
        Walking,
    }

    /// <summary>
    /// How a resident is travelling right now, shared by the city panel and the district section.
    /// Waiting for a taxi counts as car; through traffic and people hanging around are not travellers.
    /// </summary>
    internal class TravelModeClassifier
    {
        private ComponentLookup<CurrentVehicle> m_CurrentVehicleLookup;
        private ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneLookup;
        private ComponentLookup<TaxiStand> m_TaxiStandLookup;
        private ComponentLookup<Bicycle> m_BicycleLookup;
        private ComponentLookup<PersonalCar> m_PersonalCarLookup;
        private ComponentLookup<Taxi> m_TaxiLookup;
        private ComponentLookup<PublicTransport> m_PublicTransportLookup;

        public TravelModeClassifier(SystemBase system)
        {
            m_CurrentVehicleLookup = system.GetComponentLookup<CurrentVehicle>(true);
            m_HumanCurrentLaneLookup = system.GetComponentLookup<HumanCurrentLane>(true);
            m_TaxiStandLookup = system.GetComponentLookup<TaxiStand>(true);
            m_BicycleLookup = system.GetComponentLookup<Bicycle>(true);
            m_PersonalCarLookup = system.GetComponentLookup<PersonalCar>(true);
            m_TaxiLookup = system.GetComponentLookup<Taxi>(true);
            m_PublicTransportLookup = system.GetComponentLookup<PublicTransport>(true);
        }

        public void Update(SystemBase system)
        {
            m_CurrentVehicleLookup.Update(system);
            m_HumanCurrentLaneLookup.Update(system);
            m_TaxiStandLookup.Update(system);
            m_BicycleLookup.Update(system);
            m_PersonalCarLookup.Update(system);
            m_TaxiLookup.Update(system);
            m_PublicTransportLookup.Update(system);
        }

        /// <param name="vehicle">The vehicle the resident is in, or Entity.Null when on foot.</param>
        public TravelMode Classify(Entity resident, ResidentFlags flags, out Entity vehicle)
        {
            vehicle = Entity.Null;
            if ((flags & (ResidentFlags.DummyTraffic | ResidentFlags.Hangaround)) != 0)
                return TravelMode.None;

            if (!m_CurrentVehicleLookup.TryGetComponent(resident, out var currentVehicle))
            {
                if ((flags & ResidentFlags.WaitingTransport) == 0)
                    return TravelMode.Walking;
                return IsWaitingForTaxi(resident) ? TravelMode.Car : TravelMode.PublicTransport;
            }

            vehicle = currentVehicle.m_Vehicle;
            if (m_BicycleLookup.HasComponent(vehicle))
                return TravelMode.Bicycle;
            if (m_PersonalCarLookup.HasComponent(vehicle) || m_TaxiLookup.HasComponent(vehicle))
                return TravelMode.Car;
            if (m_PublicTransportLookup.HasComponent(vehicle))
                return TravelMode.PublicTransport;
            return TravelMode.None;
        }

        private bool IsWaitingForTaxi(Entity resident)
        {
            return m_HumanCurrentLaneLookup.TryGetComponent(resident, out var currentLane)
                && m_TaxiStandLookup.HasComponent(currentLane.m_QueueEntity);
        }
    }

    internal struct ModalCounts
    {
        public int Car;
        public int Bicycle;
        public int PublicTransport;
        public int Walking;

        public int TravellerCount => Car + Bicycle + PublicTransport + Walking;

        public void Count(TravelMode mode)
        {
            switch (mode)
            {
                case TravelMode.Car: Car++; break;
                case TravelMode.Bicycle: Bicycle++; break;
                case TravelMode.PublicTransport: PublicTransport++; break;
                case TravelMode.Walking: Walking++; break;
            }
        }

        public void Add(ModalCounts other)
        {
            Car += other.Car;
            Bicycle += other.Bicycle;
            PublicTransport += other.PublicTransport;
            Walking += other.Walking;
        }

        public float ShareOf(int part) => TravellerCount > 0 ? (float)part / TravellerCount : 0f;
    }
}
