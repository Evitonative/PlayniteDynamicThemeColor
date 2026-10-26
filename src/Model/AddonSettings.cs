using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable RedundantDefaultMemberInitializer
namespace DynamicThemeColor.Model {
    public class AddonSettings : ObservableObject {
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

        // Scheme Settings
        private Dictionary<string, Dictionary<string, bool>> schemeOptionOverrides = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Dictionary<string, bool>> SchemeOptionOverrides {
            get => schemeOptionOverrides;
            set => SetValue(ref schemeOptionOverrides, value);
        }
    }
}
