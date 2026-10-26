using System;
using System.Windows.Controls;
using DynamicThemeColor.Model;
using DynamicThemeColor.UI.Views.Settings;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace DynamicThemeColor {
    [UsedImplicitly]
    public class Addon : GenericPlugin {
        public sealed override Guid Id { get; } = Guid.Parse("a7a76873-8465-4273-a8a6-d460550ce627");
        public readonly SchemeEngine SchemeEngine;

        private SettingsViewModel Settings { get; [UsedImplicitly] set; }

        public Addon(IPlayniteAPI api) : base(api) {
            Properties = new GenericPluginProperties {
                HasSettings = true,
            };

            AddonSettings savedSettings = LoadPluginSettings<AddonSettings>();
            AddonSettings addonSettings = savedSettings ?? new AddonSettings();
            addonSettings.EnsureAllEnumsPresent();

            SchemeEngine = new SchemeEngine(PlayniteApi, Id, addonSettings);
            Settings = new SettingsViewModel(this, addonSettings);
        }

        public override ISettings GetSettings(bool firstRunSettings) {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunView) {
            return new SettingsView();
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args) {

        }
    }
}
