using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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

            yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextCopyGameId"),
                Action = (args) => {
                    Guid? selectedGame = args.Games.FirstOrDefault()?.Id;
                    if (selectedGame == null) return;
                    Clipboard.SetText(selectedGame.ToString());
                },
                MenuSection = menuRoot,
            };

            yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                Action = (args) => {
                    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    bool? result = AddListFilter(
                        Guid.Parse(GameBlacklistFilter.FilterId),
                        args.Games.FirstOrDefault(),
                        ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                        ResourceProvider.GetString("LOCDTCContextBlacklistGameDuplicate"));
                    switch (result) {
                        case true:
                            if (!shiftHeld) PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString("LOCDTCContextBlacklistGameAdded"),
                                ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        case false:
                            PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString("LOCDTCContextBlacklistGameFailed"),
                                ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        case null:
                            return;
                    }
                },
                MenuSection = menuRoot,
            };

            yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextWhitelistGame"),
                Action = (args) => {
                    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    bool? result = AddListFilter(
                        Guid.Parse(GameWhitelistFilter.FilterId),
                        args.Games.FirstOrDefault(),
                        ResourceProvider.GetString("LOCDTCContextWhitelistGame"),
                        ResourceProvider.GetString("LOCDTCContextWhitelistGameDuplicate"));
                    switch (result) {
                        case true:
                            if (!shiftHeld) PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString("LOCDTCContextWhitelistGameAdded"),
                                ResourceProvider.GetString("LOCDTCContextWhitelistGame"),
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        case false:
                            PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString("LOCDTCContextWhitelistGameFailed"),
                                ResourceProvider.GetString("LOCDTCContextWhitelistGame"),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        case null:
                            return;
                    }
                },
                MenuSection = menuRoot,
            };

            yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextOverrideGameSeed"),
                Action = (args) => {
                    Game game = args.Games.FirstOrDefault();
                    if (game == null) return;

                    Guid filterId = Guid.Parse(GameOverrideSeedColor.FilterId);
                    GameOverrideSeedColorSettings oldInstanceSettings = AddonSettings.SeedFilters
                        .Where(f => f.FilterId == filterId)
                        .Select(FilterRegistry.CreateInstance)
                        .Select(f => f?.SettingsObject)
                        .Where(f => f != null)
                        .OfType<GameOverrideSeedColorSettings>()
                        .Where(f => {
                            if (!Guid.TryParse(f.GameId, out Guid id)) return false;
                            return id == game.Id;
                        })
                        .LastOrDefault();

                     if(!MaterialColorHelper.ExecutePickColor(out string newColor)) return;

                    if (oldInstanceSettings != null) {
                        MessageBoxResult result = PlayniteApi.Dialogs.ShowMessage(
                            ResourceProvider.GetString("LOCDTCContextOverrideGameSeedPreviousFound"),
                            ResourceProvider.GetString("LOCDTCContextOverrideGameSeed"),
                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Cancel) return;
                        if (result != MessageBoxResult.Yes) oldInstanceSettings = null;
                    }

                    GameOverrideSeedColorSettings settings = oldInstanceSettings;
                    if (oldInstanceSettings == null) {
                        SchemeFilter simpleInstance = FilterRegistry.CreateSimpleInstance(filterId);
                        if (!(simpleInstance?.SettingsObject is GameOverrideSeedColorSettings newSettings)) return;
                        settings = newSettings;
                    }

                    settings.GameId = game.Id.ToString();
                    settings.GameName = game.Name;
                    settings.Hex = newColor;

                    if (oldInstanceSettings == null) {
                        FilterConfiguration config = new FilterConfiguration {
                            FilterId = filterId,
                            IsEnabled = true,
                            Settings = settings,
                        };

                        AddonSettings.SeedFilters.Add(config);
                    }
                    SettingsView.EndEditRefresh();
                },
                MenuSection = menuRoot,
            };
        }

        private bool? AddListFilter(Guid filterId, Game game, string dialogTitle, string dialogTitleMessage) {
            if (game == null) return false;

            bool exists = AddonSettings.GameFilters
                .Where(f => f.FilterId == filterId)
                .Select(FilterRegistry.CreateInstance)
                .Select(f => f?.SettingsObject)
                .Where(f => f != null)
                .OfType<GameContextMenuAddedFilterSettings>()
                .Any(s => Guid.TryParse(s.GameId, out Guid id) && id == game.Id);

            if (exists) {
                MessageBoxResult result = PlayniteApi.Dialogs.ShowMessage(
                    dialogTitleMessage, dialogTitle,
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return null;
            }

            SchemeFilter simpleInstance = FilterRegistry.CreateSimpleInstance(filterId);
            if (!(simpleInstance?.SettingsObject is GameContextMenuAddedFilterSettings settings)) return false;

            settings.GameId = game.Id.ToString();
            settings.GameName = game.Name;

            FilterConfiguration config = new FilterConfiguration {
                FilterId = filterId,
                IsEnabled = true,
                Settings = settings,
            };

            AddonSettings.GameFilters.Add(config);
            SettingsView.EndEditRefresh();
            return true;
        }
    }
}
