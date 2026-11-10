using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DynamicThemeColor.Filters;
using DynamicThemeColor.Model.Filters;
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
                    bool result = AddListFilter(Guid.Parse(GameBlacklistFilter.FilterId), args.Games.FirstOrDefault());
                    if (result) {
                        if (!shiftHeld) {
                            PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString("LOCDTCContextBlacklistGameAdded"),
                                ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    } else PlayniteApi.Dialogs.ShowMessage(
                        ResourceProvider.GetString("LOCDTCContextBlacklistGameFailed"),
                        ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                },
                MenuSection = menuRoot,
            };

            yield return new GameMenuItem {
                Description = ResourceProvider.GetString("LOCDTCContextWhitelistGame"),
                Action = (args) => {
                    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    bool result = AddListFilter(Guid.Parse(GameWhitelistFilter.FilterId), args.Games.FirstOrDefault());
                    if (result) {
                        if (!shiftHeld) {
                            PlayniteApi.Dialogs.ShowMessage(
                                ResourceProvider.GetString("LOCDTCContextWhitelistGameAdded"),
                                ResourceProvider.GetString("LOCDTCContextBlacklistGame"),
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    } else {
                        PlayniteApi.Dialogs.ShowMessage(
                            ResourceProvider.GetString("LOCDTCContextWhitelistGameFailed"),
                            ResourceProvider.GetString("LOCDTCContextWhitelistGame"),
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                },
                MenuSection = menuRoot,
            };
        }

        private bool AddListFilter(Guid filterId, Game game) {
            if (game == null) return false;

            SchemeFilter simpleInstance = FilterRegistry.CreateSimpleInstance(filterId);

            if (!(simpleInstance?.SettingsObject is GameBlackWhitelistFilter settings)) return false;

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
