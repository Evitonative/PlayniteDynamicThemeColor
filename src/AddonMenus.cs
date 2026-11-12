using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using DynamicThemeColor.Core;
using DynamicThemeColor.Filters;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DynamicThemeColor {
    public partial class Addon {
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs menuItemsArgs) {
            string menuRoot = ResourceProvider.GetString("LOCDTCContextMain");

            Guid? blacklistFilterId = typeof(BlacklistFilter).GetCustomAttribute<SchemeFilterAttribute>()?.FilterId;
            Guid? whitelistFilterId = typeof(WhitelistFilter).GetCustomAttribute<SchemeFilterAttribute>()?.FilterId;
            Guid? seedOverrideFilterId = typeof(OverrideSeedFilter).GetCustomAttribute<SchemeFilterAttribute>()?.FilterId;

            yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextCopyGameId"),
                Action = (args) => {
                    Guid? selectedGame = args.Games.FirstOrDefault()?.Id;
                    if (selectedGame == null) return;
                    Clipboard.SetText(selectedGame.ToString());
                },
                MenuSection = menuRoot,
            };

            if (blacklistFilterId.HasValue)
                yield return GetBlackWhitelistGameMenuItem(blacklistFilterId.Value, "Blacklist", menuRoot);

            if (whitelistFilterId.HasValue)
                yield return GetBlackWhitelistGameMenuItem(whitelistFilterId.Value, "Whitelist", menuRoot);

            if (seedOverrideFilterId.HasValue) yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextOverrideGameSeed"),
                Action = (args) => {
                    Game game = args.Games.FirstOrDefault();
                    if (game == null) return;

                    Guid filterId = seedOverrideFilterId.Value;
                    bool exists = AddonSettings.SeedFilters
                        .Where(f => f.FilterId == filterId)
                        .Select(f => FilterRegistry.TryGetFilterInstance(f.InstanceId, out SchemeFilter instance) ? instance : null)
                        .Where(f => f != null)
                        .Select(f => f.FilterConfiguration?.Settings)
                        .Where(f => f != null)
                        .OfType<OverrideSeedColorSettings>()
                        .Any(s => Guid.TryParse(s.GameId, out Guid id) && id == game.Id);

                    if (exists) {
                        MessageBoxResult result = PlayniteApi.Dialogs.ShowMessage(
                            ResourceProvider.GetString("LOCDTCContextOverrideGameSeedPreviousFound"),
                            ResourceProvider.GetString("LOCDTCContextOverrideGameSeed"),
                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes) return;

                        IEnumerable<FilterConfiguration> itemsToRemove = AddonSettings.SeedFilters
                            .Where(f => f.FilterId == filterId)
                            .Where(f => {
                                if (!FilterRegistry.TryGetFilterInstance(f.InstanceId, out SchemeFilter instance)) return false;
                                if (!(instance.FilterConfiguration?.Settings is OverrideSeedColorSettings s)) return false;
                                if (!Guid.TryParse(s.GameId, out Guid id)) return false;
                                return id == game.Id;
                            }).ToList();

                        foreach (FilterConfiguration removeConfig in itemsToRemove) {
                            AddonSettings.SeedFilters.Remove(removeConfig);
                        }
                    }

                    if(!ColorPicker.ExecutePickColor(out string newColor)) return;

                    FilterConfiguration filterConfig = FilterRegistry.CreateNewFilterInstance(filterId);
                    if (filterConfig == null) return;
                    if (!FilterRegistry.TryGetFilterInstance(filterConfig.InstanceId, out SchemeFilter filterInstance)) return;
                    if (!(filterInstance.FilterConfiguration?.Settings is OverrideSeedColorSettings settings)) return;

                    settings.GameId = game.Id.ToString();
                    settings.GameName = game.Name;
                    settings.Hex = newColor;

                    AddonSettings.SeedFilters.Add(filterConfig);
                    SettingsView.EndEditRefresh();
                },
                MenuSection = menuRoot,
            };
        }

        private GameMenuItem GetBlackWhitelistGameMenuItem(Guid filterId, string translationKey, string menuRoot) {
            return new GameMenuItem {
                Description = ResourceProvider.GetString($"LOCDTCContext{translationKey}Game"),
                Action = (args) => {
                    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    bool? result = AddListFilter(
                        filterId,
                        args.Games.FirstOrDefault(),
                        ResourceProvider.GetString($"LOCDTCContext{translationKey}Game"),
                        ResourceProvider.GetString($"LOCDTCContext{translationKey}GameDuplicate"));
                    switch (result) {
                        case true:
                            if (!shiftHeld) PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString($"LOCDTCContext{translationKey}GameAdded"),
                                ResourceProvider.GetString($"LOCDTCContext{translationKey}Game"),
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        case false:
                            PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString($"LOCDTCContext{translationKey}GameFailed"),
                                ResourceProvider.GetString($"LOCDTCContext{translationKey}Game"),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        case null:
                            return;
                    }
                },
                MenuSection = menuRoot,
            };
        }

        private bool? AddListFilter(Guid filterId, Game game, string dialogTitle, string dialogTitleMessage) {
            if (game == null) return false;

            bool exists = AddonSettings.GameFilters
                .Where(f => f.FilterId == filterId)
                .Select(f => FilterRegistry.TryGetFilterInstance(f.InstanceId, out SchemeFilter instance) ? instance : null)
                .Where(f => f != null)
                .Select(f => f.FilterConfiguration?.Settings)
                .Where(f => f != null)
                .OfType<ContextMenuAddedFilterSettings>()
                .Any(s => Guid.TryParse(s.GameId, out Guid id) && id == game.Id);

            if (exists) {
                MessageBoxResult result = PlayniteApi.Dialogs.ShowMessage(
                    dialogTitleMessage, dialogTitle,
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return null;

                IEnumerable<FilterConfiguration> itemsToRemove = AddonSettings.GameFilters
                    .Where(f => f.FilterId == filterId)
                    .Where(f => {
                        if (!FilterRegistry.TryGetFilterInstance(f.InstanceId, out SchemeFilter instance)) return false;
                        if (!(instance.FilterConfiguration?.Settings is ContextMenuAddedFilterSettings s)) return false;
                        if (!Guid.TryParse(s.GameId, out Guid id)) return false;
                        return id == game.Id;
                    }).ToList();

                foreach (FilterConfiguration removeConfig in itemsToRemove) {
                    AddonSettings.GameFilters.Remove(removeConfig);
                }

                SettingsView.EndEditRefresh();
                return null;
            }


            FilterConfiguration filterConfig = FilterRegistry.CreateNewFilterInstance(filterId);
            if (filterConfig == null) return false;
            if (!FilterRegistry.TryGetFilterInstance(filterConfig.InstanceId, out SchemeFilter filterInstance)) return false;
            if (!(filterInstance.FilterConfiguration?.Settings is ContextMenuAddedFilterSettings settings)) return false;

            settings.GameId = game.Id.ToString();
            settings.GameName = game.Name;

            AddonSettings.GameFilters.Add(filterConfig);
            SettingsView.EndEditRefresh();
            return true;
        }
    }
}
