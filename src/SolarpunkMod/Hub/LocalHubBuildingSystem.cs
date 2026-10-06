using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SolarpunkMod.Hub
{
    /// <summary>
    /// The Local Hub buildings, train only: small on the surface, small underground and large urban
    /// (underground track). With our asset pack and Tigon's Rail Infrastructure, each one is Tigon's
    /// compact cargo train station dressed in our model, its track moved under our quay; without them
    /// the small hub falls back to a copy of the vanilla cargo train terminal, and the other two are
    /// not offered. Placed hubs keep their prefab name: without the mod they load as missing objects.
    /// Never updated: it only hooks the game preload, so the prefabs exist before a save is read.
    /// </summary>
    public partial class LocalHubBuildingSystem : GameSystemBase
    {
        public const string PrefabName = "SolarpunkLocalHub";
        public const string UndergroundPrefabName = "SolarpunkLocalHubUnderground";
        public const string LargePrefabName = "SolarpunkLocalHubLarge";

        private const string TerminalTemplateName = "CargoTrainTerminal01";
        // Tigon's Rail Infrastructure (optional): the only compact cargo train station, and a station
        // whose track runs in a tunnel.
        private const string CompactTemplateName = "Mini Train Oil Station";
        private const string TunnelTemplateName = "TO_UndergroundRailStation01";
        // Vanilla icon, so the hub does not look like the cargo terminal next to it in the toolbar.
        private const string Icon = "Media/Game/Icons/DeliveryVan.svg";
        private const float TunnelDepth = -20f;

        // Track positions follow assets/buildings/README.md (game z = − Blender y).
        private static readonly HubVariant Small = new HubVariant(PrefabName, "SolarpunkLocalHub01", 4, 5, -14f, 20f, false, 1, 0);
        private static readonly HubVariant Underground = new HubVariant(UndergroundPrefabName, "SolarpunkLocalHubUnderground01", 3, 4, 0f, 40f, true, 1, 1);
        private static readonly HubVariant Large = new HubVariant(LargePrefabName, "SolarpunkLocalHubLarge01", 4, 4, 5f, 40f, true, 2, 2);

        private PrefabSystem m_PrefabSystem;
        private readonly List<PrefabBase> m_Hubs = new List<PrefabBase>();

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnUpdate()
        {
        }

        /// <summary>Whether a building prefab is one of the Local Hubs.</summary>
        public bool IsHub(Entity prefab)
        {
            foreach (var hub in m_Hubs)
            {
                if (m_PrefabSystem.TryGetEntity(hub, out var entity) && entity == prefab)
                    return true;
            }
            return false;
        }

        public bool AnyHub => m_Hubs.Count > 0;

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (m_Hubs.Count > 0)
                return;

            var buildings = FindBuildings(CompactTemplateName, TunnelTemplateName,
                Small.ModelName, Underground.ModelName, Large.ModelName);
            buildings.TryGetValue(CompactTemplateName, out var compact);
            buildings.TryGetValue(TunnelTemplateName, out var tunnel);
            var tunnelTrack = tunnel != null ? FindTrack(tunnel, underground: true) : null;
            m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(BuildingPrefab), TerminalTemplateName), out var terminal);

            Register(TryBuild(Small, compact, tunnelTrack, terminal, buildings) ?? BuildFromTerminal(terminal));
            Register(TryBuild(Underground, compact, tunnelTrack, terminal, buildings));
            Register(TryBuild(Large, compact, tunnelTrack, terminal, buildings));
        }

        /// <summary>Building prefabs by name: imported assets cannot be found by PrefabID, which also compares their asset hash.</summary>
        private Dictionary<string, PrefabBase> FindBuildings(params string[] names)
        {
            var found = new Dictionary<string, PrefabBase>();
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<PrefabData>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var name = m_PrefabSystem.GetPrefabName(entity);
                if (names.Contains(name) && !found.ContainsKey(name))
                    found[name] = m_PrefabSystem.GetPrefab<PrefabBase>(entity);
            }
            return found;
        }

        private PrefabBase TryBuild(HubVariant variant, PrefabBase compact, ObjectSubNetInfo tunnelTrack, PrefabBase terminal,
            Dictionary<string, PrefabBase> buildings)
        {
            var missing = new List<string>();
            if (compact == null)
                missing.Add($"{CompactTemplateName} (Tigon's Rail Infrastructure)");
            if (!buildings.TryGetValue(variant.ModelName, out var model))
                missing.Add($"{variant.ModelName} (Solarpunk asset pack)");
            if (variant.IsUnderground && tunnelTrack == null)
                missing.Add($"{TunnelTemplateName} tunnel track (Tigon's Rail Infrastructure)");
            if (missing.Count > 0)
            {
                Mod.Log.Info($"Local Hub: {variant.PrefabName} not built, missing {string.Join(", ", missing)}");
                return null;
            }

            var templateTrack = FindTrack(compact, underground: false);
            if (templateTrack == null)
            {
                Mod.Log.Warn($"Local Hub: {variant.PrefabName} not built, no train track in {CompactTemplateName}");
                return null;
            }

            var hub = compact.Clone(variant.PrefabName);
            hub.Remove<ObsoleteIdentifiers>();
            hub.Remove<AssetPackItem>();
            hub.Remove<ObjectSubAreas>();
            hub.Remove<BuildingTerraformOverride>();
            var building = (BuildingPrefab)hub;
            building.m_Meshes = ((ObjectGeometryPrefab)model).m_Meshes;
            building.m_LotWidth = variant.LotWidth;
            building.m_LotDepth = variant.LotDepth;
            StockShopGoods(hub, variant, terminal);
            DressUp(hub, variant);

            var trackY = variant.IsUnderground ? TunnelDepth : 0f;
            MoveTrack(hub, variant, variant.IsUnderground ? tunnelTrack : templateTrack, trackY);
            var stopOffset = new float3(0f, trackY, variant.TrackZ - templateTrack.m_BezierCurve.a.z);
            var kept = KeepFunctionalSubObjects(hub, stopOffset);
            Mod.Log.Info($"Local Hub: {variant.PrefabName} built from {CompactTemplateName} + {variant.ModelName}, " +
                $"track at z={variant.TrackZ} y={trackY}, kept sub-objects: {string.Join(", ", kept)}");
            return hub;
        }

        /// <summary>
        /// Tigon's compact station only trades oil: give the hub the goods and storage of the vanilla
        /// cargo train terminal, which shops can use.
        /// </summary>
        private static void StockShopGoods(PrefabBase hub, HubVariant variant, PrefabBase terminal)
        {
            if (terminal == null
                || !terminal.TryGet<CargoTransportStation>(out var terminalStation)
                || !hub.TryGet<CargoTransportStation>(out var hubStation))
            {
                Mod.Log.Warn($"Local Hub: {variant.PrefabName} keeps {CompactTemplateName}'s goods, no {TerminalTemplateName} to copy from");
                return;
            }
            hubStation.m_TradedResources = (ResourceInEditor[])terminalStation.m_TradedResources.Clone();
            if (terminal.TryGet<StorageLimit>(out var terminalStorage) && hub.TryGet<StorageLimit>(out var hubStorage))
                hubStorage.storageLimit = terminalStorage.storageLimit * variant.StorageFactor;
        }

        /// <summary>The template's train track, the one in a tunnel or the one on the surface.</summary>
        private static ObjectSubNetInfo FindTrack(PrefabBase template, bool underground)
        {
            if (!template.TryGet<ObjectSubNets>(out var subNets) || subNets.m_SubNets == null)
                return null;
            return subNets.m_SubNets.FirstOrDefault(subNet =>
                subNet.m_NetPrefab is TrackPrefab track && track.m_TrackType == Game.Net.TrackTypes.Train
                && subNet.m_BezierCurve.a.y < TunnelDepth / 2 == underground);
        }

        /// <summary>
        /// Puts the train track straight along our quay, past the lot on both sides to connect it;
        /// an underground hub takes the tunnel track (and its required net pieces) instead.
        /// </summary>
        private static void MoveTrack(PrefabBase hub, HubVariant variant, ObjectSubNetInfo trackSource, float trackY)
        {
            var subNets = hub.GetComponent<ObjectSubNets>();
            foreach (var subNet in subNets.m_SubNets)
            {
                if (!(subNet.m_NetPrefab is TrackPrefab track) || track.m_TrackType != Game.Net.TrackTypes.Train)
                    continue;
                var start = new float3(-variant.TrackHalfLength, trackY, variant.TrackZ);
                var end = new float3(variant.TrackHalfLength, trackY, variant.TrackZ);
                subNet.m_NetPrefab = trackSource.m_NetPrefab;
                subNet.m_Upgrades = trackSource.m_Upgrades;
                subNet.m_BezierCurve = new Bezier4x3(start, math.lerp(start, end, 1f / 3f), math.lerp(start, end, 2f / 3f), end);
            }
        }

        /// <summary>
        /// Keeps the transport stops (moved along with the track) and the markers vehicles use; drops
        /// the template's decor, which belongs to its own model. Returns what was kept, for the log.
        /// </summary>
        private static List<string> KeepFunctionalSubObjects(PrefabBase hub, float3 stopOffset)
        {
            var kept = new List<string>();
            if (!hub.TryGet<ObjectSubObjects>(out var subObjects) || subObjects.m_SubObjects == null)
                return kept;
            var functional = new List<ObjectSubObjectInfo>();
            foreach (var subObject in subObjects.m_SubObjects)
            {
                var isStop = subObject.m_Object.Has<TransportStop>();
                if (!isStop && !(subObject.m_Object is MarkerObjectPrefab))
                    continue;
                if (isStop)
                    subObject.m_Position += stopOffset;
                functional.Add(subObject);
                kept.Add(subObject.m_Object.name);
            }
            subObjects.m_SubObjects = functional.ToArray();
            return kept;
        }

        private static void DressUp(PrefabBase hub, HubVariant variant)
        {
            if (!hub.TryGet<UIObject>(out var uiObject))
                return;
            uiObject.m_Icon = Icon;
            uiObject.m_Priority += variant.ToolbarOrder;
        }

        /// <summary>The fallback small hub: a copy of the vanilla cargo train terminal, as before our model.</summary>
        private static PrefabBase BuildFromTerminal(PrefabBase terminal)
        {
            if (terminal == null)
            {
                Mod.Log.Error($"Local Hub not available: no {TerminalTemplateName} prefab");
                return null;
            }
            var hub = terminal.Clone(PrefabName);
            hub.Remove<ObsoleteIdentifiers>();
            DressUp(hub, Small);
            Mod.Log.Info($"Local Hub: {PrefabName} uses the {TerminalTemplateName} model");
            return hub;
        }

        private void Register(PrefabBase hub)
        {
            if (hub == null)
                return;
            // DuplicatePrefab ignores AddPrefab's result: clone and register by hand instead.
            if (m_PrefabSystem.AddPrefab(hub))
                m_Hubs.Add(hub);
            else
                Mod.Log.Error($"Could not register the {hub.name} building");
        }

        private sealed class HubVariant
        {
            public readonly string PrefabName;
            public readonly string ModelName;
            public readonly int LotWidth;
            public readonly int LotDepth;
            public readonly float TrackZ;
            public readonly float TrackHalfLength;
            public readonly bool IsUnderground;
            public readonly int StorageFactor;
            // Added to the toolbar priority, to list the hubs from the smallest.
            public readonly int ToolbarOrder;

            public HubVariant(string prefabName, string modelName, int lotWidth, int lotDepth, float trackZ,
                float trackHalfLength, bool isUnderground, int storageFactor, int toolbarOrder)
            {
                PrefabName = prefabName;
                ModelName = modelName;
                LotWidth = lotWidth;
                LotDepth = lotDepth;
                TrackZ = trackZ;
                TrackHalfLength = trackHalfLength;
                IsUnderground = isUnderground;
                StorageFactor = storageFactor;
                ToolbarOrder = toolbarOrder;
            }
        }
    }
}
