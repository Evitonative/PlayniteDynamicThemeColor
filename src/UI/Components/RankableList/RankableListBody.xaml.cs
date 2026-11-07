using System.Collections;
using System.Windows;

namespace DynamicThemeColor.UI.Components.RankableList {
    public partial class RankableListBody {
        public static readonly DependencyProperty ItemSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IList), typeof(RankableListBody), new PropertyMetadata(null));

        public IList ItemsSource {
            get { return (IList)GetValue(ItemSourceProperty); }
            set { SetValue(ItemSourceProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(RankableListBody), new PropertyMetadata(null));

        public object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public RankableListBody() {
            InitializeComponent();
        }
    }
}

