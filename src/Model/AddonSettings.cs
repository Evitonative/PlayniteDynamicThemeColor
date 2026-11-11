using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicThemeColor.Model.Filters;
using MaterialColorUtilities.Palettes;

// ReSharper disable RedundantDefaultMemberInitializer
namespace DynamicThemeColor.Model {
    public class AddonSettings : ObservableObject {
        // Settings Version
        private int version = 2;
        public int Version {
            get => version;
            set => SetValue(ref version, value);
        }

        // View Targets
        private bool applyInListView = true;
        public bool ApplyInListView {
            get => applyInListView;
            set => SetValue(ref applyInListView, value);
        }

        private bool applyInDetailsView = true;
        public bool ApplyInDetailsView {
            get => applyInDetailsView;
            set => SetValue(ref applyInDetailsView, value);
        }

        private bool applyInGridView = true;
        public bool ApplyInGridView {
            get => applyInGridView;
            set => SetValue(ref applyInGridView, value);
        }

        private bool applyInFullscreen = true;
        public bool ApplyInFullscreen {
            get => applyInFullscreen;
            set => SetValue(ref applyInFullscreen, value);
        }

        // Asset Priority Order
        private ObservableCollection<GameAssetType> assetPriority = new ObservableCollection<GameAssetType>();
        public ObservableCollection<GameAssetType> AssetPriority {
            get => assetPriority;
            set => SetValue(ref assetPriority, value);
        }

        public void EnsureAllEnumsPresent() {
            Array allValues = Enum.GetValues(typeof(GameAssetType));

            foreach (GameAssetType v in allValues) {
                bool notFound = true;

                foreach (GameAssetType existingValue in assetPriority) {
                    if (existingValue.Equals(v)) notFound = false;
                }

                if (notFound) assetPriority.Add(v);
            }
        }

        // Extraction & Fallback Defaults
        private Style paletteStyle = Style.TonalSpot;
        public Style PaletteStyle {
            get => paletteStyle;
            set => SetValue(ref paletteStyle, value);
        }

        private int colorSelectionRange = 1;
        public int ColorSelectionRange {
            get => colorSelectionRange;
            set => SetValue(ref colorSelectionRange, value);
        }

        private string fallbackSeedColor = "#80FF80";
        public string FallbackSeedColor {
            get => fallbackSeedColor;
            set => SetValue(ref fallbackSeedColor, value);
        }

        private bool useFallbackOnMissing = true;
        public bool UseFallbackOnMissing {
            get => useFallbackOnMissing;
            set => SetValue(ref useFallbackOnMissing, value);
        }

        // Scheme Settings
        private Dictionary<Guid, Dictionary<string, bool>> schemeOptionOverrides = new Dictionary<Guid, Dictionary<string, bool>>();
        public Dictionary<Guid, Dictionary<string, bool>> SchemeOptionOverrides {
            get => schemeOptionOverrides;
            set => SetValue(ref schemeOptionOverrides, value);
        }

        // Filters
        private List<FilterConfiguration> gameFilters = new List<FilterConfiguration>();
        public List<FilterConfiguration> GameFilters {
            get => gameFilters;
            set => SetValue(ref gameFilters, value);
        }

        private List<FilterConfiguration> seedFilters = new List<FilterConfiguration>();
        public List<FilterConfiguration> SeedFilters {
            get => seedFilters;
            set => SetValue(ref seedFilters, value);
        }

        private List<FilterConfiguration> paletteFilters = new List<FilterConfiguration>();
        public List<FilterConfiguration> PaletteFilters {
            get => paletteFilters;
            set => SetValue(ref paletteFilters, value);
        }

        private IEnumerable<FilterConfiguration> brushFilters = new List<FilterConfiguration>();
        public IEnumerable<FilterConfiguration> BrushFilters {
            get => brushFilters;
            set => SetValue(ref brushFilters, value);
        }

        // Advanced
        private bool cacheDisabled = false;
        public bool CacheDisabled {
            get => cacheDisabled;
            set => SetValue(ref cacheDisabled, value);
        }

        public Dictionary<string, Guid> RememberedSchemeForThemes { get; } = new Dictionary<string, Guid>();
    }
}
