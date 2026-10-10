using System;
using System.Collections.Generic;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace DynamicThemeColor
{
    public class DynamicThemeColorSettings : ObservableObject
    {
        // View Targets
        private bool _applyInListView = true;
        public bool ApplyInListView
        {
            get => _applyInListView;
            set => SetValue(ref _applyInListView, value);
        }

        private bool _applyInDetailsView = true;
        public bool ApplyInDetailsView
        {
            get => _applyInDetailsView;
            set => SetValue(ref _applyInDetailsView, value);
        }

        private bool _applyInGridView = true;
        public bool ApplyInGridView
        {
            get => _applyInGridView;
            set => SetValue(ref _applyInGridView, value);
        }
        
        // Color Generation
        private Style _paletteStyle = Style.TonalSpot;
        public Style PaletteStyle 
        { 
            get => _paletteStyle; 
            set => SetValue(ref _paletteStyle, value); 
        }
        
        private bool _preferCoverArt = true;
        public bool PreferCoverArt
        {
            get => _preferCoverArt;
            set => SetValue(ref _preferCoverArt, value);
        }
        
        private int _colorSelectionRange = 1;
        public int ColorSelectionRange
        {
            get => _colorSelectionRange;
            set => SetValue(ref _colorSelectionRange, value);
        }
        
        private string _defaultSeedColorHex = "#1cd8fc";
        public string DefaultSeedColorHex
        {
            get => _defaultSeedColorHex;
            set => SetValue(ref _defaultSeedColorHex, value);
        }
        
        private bool _useSeedColorForGlyph = false;
        public bool UseSeedColorForGlyph
        {
            get => _useSeedColorForGlyph;
            set => SetValue(ref _useSeedColorForGlyph, value);
        }
    }

    public class DynamicThemeColorSettingsViewModel : ObservableObject, ISettings
    {
        private readonly DynamicThemeColor _plugin;
        private DynamicThemeColorSettings EditingClone { get; set; }
        private DynamicThemeColorSettings _settings;
        
        public Array AvailableStyles { get; } = Enum.GetValues(typeof(Style));
        public RelayCommand ClearCacheCommand { get; }
        
        private bool _hasChanges;
        
        public DynamicThemeColorSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != null) _settings.PropertyChanged -= Settings_PropertyChanged;
                
                _settings = value;
                
                if (_settings != null) _settings.PropertyChanged += Settings_PropertyChanged;
                
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
        
        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _hasChanges = true;
            _plugin.ClearCache(silent: true);
            _plugin.RefreshTheme();
        }
        
        public RelayCommand PickColorCommand => new RelayCommand(() =>
        {
            using (var dialog = new System.Windows.Forms.ColorDialog())
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

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Settings.DefaultSeedColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                }
            }
        });

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            _hasChanges = false;
        }

        public void CancelEdit()
        {
            Settings = EditingClone;

            if (!_hasChanges) return;
            _plugin.ClearCache(silent: true);
            _plugin.RefreshTheme();
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
