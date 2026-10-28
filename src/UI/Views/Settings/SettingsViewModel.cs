using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicThemeColor.Model;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace DynamicThemeColor.UI.Views.Settings {
    public class SettingsViewModel : ObservableObject, ISettings {
        private readonly Addon plugin;
        private AddonSettings EditingClone { get; set; }
        private AddonSettings settings;

        public ObservableCollection<SchemeItemViewModel> ActiveSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public ObservableCollection<SchemeItemViewModel> OtherSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public bool HasOtherSchemes => OtherSchemes.Count > 0;

        public AddonSettings Settings {
            get => settings;
            [UsedImplicitly]
            set {
                settings = value;
                OnPropertyChanged();
                LoadSchemeCollections();
            }
        }

        public SettingsViewModel(Addon plugin, AddonSettings settings) {
            this.plugin = plugin;
            this.settings = settings;
            LoadSchemeCollections();
        }

        private void LoadSchemeCollections() {
            ActiveSchemes.Clear();
            OtherSchemes.Clear();

            SchemeEngine engine = plugin.SchemeEngine;
            if (engine?.Schemes == null ) return;

            foreach (KeyValuePair<string, ColorScheme> kvp in engine.Schemes) {
                string schemeName = kvp.Key;
                ColorScheme scheme = kvp.Value;

                SchemeItemViewModel itemVm = new SchemeItemViewModel(scheme, Settings);
                if (engine.ActiveSchemeChain != null && engine.ActiveSchemeChain.Contains(schemeName)) ActiveSchemes.Add(itemVm);
                else OtherSchemes.Add(itemVm);
            }
        }

        public void BeginEdit() {
            EditingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit() {
            Settings = EditingClone;
        }

        public void EndEdit() {
            plugin.SavePluginSettings(Settings);
            plugin.SchemeEngine.RefreshBrushes();
        }

        public bool VerifySettings(out List<string> errors) {
            errors = new List<string>();
            return true;
        }
    }
}
