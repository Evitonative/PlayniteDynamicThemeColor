using System;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("c1dc94b8-2373-48b3-8933-9af361d0f228",
        "Blacklist Game", "Evito", 1,
        "Exclude a game.")]
    public class BlacklistFilter : SchemeFilter<BlacklistSettings> {
        public override string Name {
            get => "Blacklist " + (!string.IsNullOrWhiteSpace(Settings.GameName) ? Settings.GameName : "Game");
        }

        public override bool? ApplyGameFilter(Game game) {
            if (Settings.GameId == null) return null;
            try {
                Guid gameId = Guid.Parse(Settings.GameId);
                if (game.Id == gameId) return true;
            } catch (Exception) {
                return null;
            }
            return null;
        }
    }

    public class BlacklistSettings : ContextMenuAddedFilterSettings {
    }
}
