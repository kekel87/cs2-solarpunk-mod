using System.Collections.Generic;
using Colossal.Localization;
using Game.SceneFlow;

namespace SolarpunkMod.Hub
{
    /// <summary>
    /// Toolbar names and descriptions of the Local Hubs, under the game's own keys for assets.
    /// </summary>
    public static class LocalHubLocale
    {
        private const string EnglishRole = "The shops of its district buy their goods here as long as it has them in stock, with short trips instead of crossing the city; nearby shops send small delivery vehicles. It stocks the goods of a cargo station, not everything a shop sells.";
        private const string FrenchRole = "Les commerces de son district s'y approvisionnent tant qu'il a la marchandise en stock, par de courts trajets au lieu de traverser la ville ; les commerces proches envoient de petits véhicules de livraison. Il stocke les marchandises d'une gare de fret, pas tout ce que vend un commerce.";

        /// <summary>The hub's own delivery vehicles, imported assets named after their prefab.</summary>
        private static readonly (string prefabName, string english, string french)[] VehicleNames =
        {
            ("SolarpunkCargoBikeBox01", "Box cargo bike", "Vélo-cargo à caisse"),
            ("SolarpunkCargoBikePallet01", "Cargo bike with pallet trailer", "Vélo à remorque palette"),
            ("SolarpunkCoveredCargoBike01", "Covered cargo bike", "Vélo-cargo couvert"),
            ("SolarpunkElectricTrike01", "Electric delivery trike", "Triporteur électrique"),
            ("SolarpunkKeiTruck01", "Electric kei truck", "Kei truck électrique"),
        };

        public static void Register()
        {
            var english = Entries(
                ("Local Hub", "A neighbourhood cargo station fed by rail."),
                ("Underground Local Hub", "A compact neighbourhood cargo station; its rail quay lies 20 m underground."),
                ("Urban Local Hub", "A city-block cargo station with homes and offices above, built flush with the street between its neighbours; its rail quay lies 20 m underground."),
                EnglishRole);
            var french = Entries(
                ("Local Hub", "Une gare de marchandises de quartier alimentée par le rail."),
                ("Local Hub souterrain", "Une gare de marchandises de quartier compacte ; son quai ferroviaire est à 20 m sous terre."),
                ("Local Hub urbain", "Une gare de marchandises d'îlot, logements et bureaux au-dessus, alignée sur la rue entre ses voisins ; son quai ferroviaire est à 20 m sous terre."),
                FrenchRole);
            foreach (var vehicle in VehicleNames)
            {
                english[$"Assets.NAME[{vehicle.prefabName}]"] = vehicle.english;
                french[$"Assets.NAME[{vehicle.prefabName}]"] = vehicle.french;
            }

            var localizationManager = GameManager.instance.localizationManager;
            localizationManager.AddSource("en-US", new MemorySource(english));
            localizationManager.AddSource("fr-FR", new MemorySource(french));
        }

        private static Dictionary<string, string> Entries((string name, string intro) small, (string name, string intro) underground,
            (string name, string intro) large, string role)
        {
            var entries = new Dictionary<string, string>();
            Add(entries, LocalHubBuildingSystem.PrefabName, small, role);
            Add(entries, LocalHubBuildingSystem.UndergroundPrefabName, underground, role);
            Add(entries, LocalHubBuildingSystem.LargePrefabName, large, role);
            return entries;
        }

        private static void Add(Dictionary<string, string> entries, string prefabName, (string name, string intro) text, string role)
        {
            entries[$"Assets.NAME[{prefabName}]"] = text.name;
            entries[$"Assets.DESCRIPTION[{prefabName}]"] = $"{text.intro} {role}";
        }
    }
}
