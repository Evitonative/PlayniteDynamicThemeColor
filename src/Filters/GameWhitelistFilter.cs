using System;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class GameWhitelistFilter : SchemeFilter<GameWhitelistSettings> {
        public const string FilterId = "aa174ae3-8842-487d-8f28-eb5a9c9eee6c";
        public override Guid Id => Guid.Parse(FilterId);
        public override string Name => "Whitelist Game";
        public override string Author => "Evito";
        public override int Version => 1;
        public override string Description => "Include a game.";

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

    public class GameWhitelistSettings : GameBlackWhitelistFilter {
    }
}
