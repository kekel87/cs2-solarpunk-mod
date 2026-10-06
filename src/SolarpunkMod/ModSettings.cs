using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Game.Modding;
using Game.Settings;
using Game.SceneFlow;

namespace SolarpunkMod
{
    /// <summary>
    /// The mod's page in Options. Stored in the player's settings, never in a save.
    /// </summary>
    [FileLocation(Mod.Id)]
    public class ModSettings : ModSetting
    {
        // The game never calls SetDefaults for a mod's settings: a fresh install relies on this.
        public ModSettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        /// <summary>Train depots only send electric trains, when every use of trains has an electric model.</summary>
        public bool ElectricTrainsOnly { get; set; }

        /// <summary>Shops this close to their district's Local Hub, in metres, fetch goods with a small vehicle.</summary>
        [SettingsUISlider(min = 100, max = 2000, step = 50)]
        public int HubSmallVehicleRadius { get; set; }

        /// <summary>Shops this close to their Local Hub, in metres, are delivered by cargo bikes from the hub.</summary>
        [SettingsUISlider(min = 100, max = 1000, step = 50)]
        public int HubCargoBikeRadius { get; set; }

        /// <summary>The most cargo bikes one order is split between; what they cannot carry is ordered again later.</summary>
        [SettingsUISlider(min = 1, max = 8, step = 1)]
        public int HubMaxCargoBikesPerOrder { get; set; }

        public override void SetDefaults()
        {
            ElectricTrainsOnly = false;
            HubSmallVehicleRadius = 600;
            HubCargoBikeRadius = 300;
            HubMaxCargoBikesPerOrder = 4;
        }

        public void RegisterLocale()
        {
            var title = GetSettingsLocaleID();
            var label = GetOptionLabelLocaleID(nameof(ElectricTrainsOnly));
            var description = GetOptionDescLocaleID(nameof(ElectricTrainsOnly));
            var radiusLabel = GetOptionLabelLocaleID(nameof(HubSmallVehicleRadius));
            var radiusDescription = GetOptionDescLocaleID(nameof(HubSmallVehicleRadius));
            var bikeRadiusLabel = GetOptionLabelLocaleID(nameof(HubCargoBikeRadius));
            var bikeRadiusDescription = GetOptionDescLocaleID(nameof(HubCargoBikeRadius));
            var maxBikesLabel = GetOptionLabelLocaleID(nameof(HubMaxCargoBikesPerOrder));
            var maxBikesDescription = GetOptionDescLocaleID(nameof(HubMaxCargoBikesPerOrder));

            var localizationManager = GameManager.instance.localizationManager;
            localizationManager.AddSource("en-US", new MemorySource(new Dictionary<string, string>
            {
                [title] = "Solarpunk Mod",
                [label] = "Electric trains only",
                [description] = "Train depots stop sending diesel trains, including lines set to a diesel model. Applied only if passengers, freight and garbage each have an electric model in the game; otherwise nothing changes and the log says why.",
                [radiusLabel] = "Local Hub small vehicle range (m)",
                [radiusDescription] = "Shops of a Local Hub's district within this distance of the hub fetch their goods with a small delivery vehicle; farther ones still send a truck.",
                [bikeRadiusLabel] = "Local Hub cargo bike range (m)",
                [bikeRadiusDescription] = "Shops within this distance of their Local Hub are delivered by cargo bikes sent from the hub, several to an order if needed. Never more than the small vehicle range.",
                [maxBikesLabel] = "Cargo bikes per order",
                [maxBikesDescription] = "The most cargo bikes one order is split between. What they cannot carry is not sold: the shop orders it again later, in smaller and more frequent orders.",
            }));
            localizationManager.AddSource("fr-FR", new MemorySource(new Dictionary<string, string>
            {
                [title] = "Solarpunk Mod",
                [label] = "Trains électriques seulement",
                [description] = "Les dépôts de train n'envoient plus de trains diesel, y compris sur les lignes réglées sur un modèle diesel. Appliqué seulement si voyageurs, fret et déchets ont chacun un modèle électrique dans le jeu ; sinon rien ne change et le journal dit pourquoi.",
                [radiusLabel] = "Portée des petits véhicules du Local Hub (m)",
                [radiusDescription] = "Les commerces du district d'un Local Hub situés à moins de cette distance du hub vont chercher leurs marchandises avec un petit véhicule de livraison ; les plus éloignés envoient toujours un camion.",
                [bikeRadiusLabel] = "Portée des vélos-cargo du Local Hub (m)",
                [bikeRadiusDescription] = "Les commerces à moins de cette distance de leur Local Hub sont livrés par des vélos-cargo envoyés par le hub, plusieurs par commande si besoin. Jamais plus que la portée des petits véhicules.",
                [maxBikesLabel] = "Vélos-cargo par commande",
                [maxBikesDescription] = "Le nombre maximal de vélos-cargo entre lesquels une commande est répartie. Ce qu'ils ne peuvent pas porter n'est pas vendu : le commerce le recommande plus tard, en commandes plus petites et plus fréquentes.",
            }));
        }
    }
}
