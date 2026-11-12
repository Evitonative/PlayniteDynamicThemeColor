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

        public ICommand ClearCacheCommand { get; }
        public RelayCommand<CachedGameItemViewModel> RemoveCachedGameCommand { get; }
        public ICommand ClearSchemeThemeCombinationsCommand { get; }
        public ICommand ReprepareSchemesCommand { get; }
        public ICommand RefreshBrushesCommand { get; }
        public ICommand ReloadFiltersCommand { get; }
        public ICommand RefreshFilterInstancesCommand { get; }

        public AddonSettings Settings {
            get => addon.AddonSettings;
            set => addon.AddonSettings = value;
        }

        private readonly Addon addon;
        private readonly FilterSettingsViewModel filtersSettings;

        public AdvancedSettingsViewModel(Addon addon, FilterSettingsViewModel filtersSettings) {
            this.addon = addon;
            this.filtersSettings = filtersSettings;

            ClearCacheCommand = new RelayCommand(ExecuteClearCache);
            RemoveCachedGameCommand = new RelayCommand<CachedGameItemViewModel>(ExecuteRemoveCachedItem);
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
