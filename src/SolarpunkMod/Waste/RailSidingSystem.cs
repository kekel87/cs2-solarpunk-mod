using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using SolarpunkMod.Station;
using Unity.Entities;

namespace SolarpunkMod.Waste
{
    /// <summary>
    /// The "Rail siding" upgrade of garbage facilities (incinerators, landfills, any vanilla or mod
    /// building with a garbage facility and no cargo station of its own): a cargo upgrade
    /// (CargoUpgradeCloner) trading garbage only, so garbage trains from a train garbage yard or a
    /// station's waste platform unload straight into the facility, which processes it like the garbage
    /// its trucks bring. Garbage transfers already path through cargo lines
    /// (GarbageTransferDispatchSystem) and only target buildings with a garbage facility; the storage
    /// and the garbage share the building's Resources buffer, like Tigon's train garbage yards.
    /// Uninstalling: remove the sidings first. Without the mod, a placed siding loads as a missing
    /// object, but the facility it was grafted onto keeps the storage components while its vanilla
    /// prefab no longer carries the neutral cargo data, which the storage systems read unchecked: in a
    /// release build, a lookup with no safety checks may crash the game natively rather than throw.
    /// Never updated: it only hooks the game preload, so the prefab exists before a save is read.
    /// </summary>
    public partial class RailSidingSystem : GameSystemBase
    {
        public const string RailSidingName = "SolarpunkWasteRailSiding";

        private const uint RailSidingCost = 50000;

        private CargoUpgradeCloner m_Cloner;
        private EntityQuery m_GarbageFacilityQuery;
        private bool m_Registered;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Cloner = new CargoUpgradeCloner(World.GetOrCreateSystemManaged<PrefabSystem>(), EntityManager);
            m_GarbageFacilityQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<GarbageFacilityData>(), ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<PrefabData>() },
                None = new[] { ComponentType.ReadOnly<ServiceUpgradeData>(), ComponentType.ReadOnly<CargoTransportStationData>() },
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
                Mod.Log.Error($"Waste rail siding not available: no {CargoUpgradeCloner.TemplateName} prefab");
                return;
            }
            var facilities = m_Cloner.FindHosts(m_GarbageFacilityQuery, _ => true);
            if (facilities.Length == 0)
            {
                Mod.Log.Warn("Waste rail siding not available: no garbage facility found");
                return;
            }

            if (!m_Cloner.AddUpgrade(template, RailSidingName, RailSidingCost, facilities, CargoUpgradeCloner.TradeGarbageOnly))
                return;
            m_Cloner.GiveNeutralCargoData(template, facilities);
            Mod.Log.Info($"Rail siding offered on {facilities.Length} garbage facilities: {string.Join(", ", System.Array.ConvertAll(facilities, facility => facility.name))}");
        }
    }
}
