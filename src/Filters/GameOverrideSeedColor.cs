using System;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class GameOverrideSeedColor : SchemeFilter<GameOverrideSeedColorSettings> {
        public const string FilterId = "679d71ea-63a3-424f-9d03-ebfb3c5490d8";
        private const uint fallbackColor = 0xFF1CD8FC;

        public override Guid Id => Guid.Parse(FilterId);
        public override string Name{
            get => "Custom Color for " + (!string.IsNullOrWhiteSpace(Settings.GameName) ? Settings.GameName : "a Game");
        }
        public override string Author => "Evito";
        public override int Version => 1;
        public override string Description => "Override the seed color of a game.";

        public override uint ApplySeedFilter(uint currentSeed, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {
            if (Settings.GameId == null ||
                game == null ||
                game.Id != Guid.Parse(Settings.GameId)) return currentSeed;

            return MaterialColorHelper.ConvertHexToArgb(Settings.Hex) ?? fallbackColor;
        }
    }

    public class GameOverrideSeedColorSettings : GameContextMenuAddedFilterSettings {
        [ColorSetting(
            Label = "Hex Color",
            Tooltip = "Seed color used for the game.")]
        public string Hex { get; [UsedImplicitly] set; } = "#1cd8fc";

    }
}
