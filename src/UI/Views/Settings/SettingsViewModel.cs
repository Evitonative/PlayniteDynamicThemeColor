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

        // General Tab
        public AddonSettings Settings => addon.AddonSettings;
        public Array AvailableStyles { get; } = Enum.GetValues(typeof(Style));
        public RelayCommand PickColorCommand { get; }

        // Schemes Tab
        public ObservableCollection<SchemeItemViewModel> ActiveSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public ObservableCollection<SchemeItemViewModel> OtherSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public bool HasOtherSchemes => OtherSchemes.Count > 0;

        // Advanced Tab
        public ObservableCollection<CachedGameItemViewModel> CachedGames { get; } = new ObservableCollection<CachedGameItemViewModel>();
        public int CachedGamesCount => addon.ThemingEngine.PaletteCache.Count;
        public RelayCommand ClearCacheCommand { get; }
        public RelayCommand<CachedGameItemViewModel> RemoveCachedGameCommand { get; }

        public RelayCommand ClearSchemeThemeCombinationsCommand { get; }
        public RelayCommand ReprepareSchemesCommand { get; }
        public RelayCommand RefreshBrushesCommand { get; }

        public SettingsViewModel(Addon addon) {
            this.addon = addon;

            PickColorCommand = new RelayCommand(ExecutePickColor);
            ClearCacheCommand = new RelayCommand(ExecuteClearCache);
            RemoveCachedGameCommand = new RelayCommand<CachedGameItemViewModel>(ExecuteRemoveCachedItem);
            ClearSchemeThemeCombinationsCommand = new RelayCommand(ExecuteClearSchemeThemeCombinations);
            ReprepareSchemesCommand = new RelayCommand(ExecuteReprepareSchemes);
            RefreshBrushesCommand = new RelayCommand(ExecuteRefreshBrushes);

            LoadSchemeCollections();
            RefreshCachedGames();
        }

        public void BeginEdit() {
            logger.Debug("Settings editing session started (BeginEdit). Creating editing clone.");
            EditingClone = Serialization.GetClone(addon.AddonSettings);
            RefreshCachedGames();
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
            if (engine?.RegisteredSchemes == null ) return;

            if (engine.ActiveSchemeChain != null)
                foreach (Guid schemeId in engine.ActiveSchemeChain)
                    if (engine.RegisteredSchemes.TryGetValue(schemeId, out RegisteredColorScheme scheme))
                        ActiveSchemes.Add(new SchemeItemViewModel(scheme, () => addon.AddonSettings));

            foreach (SchemeItemViewModel itemVm
                     in from kvp in engine.RegisteredSchemes
                     let schemeName = kvp.Key
                     let scheme = kvp.Value
                     let itemVm = new SchemeItemViewModel(scheme, () => addon.AddonSettings)
                     where engine.ActiveSchemeChain == null || !engine.ActiveSchemeChain.Contains(schemeName) select itemVm)
                OtherSchemes.Add(itemVm);
        }

        private void RefreshCachedGames() {
            CachedGames.Clear();
            foreach (Guid gameId in addon.ThemingEngine.PaletteCache.GetCachedGameIds()) {
                Game game = addon.PlayniteApi.Database.Games.Get(gameId);
                if (game != null)
                    CachedGames.Add(new CachedGameItemViewModel(gameId, game.Name));
            }

            OnPropertyChanged(nameof(CachedGamesCount));
            OnPropertyChanged(nameof(CachedGames));
        }

        private void ExecutePickColor() {
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
        }

        private void ExecuteClearCache() {
            addon.ThemingEngine.PaletteCache.Clear();
            RefreshCachedGames();
        }

        private void ExecuteRemoveCachedItem(CachedGameItemViewModel item) {
            if (item == null) return;
            addon.ThemingEngine.PaletteCache.Remove(item.GameId);
            RefreshCachedGames();
        }

        private void ExecuteClearSchemeThemeCombinations() {
            addon.AddonSettings.RememberedSchemeForThemes.Clear();
        }

        private void ExecuteReprepareSchemes() {
            addon.SchemeEngine.RefreshBrushes();
        }

        private void ExecuteRefreshBrushes() {
            addon.SchemeEngine.RefreshBrushes();
        }
    }

    public class CachedGameItemViewModel {
        public Guid GameId { get; }
        public string GameName { get; }

        public CachedGameItemViewModel(Guid gameId, string gameName) {
            GameId = gameId;
            GameName = gameName;
        }
    }
}
