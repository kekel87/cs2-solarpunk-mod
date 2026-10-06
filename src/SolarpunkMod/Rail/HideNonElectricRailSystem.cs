using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Net;
using Game.Prefabs;
using SolarpunkMod.Station;
using Unity.Collections;
using Unity.Entities;

namespace SolarpunkMod.Rail
{
    /// <summary>
    /// Hides the non-electrified rail assets (Tigon's "No Cable", "Plain", "Non-electric", "NE" tracks
    /// and stations) from the toolbar: a solarpunk city builds electric rail only. Nothing is deleted
    /// and nothing is saved: placed ones keep working, and only the toolbar groups of prefabs change,
    /// which the game rebuilds on every launch. The Train tab gets an electric badge.
    /// </summary>
    public partial class HideNonElectricRailSystem : GameSystemBase
    {
        private static readonly Regex NonElectricName = new Regex(@"No Cables?|\bPlain\b|Non-[Ee]lectric|(?<![A-Z])NE\b");

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_ToolbarObjectQuery;
        private bool m_TrainTabMarked;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ToolbarObjectQuery = GetEntityQuery(ComponentType.ReadOnly<UIObjectData>(), ComponentType.ReadOnly<PrefabData>());
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (m_TrainTabMarked || !RailTabIcons.Available)
                return;

            var trainCategory = FindTrainCategory();
            if (trainCategory == Entity.Null)
                return;
            m_PrefabSystem.GetPrefab<UIAssetCategoryPrefab>(trainCategory).GetComponent<UIObject>().m_Icon = RailTabIcons.Electric;
            m_TrainTabMarked = true;
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            HideNonElectricAssets();
        }

        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// The toolbar tab holding most non-electric rail assets: the Train tab, found by data, not by
        /// name. Only rail assets vote, so a "Plain" or "NE" asset of another menu cannot win.
        /// </summary>
        private Entity FindTrainCategory()
        {
            var votes = new Dictionary<Entity, int>();
            using var objects = m_ToolbarObjectQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in objects)
            {
                var group = EntityManager.GetComponentData<UIObjectData>(entity).m_Group;
                if (group == Entity.Null
                    || !EntityManager.HasComponent<UIAssetCategoryData>(group)
                    || !IsNonElectricRail(entity))
                    continue;
                votes.TryGetValue(group, out var count);
                votes[group] = count + 1;
            }
            return votes.Count == 0 ? Entity.Null : votes.OrderByDescending(vote => vote.Value).First().Key;
        }

        /// <summary>A track for trains, or a building with a train stop, whose name says it has no catenary.</summary>
        private bool IsNonElectricRail(Entity prefab)
        {
            if (!NonElectricName.IsMatch(m_PrefabSystem.GetPrefabName(prefab)))
                return false;
            if (EntityManager.TryGetComponent<TrackData>(prefab, out var track))
                return (track.m_TrackType & TrackTypes.Train) != 0;
            return m_PrefabSystem.TryGetPrefab<PrefabBase>(prefab, out var prefabBase) && TrainStops.Has(prefabBase);
        }

        /// <summary>
        /// Idempotent, at every load. The prefab's own group is cleared too: the game puts an asset back
        /// in that group whenever it initializes the prefab again (UIObject.LateInitialize).
        /// </summary>
        private void HideNonElectricAssets()
        {
            var hidden = new List<string>();
            using var objects = m_ToolbarObjectQuery.ToEntityArray(Allocator.Temp);
            foreach (var asset in objects)
            {
                var uiObjectData = EntityManager.GetComponentData<UIObjectData>(asset);
                if (uiObjectData.m_Group == Entity.Null || !IsNonElectricRail(asset))
                    continue;

                var elements = EntityManager.GetBuffer<UIGroupElement>(uiObjectData.m_Group);
                for (var i = elements.Length - 1; i >= 0; i--)
                {
                    if (elements[i].m_Prefab == asset)
                        elements.RemoveAt(i);
                }
                uiObjectData.m_Group = Entity.Null;
                EntityManager.SetComponentData(asset, uiObjectData);
                if (m_PrefabSystem.TryGetPrefab<PrefabBase>(asset, out var prefab) && prefab.TryGet<UIObject>(out var uiObject))
                    uiObject.m_Group = null;
                hidden.Add(m_PrefabSystem.GetPrefabName(asset));
            }
            if (hidden.Count > 0)
                Mod.Log.Info($"Non-electric rail hidden from the toolbar ({hidden.Count}): {string.Join(", ", hidden)}");
        }
    }
}
