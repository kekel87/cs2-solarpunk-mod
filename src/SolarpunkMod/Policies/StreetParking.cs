using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using ParkingLane = Game.Net.ParkingLane;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// Street parking lanes covered by the "End residential parking" policy. Holds the lookups a
    /// system refreshes once per update, before reading lanes on the main thread.
    /// </summary>
    public struct StreetParking
    {
        private ComponentLookup<PrefabRef> m_PrefabRefLookup;
        private ComponentLookup<ParkingLaneData> m_ParkingLaneDataLookup;
        private ComponentLookup<Owner> m_OwnerLookup;
        private ComponentLookup<BorderDistrict> m_BorderDistrictLookup;
        private ComponentLookup<Building> m_BuildingLookup;

        public StreetParking(SystemBase system)
        {
            m_PrefabRefLookup = system.GetComponentLookup<PrefabRef>(true);
            m_ParkingLaneDataLookup = system.GetComponentLookup<ParkingLaneData>(true);
            m_OwnerLookup = system.GetComponentLookup<Owner>(true);
            m_BorderDistrictLookup = system.GetComponentLookup<BorderDistrict>(true);
            m_BuildingLookup = system.GetComponentLookup<Building>(true);
        }

        public void Update(SystemBase system)
        {
            m_PrefabRefLookup.Update(system);
            m_ParkingLaneDataLookup.Update(system);
            m_OwnerLookup.Update(system);
            m_BorderDistrictLookup.Update(system);
            m_BuildingLookup.Update(system);
        }

        /// <summary>
        /// The district on the lane's side of the road, for a car parking lane along a road;
        /// false for building lots and their aisles, bicycle racks, taxi stands and virtual lanes.
        /// </summary>
        public bool TryGetDistrict(Entity lane, ParkingLane parkingLane, out Entity district)
        {
            district = Entity.Null;
            if ((parkingLane.m_Flags & (ParkingLaneFlags.VirtualLane | ParkingLaneFlags.SpecialVehicles)) != 0)
                return false;
            if (!m_PrefabRefLookup.TryGetComponent(lane, out var prefabRef)
                || !m_ParkingLaneDataLookup.TryGetComponent(prefabRef.m_Prefab, out var parkingLaneData)
                || (parkingLaneData.m_RoadTypes & RoadTypes.Car) == 0)
                return false;
            if (!m_OwnerLookup.TryGetComponent(lane, out var owner)
                || !m_BorderDistrictLookup.TryGetComponent(owner.m_Owner, out var borderDistrict)
                || IsInsideBuilding(owner.m_Owner))
                return false;

            // Same side rule as the game's ParkingLaneDataSystem.GetParkingStats.
            district = (parkingLane.m_Flags & ParkingLaneFlags.RightSide) != 0 ? borderDistrict.m_Right : borderDistrict.m_Left;
            return district != Entity.Null;
        }

        /// <summary>
        /// A road belonging to a building, such as the aisles of a parking lot, is private parking.
        /// </summary>
        private bool IsInsideBuilding(Entity road)
        {
            var entity = road;
            while (m_OwnerLookup.TryGetComponent(entity, out var owner))
            {
                entity = owner.m_Owner;
                if (m_BuildingLookup.HasComponent(entity))
                    return true;
            }
            return false;
        }
    }
}
