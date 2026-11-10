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
            RemoveCommand = new RelayCommand(RemoveItem, CanRemoveItem);
            EditCommand = new RelayCommand(EditItem, CanEditItem);
            MoveUpCommand = new RelayCommand(MoveItemUp, CanMoveItemUp);
            MoveDownCommand = new RelayCommand(MoveItemDown, CanMoveItemDown);

            InitializeComponent();
        }

        private bool CanRemoveItem() =>
            ListBody?.SelectedItem != null &&
            ListBody.ItemsSource != null &&
            ListBody.ItemsSource.Contains(ListBody.SelectedItem);

        private void RemoveItem() {
            if (!(ListBody?.ItemsSource is IList list)) return;

            int idx = ListBody.ItemsSource.IndexOf(ListBody.SelectedItem);
            if (idx < 0) return;

            list.RemoveAt(idx);
            if (list.Count > 0) {
                int newIdx = idx >= list.Count ? list.Count - 1 : idx;
                ListBody.SelectedItem = list[newIdx];
            } else ListBody.SelectedItem = null;
        }

        private bool CanEditItem() => ListBody?.SelectedItem is IEditableItem;

        private void EditItem() {
            if (!(ListBody?.SelectedItem is IEditableItem editable)) return;
            editable.EditFilter();
        }

        private bool CanMoveItemUp() =>
            ListBody?.SelectedItem != null &&
            ListBody.ItemsSource != null &&
            ListBody.ItemsSource.IndexOf(ListBody.SelectedItem) > 0;

        private void MoveItemUp() {
            if (ListBody == null) return;
            object item = ListBody.SelectedItem;
            int index = ListBody.ItemsSource.IndexOf(item);
            if (index == 0 || !(ListBody.ItemsSource is IList list)) return;
            list.RemoveAt(index);
            list.Insert(index - 1, item);
            ListBody.SelectedItem = item;
        }

        private bool CanMoveItemDown() =>
            ListBody?.SelectedItem != null &&
            ListBody.ItemsSource != null &&
            ListBody.ItemsSource.IndexOf(ListBody.SelectedItem) < ListBody.ItemsSource.Count - 1;

        private void MoveItemDown() {
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

