using System;
using System.Collections.Generic;
using DynamicThemeColor.Model;

namespace DynamicThemeColor.UI.Views.Settings {
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
