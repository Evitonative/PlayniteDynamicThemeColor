using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using DynamicThemeColor.Model;

namespace DynamicThemeColor.UI.Components {
    public partial class ColorSchemeOptionsList : INotifyPropertyChanged {
        public ColorSchemeOptionsList() {
            InitializeComponent();
        }

        public string DisplayName {
            get {
                return ColorScheme == null ? string.Empty : ColorScheme.Scheme.DisplayName;
            }
        }

        public static readonly DependencyProperty ColorSchemeProperty = DependencyProperty.Register(
            nameof(ColorScheme),
            typeof(RegisteredColorScheme),
            typeof(ColorSchemeOptionsList),
            new PropertyMetadata(null, OnParametersChanged));

        public RegisteredColorScheme ColorScheme {
            get { return (RegisteredColorScheme)GetValue(ColorSchemeProperty); }
            set { SetValue(ColorSchemeProperty, value); }
        }

        public static readonly DependencyProperty SchemeOptionsProperty = DependencyProperty.Register(
            nameof(SchemeOptions),
            typeof(Dictionary<string, bool>),
            typeof(ColorSchemeOptionsList),
            new PropertyMetadata(null, OnParametersChanged));

        public Dictionary<string, bool> SchemeOptions {
            get { return (Dictionary<string, bool>)GetValue(SchemeOptionsProperty); }
            set { SetValue(SchemeOptionsProperty, value); }
        }

        public ObservableCollection<OptionItemViewModel> Options { get; } = new ObservableCollection<OptionItemViewModel>();

        private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ColorSchemeOptionsList control)) return;

            control.RebuildOptions();
            control.OnPropertyChanged(nameof(DisplayName));
        }

        private void RebuildOptions() {
            Options.Clear();

            if (ColorScheme == null || SchemeOptions == null) return;
            if (ColorScheme.Scheme.Brushes == null) return;

            foreach (KeyValuePair<string, ColorScheme.IBrushDefinition> kvp
                     in ColorScheme.Scheme.Brushes.Where(kvp => kvp.Value.Optional != null)) {
                Options.Add(new OptionItemViewModel(kvp.Key, kvp.Value.Optional, SchemeOptions));
            }

            FallbackText.Visibility = Options.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class OptionItemViewModel : ObservableObject {
            private readonly string brushKey;
            private readonly Dictionary<string, bool> schemeOptions;
            private readonly bool defaultValue;

            public string Label { get; }

            public bool IsEnabled {
                get => schemeOptions.TryGetValue(brushKey, out bool value) ? value : defaultValue;
                set {
                    if (schemeOptions.TryGetValue(brushKey, out bool current) && current == value) return;
                    schemeOptions[brushKey] = value;
                    OnPropertyChanged();
                }
            }

            public OptionItemViewModel(string brushKey, ColorScheme.OptionalConfig optional, Dictionary<string, bool> schemeOptions) {
                this.brushKey = brushKey;
                this.schemeOptions = schemeOptions;
                defaultValue = optional.Default;
                Label = optional.Label;
            }
        }
    }
}

