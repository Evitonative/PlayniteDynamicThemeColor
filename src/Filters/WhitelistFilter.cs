using System;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("aa174ae3-8842-487d-8f28-eb5a9c9eee6c",
        "Whitelist Game", "Evito", 1,
        "Include a game.")]
    public class WhitelistFilter : SchemeFilter<WhitelistSettings> {
        public override string Name {
            get => "Whitelist " + (!string.IsNullOrWhiteSpace(Settings.GameName) ? Settings.GameName : "Game");
        }

        public override bool? ApplyGameFilter(Game game) {
            if (Settings.GameId == null) return null;
            try {
                Guid gameId = Guid.Parse(Settings.GameId);
                if (game.Id == gameId) return false;
            } catch (Exception) {
                return null;
            }
            return null;
        }
    }

    public class WhitelistSettings : ContextMenuAddedFilterSettings {
    }
}
