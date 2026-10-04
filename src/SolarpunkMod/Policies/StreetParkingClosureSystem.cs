using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// Closes street parking in districts with the "End residential parking" policy. Runs right after
    /// the game's ParkingLaneDataSystem, which recomputes ParkingDisabled on every updated lane, and
    /// before LanesModifiedSystem hands the lanes to the pathfinder. Stores nothing: the closure is
    /// derived again from the district policy on each lane update.
    /// </summary>
    public partial class StreetParkingClosureSystem : GameSystemBase
    {
        private EndResidentialParkingPolicySystem m_PolicySystem;
        private EntityQuery m_UpdatedParkingLaneQuery;
        private StreetParking m_StreetParking;
        private ComponentLookup<ParkingLane> m_ParkingLaneLookup;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PolicySystem = World.GetOrCreateSystemManaged<EndResidentialParkingPolicySystem>();
            m_UpdatedParkingLaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<ParkingLane>() },
                Any = new[] { ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<PathfindUpdated>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() },
            });
            m_StreetParking = new StreetParking(this);
            m_ParkingLaneLookup = GetComponentLookup<ParkingLane>();
            RequireForUpdate(m_UpdatedParkingLaneQuery);
        }

        protected override void OnUpdate()
        {
            // Lanes get updated by every car parking or leaving: leave before waiting on the
            // game's lane job when no district has the policy.
            using var bannedDistricts = m_PolicySystem.GetBannedDistricts(Allocator.Temp);
            if (bannedDistricts.IsEmpty)
                return;

            CompleteDependency();
            m_StreetParking.Update(this);
            m_ParkingLaneLookup.Update(this);

            using var lanes = m_UpdatedParkingLaneQuery.ToEntityArray(Allocator.Temp);
            foreach (var lane in lanes)
            {
                var parkingLane = m_ParkingLaneLookup[lane];
                if (!m_StreetParking.TryGetDistrict(lane, parkingLane, out var district) || !bannedDistricts.Contains(district))
                    continue;

                parkingLane.m_Flags |= ParkingLaneFlags.ParkingDisabled;
                m_ParkingLaneLookup[lane] = parkingLane;
            }
        }
    }
}
