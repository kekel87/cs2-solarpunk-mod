using System.IO;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.Pathfind;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI.InGame;
using SolarpunkMod.Hub;
using SolarpunkMod.Info;
using SolarpunkMod.Policies;
using SolarpunkMod.Rail;
using SolarpunkMod.Station;
using SolarpunkMod.Waste;

namespace SolarpunkMod
{
    public class Mod : IMod
    {
        /// <summary>Mod identity shared with the UI module (UI/mod.json "id"): binding group and locale key prefix.</summary>
        public const string Id = nameof(SolarpunkMod);

        public static readonly ILog Log = LogManager.GetLogger($"{nameof(SolarpunkMod)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static ModSettings Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Log.Info($"Current mod asset at {asset.path}");
                RailTabIcons.Compose(Path.GetDirectoryName(asset.path));
            }
            // Prefabs (policy, Local Hub, platforms, rail tab) are registered on game preload.
            if (GameManager.instance.gameMode.IsGameOrEditor())
                Log.Warn("Enabled in a loaded city: the mod's buildings, policy and toolbar tab appear after the next load");

            Settings = new ModSettings(this);
            Settings.RegisterLocale();
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(Id, Settings, new ModSettings(this));

            CityInfoLocale.Register();
            updateSystem.UpdateAt<SolarpunkInfoUISystem>(SystemUpdatePhase.UIUpdate);
            // Updated by the game's selected info panel, like its own sections.
            updateSystem.World.GetOrCreateSystemManaged<SelectedInfoUISystem>()
                .AddMiddleSection(updateSystem.World.GetOrCreateSystemManaged<SolarpunkDistrictSection>());

            PolicyLocale.Register();
            // No phase: it only registers the policy prefab on game preload.
            updateSystem.World.GetOrCreateSystemManaged<EndResidentialParkingPolicySystem>();
            updateSystem.UpdateAfter<StreetParkingClosureSystem, ParkingLaneDataSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<ImpoundSystem>(SystemUpdatePhase.GameSimulation);
            // Same phase as the game's ModifiedSystem, which reads the same policy events.
            updateSystem.UpdateAt<ParkingBanToggleSystem>(SystemUpdatePhase.Modification4);

            // No phase: both only react to game loads (and to the option).
            updateSystem.World.GetOrCreateSystemManaged<HideNonElectricRailSystem>();
            updateSystem.World.GetOrCreateSystemManaged<ElectricTrainsOnlySystem>();

            LocalHubLocale.Register();
            // No phase: it only registers the Local Hub prefab on game preload.
            updateSystem.World.GetOrCreateSystemManaged<LocalHubBuildingSystem>();
            // Just before the game's seller search, so a shop's purchase is caught first.
            updateSystem.UpdateBefore<HubSaleSystem, ResourceBuyerSystem>(SystemUpdatePhase.GameSimulation);
            // Asks the cargo bikes' bicycle path before the game asks a road one, in both phases where the game runs it.
            updateSystem.UpdateBefore<CargoBikeSystem, DeliveryTruckAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<CargoBikeSystem, DeliveryTruckAISystem>(SystemUpdatePhase.LoadSimulation);

            StationPlatformLocale.Register();
            // No phase: it only registers the station platform upgrades on game preload.
            updateSystem.World.GetOrCreateSystemManaged<StationPlatformSystem>();

            RailSidingLocale.Register();
            // No phase: it only registers the upgrade on game preload.
            updateSystem.World.GetOrCreateSystemManaged<RailSidingSystem>();
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            Settings?.UnregisterInOptionsUI();
            Settings = null;
        }
    }
}
