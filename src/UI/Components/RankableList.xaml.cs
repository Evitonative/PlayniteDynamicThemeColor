using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Playnite.SDK;

namespace DynamicThemeColor.UI.Components {
    public partial class RankableList {
        public RankableList() {
            InitializeComponent();
            MoveUpCommand = new RelayCommand<object>(MoveUp, CanMoveUp);
            MoveDownCommand = new RelayCommand<object>(MoveDown, CanMoveDown);
        }

        public static readonly DependencyProperty ItemSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IList),
                typeof(RankableList),
                new PropertyMetadata(null));

        public IList ItemsSource {
            get { return (IList)GetValue(ItemSourceProperty); }
            set { SetValue(ItemSourceProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(RankableList),
                new PropertyMetadata(null));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        private bool CanMoveUp(object item) =>
            item != null &&
            ItemsSource != null &&
            ItemsSource.IndexOf(item) > 0;
        private void MoveUp(object item) {
            int index = ItemsSource.IndexOf(item);
            if (index == 0 || !(ItemsSource is IList list)) return;
            list.RemoveAt(index);
            list.Insert(index - 1, item);
            SelectedItem = item;
        }

        private bool CanMoveDown(object item) =>
            item != null &&
            ItemsSource != null &&
            ItemsSource.IndexOf(item) < ItemsSource.Count - 1;
        private void MoveDown(object item) {
            int index = ItemsSource.IndexOf(item);
            if (index < 0 || index >= ItemsSource.Count - 1 || !(ItemsSource is IList list)) return;
            list.RemoveAt(index);
            list.Insert(index + 1, item);
            SelectedItem = item;
        }
    }
}

