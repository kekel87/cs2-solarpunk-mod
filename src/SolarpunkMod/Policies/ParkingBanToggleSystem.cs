using Game;
using Game.Common;
using Game.Policies;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// Lifts a district's ban the moment the "End residential parking" policy is switched off.
    /// ImpoundSystem only looks every 4096 frames, so an off-and-on toggle between two of its passes
    /// would otherwise keep the old notice; with the ban gone, its next pass starts a fresh one.
    /// </summary>
    public partial class ParkingBanToggleSystem : GameSystemBase
    {
        private EndResidentialParkingPolicySystem m_PolicySystem;
        private ModificationBarrier4 m_ModificationBarrier;
        private EntityQuery m_PolicyEventQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PolicySystem = World.GetOrCreateSystemManaged<EndResidentialParkingPolicySystem>();
            m_ModificationBarrier = World.GetOrCreateSystemManaged<ModificationBarrier4>();
            m_PolicyEventQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Modify>());
            RequireForUpdate(m_PolicyEventQuery);
        }

        protected override void OnUpdate()
        {
            var policy = m_PolicySystem.PolicyEntity;
            if (policy == Entity.Null)
                return;

            var commandBuffer = m_ModificationBarrier.CreateCommandBuffer();
            using var modifications = m_PolicyEventQuery.ToComponentDataArray<Modify>(Allocator.Temp);
            foreach (var modify in modifications)
            {
                if (modify.m_Policy != policy || (modify.m_Flags & PolicyFlags.Active) != 0
                    || !EntityManager.HasComponent<StreetParkingBan>(modify.m_Entity))
                    continue;

                commandBuffer.RemoveComponent<StreetParkingBan>(modify.m_Entity);
                Mod.Log.Info($"Street parking ban lifted in district {modify.m_Entity}");
            }
        }
    }
}
