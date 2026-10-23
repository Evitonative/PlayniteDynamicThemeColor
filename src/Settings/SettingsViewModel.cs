using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace DynamicThemeColor.Settings
{
    public class SettingsViewModel : ObservableObject, ISettings
    {
        private readonly DynamicThemeColor plugin;
        private AddonSettings EditingClone { get; set; }
        private AddonSettings settings;

        private bool hasChanges;

        [UsedImplicitly]
        public AddonSettings Settings
        {
            get => settings;
            set
            {
                if (settings != null) settings.PropertyChanged -= Settings_PropertyChanged;

                settings = value;

                if (settings != null) settings.PropertyChanged += Settings_PropertyChanged;

                OnPropertyChanged();
            }
        }

        public SettingsViewModel(DynamicThemeColor plugin)
        {
            this.plugin = plugin;
            AddonSettings savedSettings = plugin.LoadPluginSettings<AddonSettings>();
            Settings = savedSettings ?? new AddonSettings();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            hasChanges = true;
        }

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            hasChanges = false;
        }

        public void CancelEdit()
        {
            Settings = EditingClone;

            if (!hasChanges) return;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
