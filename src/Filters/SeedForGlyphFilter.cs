using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("2a3c1e2c-3cf2-4d64-b3d2-76412c8923dc",
        "Original Color for Accents", "Evito", 1,
        "Made for the built-in default and Harmony schemes.\n" +
        "Colorful elements like play buttons and checkmarks will use the exact color\n" +
        "pulled from the image instead of blending into the background theme.\n\n" +
        "This potentially results in poor contrast. Use with care.")]
    public class SeedForGlyphFilter : SchemeFilter {
        public override uint ApplyBrushFilter(string brushKey, uint brushArgb, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {
            uint accentColor = colorExtractionResults?.SeedColor ?? brushArgb;

            switch (brushKey) {
                case "GlyphBrush":
                case "HighlightGlyphBrush":
                case "CheckBoxCheckMarkBrush":
                case "MainPanelBackgroundBrush":
                    return accentColor;
                default:
                    return brushArgb;
            }
        }
    }
}
