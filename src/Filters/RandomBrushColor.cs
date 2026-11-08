using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class RandomBrushColor : SchemeFilter {
        public override Guid Id { get; } = Guid.Parse("12b95ffc-3465-465a-baa4-470ec8d6ad88");
        public override string Name => "[TESTING] Random Brush Color";
        internal override object SettingsObject { get; set; }

        public override uint ApplyBrushFilters(
            string brushKey, uint brushArgb,
            ColorExtractionResults colorExtractionResults,
            Game game,  IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            return 0xFF000000 + (uint)new Random().Next(0xFFFFFF);
        }
    }
}
