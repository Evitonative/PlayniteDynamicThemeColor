using System;
using System.Collections.Generic;
using DynamicThemeColor.Model;
using DynamicThemeColor.Utils;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels.SettingsTabs {
    public class GeneralSettingsViewModel : ObservableObject, ISettings {
        public Array AvailableStyles { get; } = Enum.GetValues(typeof(Style));
        public RelayCommand PickColorCommand { get; }

        public AddonSettings Settings => addon.AddonSettings;

        private readonly Addon addon;

        public GeneralSettingsViewModel(Addon addon) {
            this.addon = addon;

            PickColorCommand = new RelayCommand(ExecuteColorPicker);
        }

        public void BeginEdit() {
        }

        public void EndEdit() {
        }

        public void CancelEdit() {
        }

        public bool VerifySettings(out List<string> errors) {
            errors = new List<string>();
            return true;
        }

        private void ExecuteColorPicker() {
            if (!ColorPicker.ExecutePickColor(out string color, defaultColor: Settings.FallbackSeedColor)) return;
            Settings.FallbackSeedColor = color;
        }
    }
}
