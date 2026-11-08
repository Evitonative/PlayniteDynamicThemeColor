using System;
using System.Collections.Generic;

namespace DynamicThemeColor.Model.Filters {
    public class FilterConfiguration : ObservableObject {
        private Guid filterId;
        public Guid FilterId {
            get => filterId;
            set => SetValue(ref filterId, value);
        }

        private bool isEnabled = true;
        public bool IsEnabled {
            get => isEnabled;
            set {
                SetValue(ref isEnabled, value);
                OnPropertyChanged(string.Empty);
            }
        }

        private object settings;
        public object Settings {
            get => settings;
            set => SetValue(ref settings, value);
        }
    }
}
