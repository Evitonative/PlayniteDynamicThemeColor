using System;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    public class GamesExcludeAllGame : SchemeFilter {
        public override Guid Id => Guid.Parse("144ac390-95fb-4735-aa3f-1acdd5dc5603");
        public override string Name => "Exclude All Games";
        public override string Author => "Evito";
        public override int Version => 1;
        public override string Description => "Excludes all games.";

        public override bool? ApplyGameFilter(Game game) {
            return true;
        }
    }
}
