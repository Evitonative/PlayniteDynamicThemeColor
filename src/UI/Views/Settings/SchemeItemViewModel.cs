using System;
using System.Collections.Generic;
using DynamicThemeColor.Model;

namespace DynamicThemeColor.UI.Views.Settings {
    public class SchemeItemViewModel {
        public ColorScheme Scheme { get; }
        public Dictionary<string, bool> OptionsDictionary { get; }

        public SchemeItemViewModel(ColorScheme scheme, Func<AddonSettings> addonSettings) {
            AddonSettings settings = addonSettings();
            Scheme = scheme;

            settings.SchemeOptionOverrides = settings.SchemeOptionOverrides ?? new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);

            if (!settings.SchemeOptionOverrides.TryGetValue(scheme.Theme, out Dictionary<string, bool> dict)) {
                dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                settings.SchemeOptionOverrides[scheme.Theme] = dict;
            }
            OptionsDictionary = dict;
        }
    }
}
