using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace DynamicThemeColor {
    public static class FilterRegistry {
        private static readonly Dictionary<Guid, Type> registeredFilters = new Dictionary<Guid, Type>();

        static FilterRegistry() {
            IEnumerable<Type> filterTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(SchemeFilter).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (Type type in filterTypes) {
                if (Activator.CreateInstance(type) is SchemeFilter instance) {
                    registeredFilters[instance.Id] = type;
                }
            }
        }

        public static SchemeFilter CreateInstance([NotNull] FilterConfiguration config) {
            if (!registeredFilters.TryGetValue(config.FilterId, out Type filterType))
                return null;

            if (!(Activator.CreateInstance(filterType) is SchemeFilter filter)) return null;

            object defaultSettings = filter.SettingsObject;

            if (defaultSettings == null) {
                config.Settings = null;
                return filter;
            }

            switch (config.Settings) {
                case JObject jObj:
                    filter.SettingsObject = jObj.ToObject(defaultSettings.GetType());
                    config.Settings = filter.SettingsObject;
                    break;
                case null:
                    config.Settings = defaultSettings;
                    break;
                default:
                    filter.SettingsObject = config.Settings;
                    break;
            }

            return filter;
        }

        public static IEnumerable<Guid> GetRegisteredFilterIds() => registeredFilters.Keys;
    }
}
