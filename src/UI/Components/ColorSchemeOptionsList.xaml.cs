using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DynamicThemeColor.Model;

namespace DynamicThemeColor.UI.Components {
    public partial class ColorSchemeOptionsList : UserControl {
        public ColorSchemeOptionsList() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ColorSchemeProperty = DependencyProperty.Register(
            nameof(ColorScheme),
            typeof(ColorScheme),
            typeof(ColorSchemeOptionsList),
            new PropertyMetadata(null, OnParametersChanged));

        public ColorScheme ColorScheme {
            get { return (ColorScheme)GetValue(ColorSchemeProperty); }
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

        public ObservableCollection<OptionItem> Options { get; } = new ObservableCollection<OptionItem>();

        private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ColorSchemeOptionsList control) control.RebuildOptions();
        }

        private void RebuildOptions() {
            Options.Clear();

            if (ColorScheme == null || SchemeOptions == null) return;
            if (ColorScheme.Brushes == null) return;

            foreach (KeyValuePair<string, ColorScheme.IBrushDefinition> kvp
                     in ColorScheme.Brushes.Where(kvp => kvp.Value.Optional != null)) {
                Options.Add(new OptionItem(kvp.Key, kvp.Value.Optional, SchemeOptions));
            }

            FallbackText.Visibility = Options.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }


        public class OptionItem : ObservableObject {
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

            public OptionItem(string brushKey, ColorScheme.OptionalConfig optional, Dictionary<string, bool> schemeOptions)
            {
                this.brushKey = brushKey;
                this.schemeOptions = schemeOptions;
                defaultValue = optional.Default;
                Label = optional.Label;
            }
        }
    }
}

