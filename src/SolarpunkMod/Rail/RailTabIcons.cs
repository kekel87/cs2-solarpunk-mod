using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SolarpunkMod.Rail
{
    /// <summary>
    /// The Train tab icon: the game's train icon with its electricity icon in superscript.
    /// Composed at load from the installed game's icons, so the repository holds none of its art.
    /// </summary>
    internal static class RailTabIcons
    {
        // The game serves every UI mod's folder under this host.
        public const string Electric = "coui://ui-mods/images/SolarpunkTrainElectric.svg";

        private const int CanvasSize = 40;
        private static readonly Regex RootTag = new Regex("<svg[^>]*>");
        private static readonly Regex ViewBoxWidth = new Regex("viewBox=\"[-\\d.]+ [-\\d.]+ ([\\d.]+)");

        public static bool Available { get; private set; }

        public static void Compose(string modDirectory)
        {
            try
            {
                // The game's own content folder (EnvPath.kContentPath, in an assembly we do not reference).
                var content = Path.GetDirectoryName(Application.streamingAssetsPath).Replace('\\', '/') + "/Content";
                var gameIcons = content + "/Game/UI/Media/Game/Icons/";
                var directory = Path.Combine(modDirectory, "images");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, Path.GetFileName(Electric)),
                    Superscript(File.ReadAllText(gameIcons + "Train.svg"), File.ReadAllText(gameIcons + "Electricity.svg")));
                Available = true;
            }
            catch (Exception exception)
            {
                Mod.Log.Warn($"Train tab icon not composed, the game's icon is kept: {exception.Message}");
            }
        }

        /// <summary>The main icon in the lower left, the badge smaller in the upper right.</summary>
        private static string Superscript(string main, string badge)
        {
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\""
                + $" width=\"{CanvasSize}\" height=\"{CanvasSize}\" viewBox=\"0 0 {CanvasSize} {CanvasSize}\">"
                + Place(main, 0, 6, 34)
                + Place(badge, 22, 0, 18)
                + "</svg>";
        }

        /// <summary>The content of an icon, scaled to a square of the given size at the given corner.</summary>
        private static string Place(string svg, int x, int y, int size)
        {
            var root = RootTag.Match(svg);
            var width = float.Parse(ViewBoxWidth.Match(root.Value).Groups[1].Value, CultureInfo.InvariantCulture);
            var start = root.Index + root.Length;
            var content = svg.Substring(start, svg.LastIndexOf("</svg>", StringComparison.Ordinal) - start);
            var scale = (size / width).ToString(CultureInfo.InvariantCulture);
            return $"<g transform=\"translate({x} {y}) scale({scale})\">{content}</g>";
        }
    }
}
