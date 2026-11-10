using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class PinkSeedFilter : SchemeFilter {
        public override Guid Id { get; } = Guid.Parse("d42292e9-8c73-4c45-a286-8919ba0efde2");
        public override string Name => "[TESTING] Pink Seed Filter";
        public override string Author => "Evito";
        public override int Version => 1;
        public override string Description => "Makes the seed color pink for testing";

        public override uint ApplySeedFilter(
            uint currentSeed, ColorExtractionResults colorExtractionResults,
            Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            return 0xFFFF00FF;
        }
    }
}
