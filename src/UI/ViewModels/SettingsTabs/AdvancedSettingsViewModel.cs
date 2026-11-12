using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DynamicThemeColor.Core;
using DynamicThemeColor.Model;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.UI.ViewModels.SettingsTabs {
    public class AdvancedSettingsViewModel : ObservableObject, ISettings {
        public ObservableCollection<CachedGameItemViewModel> CachedGames { get; } = new ObservableCollection<CachedGameItemViewModel>();
        public int CachedGamesCount => addon.ThemingEngine.PaletteCache.Count;
        public bool GamesAreCached => CachedGamesCount > 0;

        public ICommand ResetSettingsCommand { get; }
        public ICommand ClearCacheCommand { get; }
        public RelayCommand<CachedGameItemViewModel> RemoveCachedGameCommand { get; }
        public ICommand SchemeSelectCommand { get; }
        public ICommand ClearSchemeThemeCombinationsCommand { get; }
        public ICommand ReprepareSchemesCommand { get; }
        public ICommand RefreshBrushesCommand { get; }
        public ICommand ReloadFiltersCommand { get; }
        public ICommand RefreshFilterInstancesCommand { get; }

        public AddonSettings Settings {
            get => addon.AddonSettings;
            private set => addon.AddonSettings = value;
        }

        private readonly Addon addon;
        private readonly SettingsViewModel settingsMain;
        private readonly FilterSettingsViewModel filtersSettings;

        public AdvancedSettingsViewModel(Addon addon,
            SettingsViewModel settingsViewModel,
            FilterSettingsViewModel filtersSettings) {
            this.addon = addon;
            settingsMain = settingsViewModel;
            this.filtersSettings = filtersSettings;

            ResetSettingsCommand = new RelayCommand(ExecuteResetSettings);
            ClearCacheCommand = new RelayCommand(ExecuteClearCache);
            RemoveCachedGameCommand = new RelayCommand<CachedGameItemViewModel>(ExecuteRemoveCachedItem);
            SchemeSelectCommand = new RelayCommand(ExecuteSchemeSelectCommand);
            ClearSchemeThemeCombinationsCommand = new RelayCommand(ExecuteClearSchemeThemeCombinations);
            ReprepareSchemesCommand = new RelayCommand(ExecuteReprepareSchemes);
            RefreshBrushesCommand = new RelayCommand(ExecuteRefreshBrushes);
            ReloadFiltersCommand = new RelayCommand(ExecuteRefreshFilter);
            RefreshFilterInstancesCommand = new RelayCommand(ExecuteRefreshFilterInstances);

            RefreshCachedGames();
        }

        public void BeginEdit() {
            RefreshCachedGames();
        }

        public void EndEdit() {
        }

        public void CancelEdit() {
        }

        public bool VerifySettings(out List<string> errors) {
            errors = new List<string>();
            return true;
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
            OnPropertyChanged(nameof(GamesAreCached));
        }

        private void ExecuteResetSettings() {
            Settings = new AddonSettings();
            Settings.EnsureAllEnumsPresent();

            ExecuteClearCache();
            settingsMain.Schemes.ActiveSchemes.Clear();
            settingsMain.Schemes.OtherSchemes.Clear();
            settingsMain.Filters.GameFilters.Clear();
            settingsMain.Filters.SeedFilters.Clear();
            settingsMain.Filters.PaletteFilters.Clear();
            settingsMain.Filters.BrushFilters.Clear();

            addon.SchemeEngine.PrepareSchemes();
            addon.SchemeEngine.RefreshBrushes();

            Settings.SchemeOptionOverrides.Clear();
            settingsMain.Schemes.LoadSchemeCollections();

            settingsMain.EndEdit();
            settingsMain.General.OnPropertyChanged(string.Empty);
            settingsMain.Schemes.OnPropertyChanged(string.Empty);
            settingsMain.Filters.OnPropertyChanged(string.Empty);
            settingsMain.Advanced.OnPropertyChanged(string.Empty);
        }

        private void ExecuteClearCache() {
            addon.ThemingEngine.PaletteCache.Clear();
            FilterRegistry.ClearFilterCaches();
            RefreshCachedGames();
        }

        private void ExecuteRemoveCachedItem(CachedGameItemViewModel item) {
            if (item == null) return;
            addon.ThemingEngine.PaletteCache.Remove(item.GameId);
            RefreshCachedGames();
        }

        private void ExecuteSchemeSelectCommand() {
            addon.SchemeEngine.RefreshBrushes(forceUserColorPick: true);
        }

        private void ExecuteClearSchemeThemeCombinations() {
            Settings.RememberedSchemeForThemes.Clear();
        }

        private void ExecuteReprepareSchemes() {
            addon.SchemeEngine.PrepareSchemes();
        }

        private void ExecuteRefreshBrushes() {
            addon.SchemeEngine.RefreshBrushes();
        }

        private static void ExecuteRefreshFilter() {
            FilterRegistry.ReloadFilters();
        }

        private void ExecuteRefreshFilterInstances() {
            filtersSettings.WarmupFilterInstances();
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
