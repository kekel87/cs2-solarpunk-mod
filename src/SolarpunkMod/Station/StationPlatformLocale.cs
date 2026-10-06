using System.Collections.Generic;
using Colossal.Localization;
using Game.SceneFlow;

namespace SolarpunkMod.Station
{
    /// <summary>
    /// Names and descriptions of the station platform upgrades, under the game's own keys for assets.
    /// </summary>
    public static class StationPlatformLocale
    {
        private const string FreightName = "Assets.NAME[" + StationPlatformSystem.FreightPlatformName + "]";
        private const string FreightDescription = "Assets.DESCRIPTION[" + StationPlatformSystem.FreightPlatformName + "]";
        private const string WasteName = "Assets.NAME[" + StationPlatformSystem.WastePlatformName + "]";
        private const string WasteDescription = "Assets.DESCRIPTION[" + StationPlatformSystem.WastePlatformName + "]";

        public static void Register()
        {
            var localizationManager = GameManager.instance.localizationManager;
            localizationManager.AddSource("en-US", new MemorySource(new Dictionary<string, string>
            {
                [FreightName] = "Freight platform",
                [FreightDescription] = "A cargo platform beside the passenger station, with its own track and storage. The station then serves a passenger line and a cargo line, each on its own platform.",
                [WasteName] = "Waste platform",
                [WasteDescription] = "A garbage platform beside the passenger station, with its own track. Garbage trucks unload there and garbage trains carry it away on their own line; part of it is processed on site.",
            }));
            localizationManager.AddSource("fr-FR", new MemorySource(new Dictionary<string, string>
            {
                [FreightName] = "Quai fret",
                [FreightDescription] = "Un quai de marchandises à côté de la gare voyageurs, avec sa propre voie et son stockage. La gare accueille alors une ligne voyageurs et une ligne de marchandises, chacune sur son quai.",
                [WasteName] = "Quai déchets",
                [WasteDescription] = "Un quai à déchets à côté de la gare voyageurs, avec sa propre voie. Les bennes y déchargent et des trains-poubelles emportent les déchets sur leur propre ligne ; une partie est traitée sur place.",
            }));
        }
    }
}
