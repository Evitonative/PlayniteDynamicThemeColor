using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("144ac390-95fb-4735-aa3f-1acdd5dc5603",
        "Blacklist All Games", "Evito", 1,
        "Do do theming on an opt-in basis with whitelisting.")]
    public class BlacklistAllFilter : SchemeFilter {
        public override bool? ApplyGameFilter(Game game) {
            return true;
        }
    }
}
