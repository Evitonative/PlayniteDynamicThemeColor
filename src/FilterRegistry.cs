using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace DynamicThemeColor {
    public static class FilterRegistry {
        private static readonly Dictionary<Guid, Type> registeredFilters = new Dictionary<Guid, Type>();
        private static readonly Dictionary<Guid, FilterDescriptor> availableFilters = new Dictionary<Guid, FilterDescriptor>();

        static FilterRegistry() {
            IEnumerable<Type> filterTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract
                            && typeof(SchemeFilter).IsAssignableFrom(t)
                            && t.Namespace != null
                            && t.Namespace.StartsWith("DynamicThemeColor.Filters"));

            foreach (Type type in filterTypes) {
                if (!(Activator.CreateInstance(type) is SchemeFilter instance)) continue;
                registeredFilters[instance.Id] = type;
                availableFilters.Add(instance.Id, new FilterDescriptor(instance.Id, instance.Name, type));
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

        public static IReadOnlyDictionary<Guid, FilterDescriptor> GetAvailableFilters() => availableFilters;

        [CanBeNull]
        public static FilterDescriptor GetFilter(Guid id) {
            return availableFilters.TryGetValue(id, out FilterDescriptor descriptor) ? descriptor : null;
        }
    }

    public class FilterDescriptor {
        public Guid Id { get; }
        public string Name { get; }
        public Type FilterType { get; }

        public FilterDescriptor(Guid id, string name, Type filterType) {
            Id = id;
            Name = name;
            FilterType = filterType;
        }
    }
}
