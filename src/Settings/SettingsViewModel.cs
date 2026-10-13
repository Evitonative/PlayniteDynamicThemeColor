using System;
using System.Collections.Generic;
using System.Windows.Forms;
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

        // ReSharper disable once UnusedMember.Global
        public Array AvailableStyles { get; } = Enum.GetValues(typeof(Style));
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public RelayCommand ClearCacheCommand { get; }

        private bool hasChanges;

        public AddonSettings Settings
        {
            get => settings;
            // ReSharper disable once MemberCanBePrivate.Global
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

            ClearCacheCommand = new RelayCommand(() =>
            {
                this.plugin.PaletteCache.Clear(notify: true);
                this.plugin.RefreshTheme();
            });
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            hasChanges = true;
            plugin.PaletteCache.Clear();
            plugin.RefreshTheme();
        }

        public RelayCommand PickColorCommand => new RelayCommand(() =>
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                try
                {
                    if (System.Windows.Media.ColorConverter.ConvertFromString(Settings.DefaultSeedColorHex) is System.Windows.Media.Color c)
                    {
                        dialog.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                    }
                }
                catch
                {
                    // ignored
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Settings.DefaultSeedColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                }
            }
        });

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            hasChanges = false;
        }

        public void CancelEdit()
        {
            Settings = EditingClone;

            if (!hasChanges) return;
            plugin.PaletteCache.Clear();
            plugin.RefreshTheme();
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);

            if (EditingClone.EnableHarmonyDetailsPanelBrush && !Settings.EnableHarmonyDetailsPanelBrush)
            {
                plugin.PlayniteApi.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCDynamicThemeColorRestartRequiredMsg"),
                    ResourceProvider.GetString("LOCDynamicThemeColorRestartRequiredTitle")
                );
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
