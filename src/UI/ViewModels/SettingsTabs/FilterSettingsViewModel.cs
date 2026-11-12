using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using DynamicThemeColor.Core;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.UI.Views;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels.SettingsTabs {
    public class FilterSettingsViewModel : ObservableObject, ISettings {
        public ObservableCollection<FilterItemViewModel> GameFilters { get; set; } = new ObservableCollection<FilterItemViewModel>();
        public ObservableCollection<FilterItemViewModel> SeedFilters { get; set; } = new ObservableCollection<FilterItemViewModel>();
        public ObservableCollection<FilterItemViewModel> PaletteFilters { get; set; } = new ObservableCollection<FilterItemViewModel>();
        public ObservableCollection<FilterItemViewModel> BrushFilters { get; set; } = new ObservableCollection<FilterItemViewModel>();
        private readonly List<Guid> sessionAddedInstances = new List<Guid>();

        public RelayCommand AddGameFilterCommand { get; }
        public RelayCommand AddSeedFilterCommand { get; }
        public RelayCommand AddPaletteFilterCommand { get; }
        public RelayCommand AddBrushFilterCommand { get; }

        private AddonSettings Settings => addon.AddonSettings;

        private readonly ILogger logger = LogManager.GetLogger();
        private readonly Addon addon;

        public FilterSettingsViewModel(Addon addon) {
            this.addon = addon;

            AddGameFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Game));
            AddSeedFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Seed));
            AddPaletteFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Palette));
            AddBrushFilterCommand = new RelayCommand(() => ExecuteAddFilter(FilterStage.Brush));

            WarmupFilterInstances();
        }

        public void BeginEdit() {
            sessionAddedInstances.Clear();
            LoadFilterCollections();
        }

        public void EndEdit() {
            sessionAddedInstances.Clear();
            SaveFilterCollections();
        }

        public void CancelEdit() {
            foreach (Guid instanceId in sessionAddedInstances) FilterRegistry.RemoveFilterInstance(instanceId);
            sessionAddedInstances.Clear();
        }

        public bool VerifySettings(out List<string> errors) {
            errors = new List<string>();
            return true;
        }

        public void WarmupFilterInstances() {
            FilterRegistry.ClearFilterInstances();
            foreach (FilterConfiguration config in Settings.GameFilters) WarmupFilter(config);
            foreach (FilterConfiguration config in Settings.SeedFilters) WarmupFilter(config);
            foreach (FilterConfiguration config in Settings.PaletteFilters) WarmupFilter(config);
            foreach (FilterConfiguration config in Settings.BrushFilters) WarmupFilter(config);
            return;

            void WarmupFilter(FilterConfiguration config) {
                try {
                    FilterRegistry.TryCreateFilterInstance(config);
                } catch (KeyNotFoundException) {
                    logger.Warn($"Could not create filter instance {config.InstanceId}. Filter is using a not loaded filter {config.FilterId}");
                }
            }
        }

        private void LoadFilterCollections() {
            GameFilters.Clear();
            if (Settings.GameFilters != null)
                foreach (FilterConfiguration config in Settings.GameFilters) {
                    FilterItemViewModel vm = FilterSettingsListToVm(config);
                    if (vm != null) GameFilters.Add(vm);
                }

            SeedFilters.Clear();
            if (Settings.SeedFilters != null)
                foreach (FilterConfiguration config in Settings.SeedFilters) {
                    FilterItemViewModel vm = FilterSettingsListToVm(config);
                    if (vm != null) SeedFilters.Add(vm);
                }

            PaletteFilters.Clear();
            if (Settings.PaletteFilters != null)
                foreach (FilterConfiguration config in Settings.PaletteFilters) {
                    FilterItemViewModel vm = FilterSettingsListToVm(config);
                    if (vm != null) PaletteFilters.Add(vm);
                }

            BrushFilters.Clear();
            if (Settings.BrushFilters == null) return;
            foreach (FilterConfiguration config in Settings.BrushFilters) {
                FilterItemViewModel vm = FilterSettingsListToVm(config);
                if (vm != null) BrushFilters.Add(vm);
            }
        }

        private FilterItemViewModel FilterSettingsListToVm(FilterConfiguration config) {
            if (!FilterRegistry.TryGetFilterInstance(config.InstanceId, out SchemeFilter instance)) return null;
            SchemeFilterAttribute attr = instance?.GetType().GetCustomAttribute<SchemeFilterAttribute>();
            return attr == null ? null : new FilterItemViewModel(instance, attr, addon.PlayniteApi);
        }

        private void SaveFilterCollections() {
            Settings.GameFilters = GameFilters.Select(vm => vm.Instance.FilterConfiguration).ToObservable();
            Settings.SeedFilters = SeedFilters.Select(vm => vm.Instance.FilterConfiguration).ToObservable();
            Settings.PaletteFilters = PaletteFilters.Select(vm => vm.Instance.FilterConfiguration).ToObservable();
            Settings.BrushFilters = BrushFilters.Select(vm => vm.Instance.FilterConfiguration).ToObservable();
        }

        private void ExecuteAddFilter(FilterStage stage) {
            IEnumerable<FilterDescriptorNew> compatibleFilters = FilterRegistry.GetAllFilters()
                .Where(f => {
                    string methodName;
                    switch (stage) {
                        case FilterStage.Game: methodName = nameof(SchemeFilter.ApplyGameFilter); break;
                        case FilterStage.Seed: methodName = nameof(SchemeFilter.ApplySeedFilter); break;
                        case FilterStage.Palette: methodName = nameof(SchemeFilter.ApplyPaletteFilter); break;
                        case FilterStage.Brush: methodName = nameof(SchemeFilter.ApplyBrushFilter); break;
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
            window.Icon = new BitmapImage(
                new Uri("pack://application:,,,/DynamicThemeColor;component/assets/img/icon.ico", UriKind.Absolute)
            );

            AddFilterViewModel viewModel = new AddFilterViewModel(compatibleFilters, stage, Settings.EditFilterAfterAdding);
            AddFilterView selectorView = new AddFilterView { DataContext = viewModel };
            window.Content = selectorView;

            bool? result = window.ShowDialog();
            if (result != true || viewModel.SelectedItem == null)
                return;

            Guid? filterId = viewModel.SelectedItem?.FilterId;
            if (!filterId.HasValue) return;

            FilterConfiguration filterConfig = FilterRegistry.CreateNewFilterInstance(filterId.Value);

            if (filterConfig == null ||
                !FilterRegistry.TryGetFilterInstance(filterConfig.InstanceId, out SchemeFilter instance)) {
                addon.PlayniteApi.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCDTCFilterSelectFailed"),
                    ResourceProvider.GetString("LOCDTCFilterSelectWindowTitel"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            sessionAddedInstances.Add(filterConfig.InstanceId);

            SchemeFilterAttribute attr = instance.GetType().GetCustomAttribute<SchemeFilterAttribute>();
            if (attr == null) {
                addon.PlayniteApi.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCDTCFilterSelectFailed"),
                    ResourceProvider.GetString("LOCDTCFilterSelectWindowTitel"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FilterItemViewModel itemVm = new FilterItemViewModel(instance, attr, addon.PlayniteApi);

            switch (stage) {
                case FilterStage.Game:
                    GameFilters.Add(itemVm);
                    break;
                case FilterStage.Seed:
                    SeedFilters.Add(itemVm);
                    break;
                case FilterStage.Palette:
                    PaletteFilters.Add(itemVm);
                    break;
                case FilterStage.Brush:
                    BrushFilters.Add(itemVm);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }

            Settings.EditFilterAfterAdding = viewModel.EditOnAdd;
            if (viewModel.EditOnAdd) itemVm.EditFilter();
        }
    }

    public class FilterItemViewModel : ObservableObject, IEditableItem {
        public SchemeFilter Instance { get; }
        private readonly SchemeFilterAttribute attribute;
        private readonly IPlayniteAPI playniteApi;

        private FilterConfiguration FilterConfiguration => Instance.FilterConfiguration;

        public FilterItemViewModel(SchemeFilter instance, SchemeFilterAttribute attribute, IPlayniteAPI playniteApi) {
            Instance = instance;
            this.attribute = attribute;
            this.playniteApi = playniteApi;
        }

        public bool IsEnabled {
            get => FilterConfiguration.IsEnabled;
            set {
                if (FilterConfiguration.IsEnabled == value) return;
                FilterConfiguration.IsEnabled = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName => Instance.Name ?? attribute.Name;

        public void EditFilter() {
            EnsureStronglyTypedSettings();
            object filterSettingsClone = FilterConfiguration.Settings != null ? CloneObject(FilterConfiguration.Settings) : null;

            Window window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
            });

            window.Title = string.Format(
                ResourceProvider.GetString("LOCDTCFilterSettingsWindowTitel"),
                DisplayName);
            window.Width = 550;
            window.MinWidth = 400;
            window.SizeToContent = SizeToContent.Height;
            window.MinHeight = 200;
            window.MaxHeight = 450;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Icon = new BitmapImage(
                new Uri("pack://application:,,,/DynamicThemeColor;component/assets/img/icon.ico", UriKind.Absolute)
            );
            window.Loaded += (sender, args) => window.MaxHeight = double.PositiveInfinity;

            FilterInstanceSettingsViewModel viewModel = new FilterInstanceSettingsViewModel(filterSettingsClone, FilterConfiguration);
            FilterInstanceSettingsView selectorView = new FilterInstanceSettingsView { DataContext = viewModel };
            window.Content = selectorView;

            bool? result = window.ShowDialog();
            if (result != true) return;

            if (FilterConfiguration.Settings != null) {
                if (viewModel.FilterSettings == null) return;
                if (viewModel.FilterSettings.GetType() != FilterConfiguration.Settings.GetType()) return;

                CopyProperties(viewModel.FilterSettings, FilterConfiguration.Settings);
            }

            IsEnabled = viewModel.IsEnabled;
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(IsEnabled));
        }

        private void EnsureStronglyTypedSettings() {
            FilterConfiguration oldFilterSettings = FilterConfiguration;
            Instance.FilterConfiguration = null;
            Instance.FilterConfiguration = oldFilterSettings;
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
