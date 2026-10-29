using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using DynamicThemeColor.Model;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;

namespace DynamicThemeColor.UI.Views.Settings {
    public class SettingsViewModel : ObservableObject, ISettings {
        private readonly Addon addon;
        private readonly ILogger logger = LogManager.GetLogger();

        private AddonSettings EditingClone { get; set; }

        public AddonSettings Settings => addon.AddonSettings;

        public Array AvailableStyles { get; } = Enum.GetValues(typeof(Style));
        public ObservableCollection<SchemeItemViewModel> ActiveSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public ObservableCollection<SchemeItemViewModel> OtherSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public bool HasOtherSchemes => OtherSchemes.Count > 0;

        public SettingsViewModel(Addon addon) {
            this.addon = addon;
            LoadSchemeCollections();
        }

        public void BeginEdit() {
            logger.Debug("Settings editing session started (BeginEdit). Creating editing clone.");
            EditingClone = Serialization.GetClone(addon.AddonSettings);
        }

        public void CancelEdit() {
            logger.Debug("Settings editing session cancelled (CancelEdit). Reverting changes.");
            addon.AddonSettings = EditingClone;
        }

        public void EndEdit() {
            logger.Debug("Settings editing session committed (EndEdit). Saving settings and refreshing SchemeEngine.");
            addon.SavePluginSettings(addon.AddonSettings);
            addon.SchemeEngine.RefreshBrushes();

            Game currentGame = addon.PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
            addon.ThemingEngine.PaletteCache.Clear();
            addon.ThemingEngine.ApplyPalette(currentGame);
        }

        public bool VerifySettings(out List<string> errors) {
            errors = new List<string>();
            return true;
        }

        public void LoadSchemeCollections() {
            logger.Trace("Reloading active and other scheme collections for UI view.");
            ActiveSchemes.Clear();
            OtherSchemes.Clear();

            SchemeEngine engine = addon.SchemeEngine;
            if (engine?.Schemes == null ) return;

            if (engine.ActiveSchemeChain != null)
                foreach (string schemeName in engine.ActiveSchemeChain)
                    if (engine.Schemes.TryGetValue(schemeName, out ColorScheme scheme))
                        ActiveSchemes.Add(new SchemeItemViewModel(scheme, () => addon.AddonSettings));

            foreach (SchemeItemViewModel itemVm
                     in from kvp in engine.Schemes
                     let schemeName = kvp.Key
                     let scheme = kvp.Value
                     let itemVm = new SchemeItemViewModel(scheme, () => addon.AddonSettings)
                     where engine.ActiveSchemeChain == null || !engine.ActiveSchemeChain.Contains(schemeName) select itemVm)
                OtherSchemes.Add(itemVm);
        }

        public RelayCommand PickColorCommand => new RelayCommand(() => {
            using (ColorDialog dialog = new ColorDialog()) {
                try {
                    if (ColorConverter.ConvertFromString(addon.AddonSettings.FallbackSeedColor) is Color c) {
                        dialog.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                    }
                } catch {
                    // ignored
                }

                if (dialog.ShowDialog() == DialogResult.OK) {
                    addon.AddonSettings.FallbackSeedColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                }
            }
        });
    }
}
