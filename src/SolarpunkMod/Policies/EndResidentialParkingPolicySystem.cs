using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Game.Common;
using Game.Policies;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// Registers the "End residential parking" district policy and tells whether a district has it.
    /// Never updated: it only hooks the game preload, so the prefab exists before a save's policy
    /// references are resolved.
    /// </summary>
    public partial class EndResidentialParkingPolicySystem : GameSystemBase
    {
        public const string PolicyName = "SolarpunkEndResidentialParking";

        private const string Icon = "Media/Game/Policies/LotParkingFee.svg";
        private const float CarReserveProbabilityChange = -0.5f;

        private PrefabSystem m_PrefabSystem;
        private PolicyTogglePrefab m_Prefab;
        private EntityQuery m_DistrictQuery;

        private Entity PolicyEntity =>
            m_Prefab != null && m_PrefabSystem.TryGetEntity(m_Prefab, out var entity) ? entity : Entity.Null;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadOnly<Policy>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (m_Prefab != null)
                return;

            var prefab = ScriptableObject.CreateInstance<PolicyTogglePrefab>();
            prefab.name = PolicyName;
            prefab.m_Category = PolicyCategory.Traffic;
            prefab.AddComponent<UIObject>().m_Icon = Icon;
            prefab.AddComponent<DistrictModifiers>().m_Modifiers = new[]
            {
                Modifier(DistrictModifierType.CarReserveProbability, ModifierValueMode.Relative, CarReserveProbabilityChange),
                // No effect (+0, and fees only apply under Paid Parking): its presence makes the
                // game's LanePoliciesSystem refresh the district's parking lanes when the policy toggles.
                Modifier(DistrictModifierType.ParkingFee, ModifierValueMode.Absolute, 0f),
            };

            if (!m_PrefabSystem.AddPrefab(prefab))
            {
                Mod.Log.Error($"Could not register the {PolicyName} policy prefab");
                return;
            }
            m_Prefab = prefab;
        }

        /// <summary>Districts where the policy is active.</summary>
        public NativeHashSet<Entity> GetBannedDistricts(Allocator allocator)
        {
            var bannedDistricts = new NativeHashSet<Entity>(4, allocator);
            var policy = PolicyEntity;
            if (policy == Entity.Null)
                return bannedDistricts;

            using var districts = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            foreach (var district in districts)
            {
                foreach (var districtPolicy in EntityManager.GetBuffer<Policy>(district, true))
                {
                    if (districtPolicy.m_Policy == policy && (districtPolicy.m_Flags & PolicyFlags.Active) != 0)
                        bannedDistricts.Add(district);
                }
            }
            return bannedDistricts;
        }

        private static DistrictModifierInfo Modifier(DistrictModifierType type, ModifierValueMode mode, float value) =>
            new DistrictModifierInfo { m_Type = type, m_Mode = mode, m_Range = new Bounds1(value, value) };
    }
}
