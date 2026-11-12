using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicThemeColor.Core;
using DynamicThemeColor.Model;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels.SettingsTabs {
    public class SchemeSettingsViewModel : ISettings{
        public ObservableCollection<SchemeItemViewModel> ActiveSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public ObservableCollection<SchemeItemViewModel> OtherSchemes { get; } = new ObservableCollection<SchemeItemViewModel>();
        public bool HasOtherSchemes => OtherSchemes.Count > 0;

        private AddonSettings Settings => addon.AddonSettings;

        private readonly Addon addon;
        private readonly ILogger logger = LogManager.GetLogger();

        public SchemeSettingsViewModel(Addon addon) {
            this.addon = addon;

            LoadSchemeCollections();
        }

        public void BeginEdit() {
        }

        public void EndEdit() {
        }

        public void CancelEdit() {
        }

        public bool VerifySettings(out List<string> errors) {
            errors = new List<string>();
            return true;
        }

        public void LoadSchemeCollections() {
            logger.Trace("Reloading active and other scheme collections for UI view.");
            ActiveSchemes.Clear();
            OtherSchemes.Clear();

            SchemeEngine engine = addon.SchemeEngine;
            if (engine?.RegisteredSchemes == null ) return;

            if (engine.ActiveSchemeChain != null)
                foreach (Guid schemeId in engine.ActiveSchemeChain)
                    if (engine.RegisteredSchemes.TryGetValue(schemeId, out RegisteredColorScheme scheme))
                        ActiveSchemes.Add(new SchemeItemViewModel(scheme, () => Settings));

            foreach (SchemeItemViewModel itemVm
                     in from kvp in engine.RegisteredSchemes
                     let schemeName = kvp.Key
                     let scheme = kvp.Value
                     let itemVm = new SchemeItemViewModel(scheme, () => Settings)
                     where engine.ActiveSchemeChain == null || !engine.ActiveSchemeChain.Contains(schemeName) select itemVm)
                OtherSchemes.Add(itemVm);
        }
    }

    public class SchemeItemViewModel {
        public RegisteredColorScheme Scheme { get; }
        public Dictionary<string, bool> OptionsDictionary { get; }

        public SchemeItemViewModel(RegisteredColorScheme scheme, Func<AddonSettings> addonSettings) {
            AddonSettings settings = addonSettings();
            Scheme = scheme;

            settings.SchemeOptionOverrides = settings.SchemeOptionOverrides ?? new Dictionary<Guid, Dictionary<string, bool>>();

            if (!settings.SchemeOptionOverrides.TryGetValue(scheme.Scheme.Id, out Dictionary<string, bool> dict)) {
                dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                settings.SchemeOptionOverrides[scheme.Scheme.Id] = dict;
            }
            OptionsDictionary = dict;
        }
    }
}
