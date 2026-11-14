using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels {
    public class FilterInstanceSettingsViewModel {
        public ObservableCollection<FilterInstanceSettingsOptionViewModel> FilterSettingsItems { get; } =
            new ObservableCollection<FilterInstanceSettingsOptionViewModel>();

        [CanBeNull] public object FilterSettings { get; }
        public bool IsEnabled { get; set; }
        public bool HasSettings => FilterSettings != null && FilterSettingsItems.Any();
        public bool HasNoSettings => !HasSettings;

        public RelayCommand<Window> SaveCommand { get; }
        public ICommand ResetAllCommand { get; }

        public FilterInstanceSettingsViewModel([CanBeNull] object editingSettings, FilterConfiguration configuration, FilterStage stage) {
            FilterSettings = editingSettings;
            IsEnabled = configuration.IsEnabled;

            CollectSettingsItems(stage);

            ResetAllCommand = new RelayCommand(ExecuteResetAll);
            SaveCommand = new RelayCommand<Window>(ExecuteSave);
        }

        private void CollectSettingsItems(FilterStage stage) {
            if (FilterSettings == null) return;
            Type type = FilterSettings.GetType();

            FilterSettingsItems.Clear();
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                FilterSettingAttribute attr = prop.GetCustomAttribute<FilterSettingAttribute>();
                if (attr != null && attr.Stages.Contains(stage)) FilterSettingsItems.Add(new FilterInstanceSettingsOptionViewModel(FilterSettings, prop, attr));
            }
        }

        private void ExecuteResetAll() {
            foreach (FilterInstanceSettingsOptionViewModel item in FilterSettingsItems) item.Reset();
        }

        private static void ExecuteSave(Window window) {
            if (window == null) return;
            window.DialogResult = true;
        }
    }
}
