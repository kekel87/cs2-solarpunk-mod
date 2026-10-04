using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using SolarpunkMod.Info;

namespace SolarpunkMod
{
    public class Mod : IMod
    {
        /// <summary>Mod identity shared with the UI module (UI/mod.json "id"): binding group and locale key prefix.</summary>
        public const string Id = nameof(SolarpunkMod);

        public static readonly ILog Log = LogManager.GetLogger($"{nameof(SolarpunkMod)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                Log.Info($"Current mod asset at {asset.path}");

            CityInfoLocale.Register();
            updateSystem.UpdateAt<SolarpunkInfoUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
        }
    }
}
