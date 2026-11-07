using System.Collections;
using System.Windows;
using System.Windows.Input;
using DynamicThemeColor.Model;
using Playnite.SDK;

namespace DynamicThemeColor.UI.Components.RankableList {
    public partial class RankableToolbarHeader {
        public static readonly DependencyProperty ListBodyProperty = DependencyProperty.Register(
            nameof(ListBody), typeof(RankableListBody), typeof(RankableToolbarHeader), new PropertyMetadata(default(RankableListBody)));

        public RankableListBody ListBody {
            get { return (RankableListBody)GetValue(ListBodyProperty); }
            set { SetValue(ListBodyProperty, value); }
        }

        public ICommand RemoveCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }


        public RankableToolbarHeader() {
            RemoveCommand = new RelayCommand<object>(RemoveItem, CanRemoveItem);
            EditCommand = new RelayCommand<object>(EditItem, CanEditItem);
            MoveUpCommand = new RelayCommand<object>(MoveItemUp, CanMoveItemUp);
            MoveDownCommand = new RelayCommand<object>(MoveItemDown, CanMoveItemDown);

            InitializeComponent();
        }

        private bool CanRemoveItem(object _) =>
            ListBody?.SelectedItem != null &&
            ListBody.ItemsSource != null &&
            ListBody.ItemsSource.Contains(ListBody.SelectedItem);

        private void RemoveItem(object _) {
            if (!(ListBody?.ItemsSource is IList list)) return;
            list.Remove(ListBody.SelectedItem);
        }

        private bool CanEditItem(object _) => ListBody?.SelectedItem is IEditableItem;

        private void EditItem(object _) {
            if (!(ListBody?.SelectedItem is IEditableItem editable)) return;
            editable.Edit();
        }

        private bool CanMoveItemUp(object _) =>
            ListBody?.SelectedItem != null &&
            ListBody.ItemsSource != null &&
            ListBody.ItemsSource.IndexOf(ListBody.SelectedItem) > 0;

        private void MoveItemUp(object _) {
            if (ListBody == null) return;
            object item = ListBody.SelectedItem;
            int index = ListBody.ItemsSource.IndexOf(item);
            if (index == 0 || !(ListBody.ItemsSource is IList list)) return;
            list.RemoveAt(index);
            list.Insert(index - 1, item);
            ListBody.SelectedItem = item;
        }

        private bool CanMoveItemDown(object _) =>
            ListBody?.SelectedItem != null &&
            ListBody.ItemsSource != null &&
            ListBody.ItemsSource.IndexOf(ListBody.SelectedItem) < ListBody.ItemsSource.Count - 1;

        private void MoveItemDown(object _) {
            if (ListBody == null) return;
            object item = ListBody.SelectedItem;
            int index = ListBody.ItemsSource.IndexOf(item);
            if (index < 0 || index >= ListBody.ItemsSource.Count - 1 || !(ListBody.ItemsSource is IList list)) return;
            list.RemoveAt(index);
            list.Insert(index + 1, item);
            ListBody.SelectedItem = item;
        }
    }
}

