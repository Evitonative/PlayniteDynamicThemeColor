using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Style = MaterialColorUtilities.Palettes.Style;

namespace DynamicThemeColor.UI.Views {
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

        // Filters Tab
        public ObservableCollection<FilterItemViewModel> SeedFilters { get; } = new ObservableCollection<FilterItemViewModel>();
        public ObservableCollection<FilterItemViewModel> PaletteFilters { get; } = new ObservableCollection<FilterItemViewModel>();
        public ObservableCollection<FilterItemViewModel> BrushFilters { get; } = new ObservableCollection<FilterItemViewModel>();
        public RelayCommand AddSeedFilterCommand { get; }
        public RelayCommand AddPaletteFilterCommand { get; }
        public RelayCommand AddBrushFilterCommand { get; }

        // Advanced Tab
        public ObservableCollection<CachedGameItemViewModel> CachedGames { get; } = new ObservableCollection<CachedGameItemViewModel>();
        public int CachedGamesCount => addon.ThemingEngine.PaletteCache.Count;
        public RelayCommand ClearCacheCommand { get; }
        public RelayCommand<CachedGameItemViewModel> RemoveCachedGameCommand { get; }

        public ICommand ClearSchemeThemeCombinationsCommand { get; }
        public ICommand ReprepareSchemesCommand { get; }
        public ICommand RefreshBrushesCommand { get; }

