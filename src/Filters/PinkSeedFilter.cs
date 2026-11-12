using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("d42292e9-8c73-4c45-a286-8919ba0efde2",
        "[TESTING] Pink Seed", "Evito", 1,
        "Makes the seed color pink for testing")]
    public class PinkSeedFilter : SchemeFilter {
        public override uint ApplySeedFilter(
            uint currentSeed, ColorExtractionResults colorExtractionResults,
            Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            return 0xFFFF00FF;
        }
    }
}
