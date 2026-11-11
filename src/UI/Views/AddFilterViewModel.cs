using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DynamicThemeColor.Model;
using Playnite.SDK;

namespace DynamicThemeColor.UI.Views {
    public class AddFilterViewModel : ObservableObject {
        public IEnumerable<FilterDescriptor> Candidates { get; }
        private readonly FilterStage stage;
        public string DescriptionMessage { get; }

        private FilterDescriptor selectedItem;
        public FilterDescriptor SelectedItem {
            get => selectedItem;
            set => SetValue(ref selectedItem, value);
        }

        private bool editOnAdd = true;
        public bool EditOnAdd {
            get => editOnAdd;
            set => SetValue(ref editOnAdd, value);
        }

        public RelayCommand<Window> SelectCommand { get; }

        public AddFilterViewModel(IEnumerable<FilterDescriptor> candidates, FilterStage stage) {
            Candidates = candidates;
            this.stage = stage;
            SelectedItem = Candidates?.FirstOrDefault();

            DescriptionMessage = string.Format(
                ResourceProvider.GetString("LOCDTCFilterSelectDescription"), stage);
            SelectCommand = new RelayCommand<Window>(ExecuteSelect, window => SelectedItem != null);
        }

        private static void ExecuteSelect(Window window) {
            if (window == null) return;
            window.DialogResult = true;
        }
    }
}
