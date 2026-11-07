using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DynamicThemeColor.Model;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels {
    public class SchemeSelectorViewModel : ObservableObject {
        public IEnumerable<RegisteredColorScheme> Candidates { get; }
        public string DescriptionMessage { get; }

        private RegisteredColorScheme selectedItem;
        public RegisteredColorScheme SelectedItem {
            get => selectedItem;
            set => SetValue(ref selectedItem, value);
        }

        private bool rememberChoice = true;
        public bool RememberChoice {
            get => rememberChoice;
            set => SetValue(ref rememberChoice, value);
        }

        public RelayCommand<Window> SelectCommand { get; }

        public SchemeSelectorViewModel(IEnumerable<RegisteredColorScheme> candidates, string theme) {
            Candidates = candidates;
            SelectedItem = Candidates?.FirstOrDefault();

            DescriptionMessage = string.Format(
                ResourceProvider.GetString("LOCDTCSchemeSelectDescription"),
                theme ?? "unknown");

            SelectCommand = new RelayCommand<Window>(ExecuteSelect, window => SelectedItem != null);
        }

        private void ExecuteSelect(Window window) {
            if (window == null) return;
            window.DialogResult = true;
        }
    }
}
