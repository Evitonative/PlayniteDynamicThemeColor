using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("12b95ffc-3465-465a-baa4-470ec8d6ad88",
        "[TESTING] Random Brush Color", "Evito", 1,
        "Makes every brush a random color for testing.")]
    public class RandomBrushColorFilter : SchemeFilter {
        public override uint ApplyBrushFilter(
            string brushKey, uint brushArgb,
            ColorExtractionResults colorExtractionResults,
            Game game,  IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            return 0xFF000000 + (uint)new Random().Next(0xFFFFFF);
        }
    }
}
