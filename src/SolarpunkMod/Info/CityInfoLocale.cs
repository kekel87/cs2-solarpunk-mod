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
            ["ELECTRICITY"] = "Electricity",
            ["RENEWABLE_SHARE"] = "Renewable share",
            ["RENEWABLE_TOOLTIP"] = "Wind, solar and hydro share of the city's available power plant capacity, as in the electricity info view. Imports not included.",
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
            ["ELECTRICITY"] = "Électricité",
            ["RENEWABLE_SHARE"] = "Part renouvelable",
            ["RENEWABLE_TOOLTIP"] = "Part de l'éolien, du solaire et de l'hydraulique dans la capacité disponible des centrales de la ville, comme dans la vue électricité. Importations non comptées.",
        };
    }
}
