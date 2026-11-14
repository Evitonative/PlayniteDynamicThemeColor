using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;

namespace DynamicThemeColor.Filters {
    public abstract class ContextMenuAddedFilterSettings {
        [StringSetting(
            Label = "Game ID", Stages = new []{ FilterStage.Game },
            Tooltip = "You can get this by right clicking a game > Dynamic Theme Color > Copy Game ID")]
        [CanBeNull] public string GameId { get; [UsedImplicitly] set; }

        [StringSetting(
            Label = "Game Name", Stages = new []{ FilterStage.Game },
            Tooltip = "This property is only used for convenience purposes and is not evaluated.")]
        [CanBeNull] public string GameName { [UsedImplicitly] get; [UsedImplicitly] set; }
    }
}
