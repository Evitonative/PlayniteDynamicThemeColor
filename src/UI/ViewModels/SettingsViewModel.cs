using System.Collections.Generic;
using System.Linq;
using DynamicThemeColor.Model;
using DynamicThemeColor.UI.ViewModels.SettingsTabs;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;

namespace DynamicThemeColor.UI.ViewModels {
    public class SettingsViewModel : ObservableObject, ISettings {
        public GeneralSettingsViewModel General { get; }
        public SchemeSettingsViewModel Schemes { get; }
        public FilterSettingsViewModel Filters { get; }
        public AdvancedSettingsViewModel Advanced { get; }

        private AddonSettings EditingClone { get; set; }
        private AddonSettings Settings {
            get => addon.AddonSettings;
            set => addon.AddonSettings = value;
        }

        private readonly ILogger logger = LogManager.GetLogger();
        private readonly Addon addon;

        public SettingsViewModel(Addon addon) {
            this.addon = addon;

            General = new GeneralSettingsViewModel(addon);
            Schemes = new SchemeSettingsViewModel(addon);
            Filters = new FilterSettingsViewModel(addon);
            Advanced = new AdvancedSettingsViewModel(addon, Filters);

        }

        public void BeginEdit() {
            logger.Debug("Settings editing session started (BeginEdit). Creating editing clone.");
            EditingClone = Serialization.GetClone(Settings);

            General.BeginEdit();
            Schemes.BeginEdit();
            Filters.BeginEdit();
            Advanced.BeginEdit();
        }

        public void CancelEdit() {
            logger.Debug("Settings editing session cancelled (CancelEdit). Reverting changes.");

            General.CancelEdit();
            Schemes.CancelEdit();
            Filters.CancelEdit();
            Advanced.CancelEdit();

            Settings = EditingClone;
        }

        public void EndEdit() {
            logger.Debug("Settings editing session committed (EndEdit). Saving settings and refreshing SchemeEngine.");

            General.EndEdit();
            Schemes.EndEdit();
            Filters.EndEdit();
            Advanced.EndEdit();

            EndEditRefresh();
        }

        public void EndEditRefresh() {
            addon.SavePluginSettings(Settings);
            addon.SchemeEngine.RefreshBrushes();

            Game currentGame = addon.PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
            FilterRegistry.ClearFilterCaches();
            addon.ThemingEngine.PaletteCache.Clear();
            addon.ThemingEngine.UnapplyPalette();
            addon.ThemingEngine.ApplyPalette(currentGame);
        }

        public bool VerifySettings(out List<string> errors) {
            bool result = General.VerifySettings(out List<string> generalErrors);
            errors = generalErrors.ToList();

            if (!Schemes.VerifySettings(out List<string> schemeErrors)) result = false;
            errors.AddRange(schemeErrors);

            if (!Filters.VerifySettings(out List<string> filerErrors)) result = false;
            errors.AddRange(filerErrors);

            if (!Advanced.VerifySettings(out List<string> advancedErrors)) result = false;
            errors.AddRange(advancedErrors);
            return result;
        }
    }
}
