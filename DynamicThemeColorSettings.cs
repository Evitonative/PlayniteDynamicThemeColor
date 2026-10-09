using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace DynamicThemeColor
{
    public class DynamicThemeColorSettings : ObservableObject
    {
        private string _defaultSeedColorHex = "#1cd8fc";
        private bool _preferCoverArt = true;
        private int _colorSelectionRange = 1;
        
        private bool _applyInListView = true;
        private bool _applyInDetailsView = true;
        private bool _applyInGridView = true;

        public string DefaultSeedColorHex
        {
            get => _defaultSeedColorHex;
            set => SetValue(ref _defaultSeedColorHex, value);
        }
        
        public bool PreferCoverArt
        {
            get => _preferCoverArt;
            set => SetValue(ref _preferCoverArt, value);
        }

        public int ColorSelectionRange
        {
            get => _colorSelectionRange;
            set => SetValue(ref _colorSelectionRange, value);
        }
        
        public bool ApplyInListView
        {
            get => _applyInListView;
            set => SetValue(ref _applyInListView, value);
        }

        public bool ApplyInDetailsView
        {
            get => _applyInDetailsView;
            set => SetValue(ref _applyInDetailsView, value);
        }

        public bool ApplyInGridView
        {
            get => _applyInGridView;
            set => SetValue(ref _applyInGridView, value);
        }
    }

    public class DynamicThemeColorSettingsViewModel : ObservableObject, ISettings
    {
        private readonly DynamicThemeColor _plugin;
        private DynamicThemeColorSettings EditingClone { get; set; }
        private DynamicThemeColorSettings _settings;
        
        public RelayCommand ClearCacheCommand { get; }
        
        public DynamicThemeColorSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public DynamicThemeColorSettingsViewModel(DynamicThemeColor plugin)
        {
            _plugin = plugin;
            var savedSettings = plugin.LoadPluginSettings<DynamicThemeColorSettings>();
            Settings = savedSettings ?? new DynamicThemeColorSettings();
            
            ClearCacheCommand = new RelayCommand(() => _plugin.ClearCache(silent: false));
        }

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(Settings);
            _plugin.ClearCache(silent: true);
            _plugin.RefreshTheme();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
