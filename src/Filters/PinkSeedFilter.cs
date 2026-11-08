using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class PinkSeedFilter : SchemeFilter {
        public override Guid Id { get; } = Guid.Parse("d42292e9-8c73-4c45-a286-8919ba0efde2");
        public override string Name => "[TESTING] Pink Seed Filter";
        internal override object SettingsObject { get; set; }

        public override uint ApplySeedFilters(uint seedArgb, Game game, IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {
            return 0xFFFF00FF;
        }
    }
}
