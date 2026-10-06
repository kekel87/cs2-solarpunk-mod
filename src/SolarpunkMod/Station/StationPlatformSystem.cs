using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Unity.Entities;

namespace SolarpunkMod.Station
{
    /// <summary>
    /// The dedicated platforms of passenger train stations, cargo upgrades (CargoUpgradeCloner) that
    /// graft storage onto the passenger station, so one station serves a passenger line and cargo
    /// lines, each on its own platform.
    /// - Freight platform: the terminal's own traded resources.
    /// - Waste platform: garbage only, plus a garbage facility like Tigon's train garbage yards, so
    ///   garbage trucks unload there and garbage trains carry it away.
    /// Uninstalling: remove the platforms first. Without the mod, a placed platform loads as a missing
    /// object, but the station it was grafted onto keeps the storage components while its vanilla
    /// prefab no longer carries the data below, which the storage systems read unchecked: in a release
    /// build, a lookup with no safety checks may crash the game natively rather than throw.
    /// Never updated: it only hooks the game preload, so the prefabs exist before a save is read.
    /// </summary>
    public partial class StationPlatformSystem : GameSystemBase
    {
        public const string FreightPlatformName = "SolarpunkFreightPlatform";
        public const string WastePlatformName = "SolarpunkWastePlatform";

        private const uint FreightPlatformCost = 60000;
        private const uint WastePlatformCost = 60000;

        private PrefabSystem m_PrefabSystem;
        private CargoUpgradeCloner m_Cloner;
        private EntityQuery m_PassengerStationQuery;
        private bool m_Registered;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Cloner = new CargoUpgradeCloner(m_PrefabSystem, EntityManager);
            m_PassengerStationQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<PublicTransportStationData>(), ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<PrefabData>() },
                None = new[] { ComponentType.ReadOnly<ServiceUpgradeData>() },
            });
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (m_Registered)
                return;
            m_Registered = true;

            if (!m_Cloner.TryGetTemplate(out var template))
            {
                Mod.Log.Error($"Station platforms not available: no {CargoUpgradeCloner.TemplateName} prefab");
                return;
            }
            var stations = m_Cloner.FindHosts(m_PassengerStationQuery, station => TrainStops.Has(station, passengersOnly: true));
            if (stations.Length == 0)
            {
                Mod.Log.Warn("Station platforms not available: no passenger train station found");
                return;
            }

            var freight = m_Cloner.AddUpgrade(template, FreightPlatformName, FreightPlatformCost, stations, _ => { });
            var waste = m_Cloner.AddUpgrade(template, WastePlatformName, WastePlatformCost, stations, MakeWastePlatform);
            if (freight || waste)
                m_Cloner.GiveNeutralCargoData(template, stations);
            if (waste)
                GiveGarbagePrefabData(stations);
            Mod.Log.Info($"Station platforms offered on {stations.Length} passenger train stations");
        }

        /// <summary>Garbage only, with the collection and processing of Tigon's medium train garbage yard.</summary>
        private static void MakeWastePlatform(PrefabBase platform)
        {
            CargoUpgradeCloner.TradeGarbageOnly(platform);
            var garbageFacility = platform.AddComponent<GarbageFacility>();
            garbageFacility.m_GarbageCapacity = 500000;
            garbageFacility.m_VehicleCapacity = 5;
            garbageFacility.m_TransportCapacity = 1;
            garbageFacility.m_ProcessingSpeed = 25000;
        }

        /// <summary>
        /// Same reason as the neutral cargo data, for the garbage facility the waste platform grafts:
        /// GarbageFacilityAISystem indexes the station's own prefab data before adding its upgrades'.
        /// An empty one adds nothing.
        /// </summary>
        private void GiveGarbagePrefabData(BuildingPrefab[] stations)
        {
            foreach (var station in stations)
            {
                var entity = m_PrefabSystem.GetEntity(station);
                m_Cloner.AddIfMissing(entity, default(GarbageFacilityData));
            }
        }

        /// <summary>Stations with a passenger train stop among their sub-objects, vanilla and mods alike.</summary>
    }
}
