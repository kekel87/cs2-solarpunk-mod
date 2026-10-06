using System.Collections.Generic;
using Colossal.Localization;
using Game.SceneFlow;

namespace SolarpunkMod.Waste
{
    /// <summary>
    /// Name and description of the waste rail siding upgrade, under the game's own keys for assets.
    /// </summary>
    public static class RailSidingLocale
    {
        private const string Name = "Assets.NAME[" + RailSidingSystem.RailSidingName + "]";
        private const string Description = "Assets.DESCRIPTION[" + RailSidingSystem.RailSidingName + "]";

        public static void Register()
        {
            var localizationManager = GameManager.instance.localizationManager;
            localizationManager.AddSource("en-US", new MemorySource(new Dictionary<string, string>
            {
                [Name] = "Rail siding",
                [Description] = "A garbage track and platform beside the facility. Garbage trains from a train garbage yard or a station's waste platform unload here directly; garbage trucks keep delivering as before.",
            }));
            localizationManager.AddSource("fr-FR", new MemorySource(new Dictionary<string, string>
            {
                [Name] = "Embranchement ferroviaire",
                [Description] = "Une voie et un quai à déchets à côté de l'installation. Les trains-poubelles venus d'un yard déchets ou d'un quai déchets de gare y déchargent directement ; les bennes continuent de livrer comme avant.",
            }));
        }
    }
}
