using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DynamicThemeColor.Model;
using JetBrains.Annotations;
using Playnite.SDK;

namespace DynamicThemeColor.UI.Views {
    public partial class SchemeSelectorView {
        public IEnumerable<RegisteredColorScheme> Candidates { get; }
        [CanBeNull] public RegisteredColorScheme SelectedItem { get; set; }
        public bool RememberChoice { get; set; } = true;

        public string DescriptionMessage { get; }

        public SchemeSelectorView() {
            InitializeComponent();
        }

        public SchemeSelectorView(IEnumerable<RegisteredColorScheme> candidates, string theme) {
            Candidates = candidates;
            SelectedItem = Candidates?.FirstOrDefault();

            DescriptionMessage = string.Format(
                ResourceProvider.GetString("LOCDTCSchemeSelectDescription"),
                theme ?? "unknown");

            InitializeComponent();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e) {
            if (SelectedItem == null) return;
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow == null) return;
            parentWindow.DialogResult = true;
        }
    }
}

