using System.Collections.Generic;
using Colossal.Localization;
using Game.SceneFlow;

namespace SolarpunkMod.Policies
{
    /// <summary>
    /// Policy names and descriptions, under the game's own policy keys.
    /// </summary>
    public static class PolicyLocale
    {
        private const string Title = "Policy.TITLE[" + EndResidentialParkingPolicySystem.PolicyName + "]";
        private const string Description = "Policy.DESCRIPTION[" + EndResidentialParkingPolicySystem.PolicyName + "]";

        public static void Register()
        {
            var localizationManager = GameManager.instance.localizationManager;
            localizationManager.AddSource("en-US", new MemorySource(new Dictionary<string, string>
            {
                [Title] = "End Residential Parking",
                [Description] = $"Private cars may no longer park on the district's streets. Cars still parked there after a {ImpoundSystem.GracePeriodHours}-hour notice are towed away. Off-street parking and bike racks remain available. Fewer residents take the household car to work or school.",
            }));
            localizationManager.AddSource("fr-FR", new MemorySource(new Dictionary<string, string>
            {
                [Title] = "Fin du stationnement résidentiel",
                [Description] = $"Les voitures particulières ne peuvent plus se garer dans les rues du district. Celles encore garées après un préavis de {ImpoundSystem.GracePeriodHours} heures partent à la fourrière. Les parkings hors voirie et les arceaux vélo restent ouverts. Moins d'habitants prennent la voiture du ménage pour aller au travail ou à l'école.",
            }));
        }
    }
}
