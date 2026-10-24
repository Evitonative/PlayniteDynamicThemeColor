using System;
using System.Windows.Controls;
using DynamicThemeColor.Settings;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace DynamicThemeColor {
    [UsedImplicitly]
    public class DynamicThemeColor : GenericPlugin {
        public override Guid Id { get; } = Guid.Parse("a7a76873-8465-4273-a8a6-d460550ce627");

        private SettingsViewModel Settings { get; [UsedImplicitly] set; }
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly SchemeEngine schemeEngine;

        public DynamicThemeColor(IPlayniteAPI api) : base(api) {
            Settings = new SettingsViewModel(this);
            Properties = new GenericPluginProperties {
                HasSettings = true,
            };

            schemeEngine = new SchemeEngine(this);
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
