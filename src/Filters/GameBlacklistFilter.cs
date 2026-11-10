using System;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class GameBlacklistFilter : SchemeFilter<GameBlacklistSettings> {
        public const string FilterId = "c1dc94b8-2373-48b3-8933-9af361d0f228";
        public override Guid Id => Guid.Parse(FilterId);
        public override string Name => "Blacklist Game";
        public override string Author => "Evito";
        public override int Version => 1;
        public override string Description => "Exclude a game.";

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

    public class GameBlacklistSettings : GameBlackWhitelistFilter {
    }
}
