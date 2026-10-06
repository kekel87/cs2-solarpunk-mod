using System.Collections.Generic;
using Colossal.Localization;
using Game.SceneFlow;

namespace SolarpunkMod.Info
{
    /// <summary>
    /// Texts of the Solarpunk info panel, read by the UI through the game's localization.
    /// </summary>
    public static class CityInfoLocale
    {
        private const string KeyPrefix = Mod.Id + ".Info.";

        public static void Register()
        {
            var localizationManager = GameManager.instance.localizationManager;
            localizationManager.AddSource("en-US", new MemorySource(Prefixed(English)));
            localizationManager.AddSource("fr-FR", new MemorySource(Prefixed(French)));
        }

        private static Dictionary<string, string> Prefixed(Dictionary<string, string> entries)
        {
            var prefixed = new Dictionary<string, string>();
            foreach (var entry in entries)
                prefixed[KeyPrefix + entry.Key] = entry.Value;
            return prefixed;
        }

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["TITLE"] = "Solarpunk city",
            ["TRIPS"] = "Trips under way",
            ["TRIPS_TOOLTIP"] = "Who is travelling right now, averaged over the last few in-game moments. Varies with the time of day.",
            ["CAR"] = "Car",
            ["BICYCLE"] = "Bicycle",
            ["PUBLIC_TRANSPORT"] = "Public transport",
            ["WALKING"] = "Walking",
            ["VEHICLES"] = "Vehicles on the move",
            ["PERSONAL_CARS"] = "Cars",
            ["DELIVERY_TRUCKS"] = "Delivery trucks",
            ["GARBAGE_TRUCKS"] = "Garbage trucks",
            ["CARGO_TRAINS"] = "Cargo trains",
            ["GARBAGE_TRAINS"] = "Garbage trains",
            ["GARBAGE_ON_TRAINS"] = "Garbage on trains",
            ["GARBAGE_ABOARD"] = "Aboard",
            ["GARBAGE_ON_TRAINS_TOOLTIP"] = "Garbage aboard the cargo trains on the move right now, through traffic excluded.",
            ["ELECTRICITY"] = "Electricity",
            ["RENEWABLE_SHARE"] = "Renewable share",
            ["RENEWABLE_TOOLTIP"] = "Wind, solar and hydro share of the city's available power plant capacity, as in the electricity info view. Imports not included.",
            ["DISTRICT_TITLE"] = "Solarpunk",
            ["DISTRICT_TOOLTIP"] = "Traffic in the district right now, roads on its border included. Car share: green up to 20 %, red above 40 %. Delivery trucks: green under 0.1 per km of road, red from 0.5. Bars: history since the district was selected.",
            ["NOT_ENOUGH_DATA"] = "Not enough data",
            ["SMALL_DELIVERY_VEHICLES"] = "Small delivery vehicles",
        };

        private static readonly Dictionary<string, string> French = new Dictionary<string, string>
        {
            ["TITLE"] = "Ville solarpunk",
            ["TRIPS"] = "Déplacements en cours",
            ["TRIPS_TOOLTIP"] = "Qui se déplace en ce moment, moyenné sur les derniers instants de jeu. Varie selon l'heure.",
            ["CAR"] = "Voiture",
            ["BICYCLE"] = "Vélo",
            ["PUBLIC_TRANSPORT"] = "Transports en commun",
            ["WALKING"] = "À pied",
            ["VEHICLES"] = "Véhicules en circulation",
            ["PERSONAL_CARS"] = "Voitures",
            ["DELIVERY_TRUCKS"] = "Camions de livraison",
            ["GARBAGE_TRUCKS"] = "Bennes à ordures",
            ["CARGO_TRAINS"] = "Trains de marchandises",
            ["GARBAGE_TRAINS"] = "Trains-poubelle",
            ["GARBAGE_ON_TRAINS"] = "Déchets en train",
            ["GARBAGE_ABOARD"] = "À bord",
            ["GARBAGE_ON_TRAINS_TOOLTIP"] = "Déchets à bord des trains de marchandises en circulation, trafic de transit exclu.",
            ["ELECTRICITY"] = "Électricité",
            ["RENEWABLE_SHARE"] = "Part renouvelable",
            ["RENEWABLE_TOOLTIP"] = "Part de l'éolien, du solaire et de l'hydraulique dans la capacité disponible des centrales de la ville, comme dans la vue électricité. Importations non comptées.",
            ["DISTRICT_TITLE"] = "Solarpunk",
            ["DISTRICT_TOOLTIP"] = "Circulation dans le district en ce moment, routes de bordure comprises. Part voiture : vert jusqu'à 20 %, rouge au-delà de 40 %. Camions de livraison : vert sous 0,1 par km de route, rouge dès 0,5. Barres : historique depuis la sélection du district.",
            ["NOT_ENOUGH_DATA"] = "Pas assez de données",
            ["SMALL_DELIVERY_VEHICLES"] = "Petits véhicules de livraison",
        };
    }
}