        public SettingsViewModel(Addon addon) {
            this.addon = addon;

            PickColorCommand = new RelayCommand(ExecutePickColor);

            AddSeedFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Seed));
            AddPaletteFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Palette));
            AddBrushFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Brush));

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
            LoadFilterCollections();
            EditingClone = Serialization.GetClone(addon.AddonSettings);
            RefreshCachedGames();
        }

        public void CancelEdit() {
            logger.Debug("Settings editing session cancelled (CancelEdit). Reverting changes.");
            addon.AddonSettings = EditingClone;
        }

        public void EndEdit() {
            logger.Debug("Settings editing session committed (EndEdit). Saving settings and refreshing SchemeEngine.");
            SaveFilterCollections();

            addon.SavePluginSettings(addon.AddonSettings);
            addon.SchemeEngine.RefreshBrushes();

            Game currentGame = addon.PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
            addon.ThemingEngine.PaletteCache.Clear();
            FilterRegistry.ClearFilterCaches();
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

        private void LoadFilterCollections() {
            SeedFilters.Clear();
            if (Settings.SeedFilters != null)
                foreach (FilterConfiguration config in Settings.SeedFilters)
                    SeedFilters.Add(new FilterItemViewModel(config, addon.PlayniteApi));

            PaletteFilters.Clear();
            if (Settings.PaletteFilters != null)
                foreach (FilterConfiguration config in Settings.PaletteFilters)
                    PaletteFilters.Add(new FilterItemViewModel(config, addon.PlayniteApi));

            BrushFilters.Clear();
            if (Settings.BrushFilters == null) return;
            foreach (FilterConfiguration config in Settings.BrushFilters)
                BrushFilters.Add(new FilterItemViewModel(config, addon.PlayniteApi));
        }

        private void SaveFilterCollections() {
            Settings.SeedFilters = new List<FilterConfiguration>(SeedFilters.Select(vm => vm.Configuration));
            Settings.PaletteFilters = new List<FilterConfiguration>(PaletteFilters.Select(vm => vm.Configuration));
            Settings.BrushFilters = new List<FilterConfiguration>(BrushFilters.Select(vm => vm.Configuration));
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

        private void ExecuteAddFilter(FilterStage stage) {
            IEnumerable<FilterDescriptor> compatibleFilters = FilterRegistry.GetAvailableFilters().Values
                .Where(f => {
                    string methodName;
                    switch (stage) {
                        case FilterStage.Seed: methodName = nameof(SchemeFilter.ApplySeedFilters); break;
                        case FilterStage.Palette: methodName = nameof(SchemeFilter.ApplyPaletteFilters); break;
                        case FilterStage.Brush: methodName = nameof(SchemeFilter.ApplyBrushFilters); break;
                        default: throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
                    }

                    MethodInfo method = f.FilterType.GetMethod(
                        methodName, BindingFlags.Public | BindingFlags.Instance);

                    return method != null && method.DeclaringType != typeof(SchemeFilter);
                }).ToList();

            if (!compatibleFilters.Any()) {
                addon.PlayniteApi.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOCDTCFilterSelectNoFiltersForStage"), Enum.GetName(typeof(FilterStage), stage)),
                    ResourceProvider.GetString("LOCDTCFilterSelectWindowTitel"));
                return;
            }

            Window window = addon.PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
            });

            window.Title = ResourceProvider.GetString("LOCDTCFilterSelectWindowTitel");
            window.Width = 550;
            window.SizeToContent = SizeToContent.Height;
            window.MinHeight = 200;
            window.MaxHeight = 450;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Icon = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/DynamicThemeColor;component/assets/icon.ico", UriKind.Absolute)
            );

            AddFilterViewModel viewModel = new AddFilterViewModel(compatibleFilters, stage);
            AddFilterView selectorView = new AddFilterView { DataContext = viewModel };
            window.Content = selectorView;

            bool? result = window.ShowDialog();
            if (result != true || viewModel.SelectedItem == null)
                return;

            if (!(Activator.CreateInstance(viewModel.SelectedItem.FilterType) is SchemeFilter filterInstance))
                return;

            FilterConfiguration config = new FilterConfiguration {
                FilterId = viewModel.SelectedItem.Id,
                Settings = filterInstance.SettingsObject
            };

            switch (stage) {
                case FilterStage.Seed:
                    SeedFilters.Add(new FilterItemViewModel(config, addon.PlayniteApi));
                    break;
                case FilterStage.Palette:
                    PaletteFilters.Add(new FilterItemViewModel(config, addon.PlayniteApi));
                    break;
                case FilterStage.Brush:
                    BrushFilters.Add(new FilterItemViewModel(config, addon.PlayniteApi));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
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
            addon.AddonSettings.RememberedSchemeForThemes.Clear();
        }

        private void ExecuteReprepareSchemes() {
            addon.SchemeEngine.PrepareSchemes();
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

    public class SchemeItemViewModel {
        public RegisteredColorScheme Scheme { get; }
        public Dictionary<string, bool> OptionsDictionary { get; }

        public SchemeItemViewModel(RegisteredColorScheme scheme, Func<AddonSettings> addonSettings) {
            AddonSettings settings = addonSettings();
            Scheme = scheme;

            settings.SchemeOptionOverrides = settings.SchemeOptionOverrides ?? new Dictionary<Guid, Dictionary<string, bool>>();

            if (!settings.SchemeOptionOverrides.TryGetValue(scheme.Scheme.Id, out Dictionary<string, bool> dict)) {
                dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                settings.SchemeOptionOverrides[scheme.Scheme.Id] = dict;
            }
            OptionsDictionary = dict;
        }
    }

    public class FilterItemViewModel : ObservableObject, IEditableItem {
        private readonly IPlayniteAPI playniteApi;
        public FilterConfiguration Configuration { get; }

        public FilterItemViewModel(FilterConfiguration configuration, IPlayniteAPI playniteApi) {
            this.playniteApi = playniteApi;
            Configuration = configuration;
        }

        public bool IsEnabled {
            get => Configuration.IsEnabled;
            set {
                if (Configuration.IsEnabled == value) return;
                Configuration.IsEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName {
            get {
                FilterDescriptor descriptor = FilterRegistry.GetFilter(Configuration.FilterId);
                return descriptor?.Name ?? "Unknown Filter";
            }
        }

        public void EditFilter() {
            EnsureStronglyTypedSettings();
            object filterSettingsClone = Configuration.Settings != null ? CloneObject(Configuration.Settings) : null;

            Window window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
            });

            window.Title = string.Format(
                ResourceProvider.GetString("LOCDTCFilterSettingsWindowTitel"),
                FilterRegistry.CreateInstance(Configuration).Name);
            window.Width = 550;
            window.MinWidth = 400;
            window.SizeToContent = SizeToContent.Height;
            window.MinHeight = 200;
            window.MaxHeight = 450;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Icon = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/DynamicThemeColor;component/assets/icon.ico", UriKind.Absolute)
            );
            window.Loaded += (sender, args) => window.MaxHeight = double.PositiveInfinity;

            FilterSettingsViewModel viewModel = new FilterSettingsViewModel(filterSettingsClone, Configuration);
            FilterSettingsView selectorView = new FilterSettingsView { DataContext = viewModel };
            window.Content = selectorView;

            bool? result = window.ShowDialog();
            if (result != true) return;

            if (Configuration.Settings != null) {
                if (viewModel.FilterSettings == null) return;
                if (viewModel.FilterSettings.GetType() != Configuration.Settings.GetType()) return;

                CopyProperties(viewModel.FilterSettings, Configuration.Settings);
            }

            IsEnabled = viewModel.IsEnabled;
        }

        private void EnsureStronglyTypedSettings() {
            if (!(Configuration.Settings is JObject jObj)) return;
            Type correctType = FilterRegistry.CreateInstance(Configuration).SettingsObject?.GetType();
            Configuration.Settings = jObj.ToObject(correctType);
        }

        private static T CloneObject<T>(T source) where T : class, new() {
            if (source == null) return null;

            Type type = source.GetType();
            T clone = (T)Activator.CreateInstance(type);

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                object value = prop.GetValue(source);
                prop.SetValue(clone, value);
            }

            return clone;
        }

        private static void CopyProperties(object source, object destination) {
            if (source == null || destination == null) return;

            Type type = source.GetType();
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                object value = prop.GetValue(source);
                prop.SetValue(destination, value);
            }
        }
    }
}
