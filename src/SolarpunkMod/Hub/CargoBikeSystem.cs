using Colossal.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Hub
{
    /// <summary>
    /// Rides the hub's cargo bikes like bicycles. Ordered just before DeliveryTruckAISystem, whose
    /// road-only path request (FindPathIfNeeded, in Burst) only runs when a new path is required: this
    /// system asks for a bicycle path first, so the game's request is skipped. A bike with no bicycle
    /// path to the shop drops its Bicycle tag and finishes as a van; a bike done delivering is removed,
    /// as if back at the hub (the game would send it back by road).
    /// Like DeliveryTruckAISystem, it only looks at the sixteenth of the trucks whose UpdateFrame comes
    /// up this frame: the very trucks the game is about to process, and no sync on the other frames.
    /// Saves: only the game's own components are used, but the Bicycle tag is saved with a bike on its
    /// way; without the mod, such a bike is driven as a bicycle while the game asks it road paths, and
    /// may recompute its path until it gives up.
    /// </summary>
    public partial class CargoBikeSystem : GameSystemBase
    {
        private const float WalkSpeed = 5.555556f;
        private const float RandomCost = 30f;
        private const int MaxPathfindDelayFrames = 64;

        private const uint UpdateFrameCount = 16;

        private SimulationSystem m_SimulationSystem;
        private PathfindSetupSystem m_PathfindSetupSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityQuery m_CargoBikeQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_PathfindSetupSystem = World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            // Only the hub's cargo bikes are delivery trucks with the Bicycle tag.
            m_CargoBikeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Game.Vehicles.DeliveryTruck>(),
                    ComponentType.ReadOnly<Bicycle>(),
                    ComponentType.ReadOnly<PathOwner>(),
                    ComponentType.ReadOnly<CarCurrentLane>(),
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<UpdateFrame>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Game.Objects.TripSource>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                },
            });
            RequireForUpdate(m_CargoBikeQuery);
        }

        protected override void OnUpdate()
        {
            m_CargoBikeQuery.ResetFilter();
            m_CargoBikeQuery.SetSharedComponentFilter(new UpdateFrame(m_SimulationSystem.frameIndex % UpdateFrameCount));
            if (m_CargoBikeQuery.IsEmpty)
                return;

            // Paths and truck states are written by simulation jobs.
            CompleteDependency();
            var commandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
            var pathfindQueue = default(NativeQueue<SetupQueueItem>);

            using var bikes = m_CargoBikeQuery.ToEntityArray(Allocator.Temp);
            foreach (var bike in bikes)
            {
                var truck = EntityManager.GetComponentData<Game.Vehicles.DeliveryTruck>(bike);
                var pathOwner = EntityManager.GetComponentData<PathOwner>(bike);

                if ((truck.m_State & DeliveryTruckFlags.Returning) != 0)
                {
                    EntityManager.TryGetBuffer<LayoutElement>(bike, true, out var layout);
                    VehicleUtils.DeleteVehicle(commandBuffer, bike, layout);
                    continue;
                }

                if ((pathOwner.m_State & PathFlags.Failed) != 0)
                {
                    // No bicycle path: back to a van, whose road path the game requests itself.
                    pathOwner.m_State &= ~PathFlags.Failed;
                    pathOwner.m_State |= PathFlags.Obsolete;
                    EntityManager.SetComponentData(bike, pathOwner);
                    commandBuffer.RemoveComponent<Bicycle>(bike);
                    continue;
                }

                if (!VehicleUtils.RequireNewPath(pathOwner))
                    continue;

                if (!pathfindQueue.IsCreated)
                    pathfindQueue = m_PathfindSetupSystem.GetQueue(this, MaxPathfindDelayFrames);
                var currentLane = EntityManager.GetComponentData<CarCurrentLane>(bike);
                var target = EntityManager.GetComponentData<Target>(bike).m_Target;
                var carData = EntityManager.GetComponentData<CarData>(EntityManager.GetComponentData<PrefabRef>(bike).m_Prefab);
                VehicleUtils.SetupPathfind(ref currentLane, ref pathOwner, pathfindQueue.AsParallelWriter(), BicyclePath(bike, target, carData));
                EntityManager.SetComponentData(bike, currentLane);
                EntityManager.SetComponentData(bike, pathOwner);
            }
        }

        /// <summary>A bicycle's path, as PersonalCarAISystem asks for one, ending at the shop instead of a bike rack.</summary>
        private static SetupQueueItem BicyclePath(Entity bike, Entity target, CarData carData)
        {
            var parameters = new PathfindParameters
            {
                m_MaxSpeed = carData.m_MaxSpeed,
                m_WalkSpeed = WalkSpeed,
                m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
                m_Methods = PathMethod.Bicycle,
                m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRulesBicycleDefaults(),
            };
            var origin = new SetupQueueTarget
            {
                m_Type = SetupTargetType.CurrentLocation,
                m_Methods = PathMethod.Bicycle,
                m_RoadTypes = RoadTypes.Bicycle,
                m_RandomCost = RandomCost,
            };
            var destination = new SetupQueueTarget
            {
                m_Type = SetupTargetType.CurrentLocation,
                m_Methods = PathMethod.Bicycle,
                m_RoadTypes = RoadTypes.Bicycle,
                m_Entity = target,
                m_RandomCost = RandomCost,
            };
            return new SetupQueueItem(bike, parameters, origin, destination);
        }
    }
}
