using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DynamicThemeColor.UI.Components.RankableList {
    public class RankableComponentBase : UserControl, INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register(
            nameof(AddCommand), typeof(ICommand), typeof(RankableComponentBase),
            new PropertyMetadata(null, OnDependencyChanged));

        public ICommand AddCommand {
            get => (ICommand)GetValue(AddCommandProperty);
            set => SetValue(AddCommandProperty, value);
        }

        public static readonly DependencyProperty AddingEnabledOverrideProperty = DependencyProperty.Register(
            nameof(AddingEnabledOverride), typeof(bool?), typeof(RankableComponentBase),
            new PropertyMetadata(null, OnDependencyChanged));

        public bool? AddingEnabledOverride {
            get => (bool?)GetValue(AddingEnabledOverrideProperty);
            set => SetValue(AddingEnabledOverrideProperty, value);
        }

        public bool AddingEnabled => AddingEnabledOverride ?? AddCommand != null;

        public static readonly DependencyProperty RemovingEnabledProperty = DependencyProperty.Register(
            nameof(RemovingEnabled), typeof(bool), typeof(RankableComponentBase), new PropertyMetadata(false));

        public bool RemovingEnabled {
            get => (bool)GetValue(RemovingEnabledProperty);
            set => SetValue(RemovingEnabledProperty, value);
        }

        public static readonly DependencyProperty EditingEnabledProperty = DependencyProperty.Register(
            nameof(EditingEnabled), typeof(bool), typeof(RankableComponentBase), new PropertyMetadata(false));

        public bool EditingEnabled {
            get => (bool)GetValue(EditingEnabledProperty);
            set => SetValue(EditingEnabledProperty, value);
        }

        public static readonly DependencyProperty ReorderingEnabledProperty = DependencyProperty.Register(
            nameof(ReorderingEnabled), typeof(bool), typeof(RankableComponentBase), new PropertyMetadata(true));

        public bool ReorderingEnabled {
            get => (bool)GetValue(ReorderingEnabledProperty);
            set => SetValue(ReorderingEnabledProperty, value);
        }

        private static void OnDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is RankableComponentBase control)) return;
            control.PropertyChanged?.Invoke(control, new PropertyChangedEventArgs(nameof(AddingEnabled)));
        }
    }
}
