using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("679d71ea-63a3-424f-9d03-ebfb3c5490d8",
        "Seed Color Override", "Evito", 1,
        "Override the seed color of a game.")]
    public class OverrideSeedFilter : SchemeFilter<OverrideSeedColorSettings> {
        private const uint fallbackColor = 0xFF1CD8FC;
        public override string Name => "Seed Color Override for " + (!string.IsNullOrWhiteSpace(Settings.GameName) ? Settings.GameName : "Games");

        public override uint ApplySeedFilter(uint currentSeed, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {
            if (Settings.GameId == null ||
                game == null ||
                game.Id != Guid.Parse(Settings.GameId)) return currentSeed;

            return ColorConverters.ConvertHexToArgb(Settings.Hex) ?? fallbackColor;
        }
    }

    public class OverrideSeedColorSettings : ContextMenuAddedFilterSettings {
        [ColorSetting(
            Label = "Hex Color", Stages =  new []{ FilterStage.Seed },
            Tooltip = "Seed color used for the game.")]
        public string Hex { get; [UsedImplicitly] set; } = "#80FF80";

    }
}
