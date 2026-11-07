using System.Collections;
using System.Windows;

namespace DynamicThemeColor.UI.Components.RankableList {
    public partial class RankableList {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IList), typeof(RankableList), new PropertyMetadata(null));

        public IList ItemsSource {
            get => (IList)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(RankableList), new PropertyMetadata(null));

        public object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public RankableList() {
            InitializeComponent();
        }
    }
}

