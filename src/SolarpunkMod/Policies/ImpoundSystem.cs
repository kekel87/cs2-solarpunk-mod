using Game;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// Tows away personal cars still parked on a closed street once the district's notice is over;
    /// the household loses the car. Also starts and ends each district's ban with the policy.
    /// </summary>
    public partial class ImpoundSystem : GameSystemBase
    {
        // Six hours on the in-game clock: a game day is also a month, so a realistic notice in days
        // would outlast whole neighbourhoods being built.
        private const uint GracePeriodFrames = TimeSystem.kTicksPerDay / 4;
        private const int UpdateIntervalInFrames = 4096;

        private EndResidentialParkingPolicySystem m_PolicySystem;
        private SimulationSystem m_SimulationSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityQuery m_BanQuery;
        private EntityQuery m_ParkedCarQuery;
        private StreetParking m_StreetParking;
        private ComponentLookup<ParkingLane> m_ParkingLaneLookup;
        private BufferLookup<LayoutElement> m_LayoutLookup;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => UpdateIntervalInFrames;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PolicySystem = World.GetOrCreateSystemManaged<EndResidentialParkingPolicySystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_BanQuery = GetEntityQuery(
                ComponentType.ReadOnly<StreetParkingBan>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_ParkedCarQuery = GetEntityQuery(
                ComponentType.ReadOnly<ParkedCar>(),
                ComponentType.ReadOnly<PersonalCar>(),
                ComponentType.Exclude<Bicycle>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_StreetParking = new StreetParking(this);
            m_ParkingLaneLookup = GetComponentLookup<ParkingLane>(true);
            m_LayoutLookup = GetBufferLookup<LayoutElement>(true);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
            using var bannedDistricts = m_PolicySystem.GetBannedDistricts(Allocator.Temp);
            using var districtsPastNotice = UpdateBans(bannedDistricts, commandBuffer);
            if (!districtsPastNotice.IsEmpty)
                TowCars(districtsPastNotice, commandBuffer);
        }

        private NativeHashSet<Entity> UpdateBans(NativeHashSet<Entity> bannedDistricts, EntityCommandBuffer commandBuffer)
        {
            var now = m_SimulationSystem.frameIndex;
            var districtsPastNotice = new NativeHashSet<Entity>(4, Allocator.Temp);

            using var districtsWithBan = m_BanQuery.ToEntityArray(Allocator.Temp);
            using var bans = m_BanQuery.ToComponentDataArray<StreetParkingBan>(Allocator.Temp);
            for (var i = 0; i < districtsWithBan.Length; i++)
            {
                if (!bannedDistricts.Contains(districtsWithBan[i]))
                {
                    commandBuffer.RemoveComponent<StreetParkingBan>(districtsWithBan[i]);
                    Mod.Log.Info($"Street parking ban lifted in district {districtsWithBan[i]}");
                }
                else if (now - bans[i].m_SinceFrame >= GracePeriodFrames)
                    districtsPastNotice.Add(districtsWithBan[i]);
            }

            foreach (var district in bannedDistricts)
            {
                if (!EntityManager.HasComponent<StreetParkingBan>(district))
                {
                    commandBuffer.AddComponent(district, new StreetParkingBan { m_SinceFrame = now });
                    Mod.Log.Info($"Street parking ban started in district {district}");
                }
            }
            return districtsPastNotice;
        }

        private void TowCars(NativeHashSet<Entity> districtsPastNotice, EntityCommandBuffer commandBuffer)
        {
            CompleteDependency();
            m_StreetParking.Update(this);
            m_ParkingLaneLookup.Update(this);
            m_LayoutLookup.Update(this);

            var towedCount = 0;
            using var towByLane = new NativeHashMap<Entity, bool>(64, Allocator.Temp);
            using var cars = m_ParkedCarQuery.ToEntityArray(Allocator.Temp);
            using var personalCars = m_ParkedCarQuery.ToComponentDataArray<PersonalCar>(Allocator.Temp);
            using var parkedCars = m_ParkedCarQuery.ToComponentDataArray<ParkedCar>(Allocator.Temp);
            for (var i = 0; i < cars.Length; i++)
            {
                // Someone heading for the car keeps it.
                if (personalCars[i].m_Keeper != Entity.Null)
                    continue;

                var lane = parkedCars[i].m_Lane;
                if (!towByLane.TryGetValue(lane, out var tow))
                {
                    tow = m_ParkingLaneLookup.TryGetComponent(lane, out var parkingLane)
                        && (parkingLane.m_Flags & ParkingLaneFlags.ParkingDisabled) != 0
                        && m_StreetParking.TryGetDistrict(lane, parkingLane, out var district)
                        && districtsPastNotice.Contains(district);
                    towByLane.Add(lane, tow);
                }
                if (!tow)
                    continue;

                m_LayoutLookup.TryGetBuffer(cars[i], out var layout);
                VehicleUtils.DeleteVehicle(commandBuffer, cars[i], layout);
                towedCount++;
            }

            if (towedCount > 0)
                Mod.Log.Info($"Impound: {towedCount} cars towed");
        }
    }
}
