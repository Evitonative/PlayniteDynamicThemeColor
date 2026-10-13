using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using DynamicThemeColor.Settings;
using DynamicThemeColor.Theming;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DynamicThemeColor {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DynamicThemeColor : GenericPlugin {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        private SettingsViewModel Settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("a7a76873-8465-4273-a8a6-d460550ce627");

        public PaletteCache PaletteCache{ get; }
        private ThemeRegistry ThemeRegistry { get; }
        private ThemeController ThemeController { get; }

        public DynamicThemeColor(IPlayniteAPI api) : base(api) {
            Settings = new SettingsViewModel(this);
            Properties = new GenericPluginProperties {
                HasSettings = true,
            };

            PaletteCache = new PaletteCache(api, Debugger.IsAttached);

            ThemeRegistry = new ThemeRegistry();
            ThemeRegistry.LoadThemes();

            ThemeController = new ThemeController(
                api,
                PaletteCache,
                ThemeRegistry,
                () => Settings?.Settings
            );
        }

        public override ISettings GetSettings(bool firstRunSettings) {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunView) {
            return new SettingsView();
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args) {
            Game game = args.NewValue?.FirstOrDefault();
            ThemeController.UpdateTheme(game);
        }

        public void RefreshTheme() {
            Game game = PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
            ThemeController.UpdateTheme(game);
        }
    }
}
