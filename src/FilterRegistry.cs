using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace DynamicThemeColor {
    public static class FilterRegistry {
        private static readonly Dictionary<Guid, FilterDescriptor> filters =
            new Dictionary<Guid, FilterDescriptor>();

        static FilterRegistry() {
            IEnumerable<Type> filterTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract
                            && typeof(SchemeFilter).IsAssignableFrom(t)
                            && t.Namespace != null
                            && t.Namespace.StartsWith("DynamicThemeColor.Filters"));

            foreach (Type type in filterTypes) {
                if (!(Activator.CreateInstance(type) is SchemeFilter instance)) continue;
                filters.Add(instance.Id, new FilterDescriptor(instance.Id, instance.Name, type));
            }
        }

        [CanBeNull]
        private static SchemeFilter CreateSimpleInstance(Guid filterId) {
            if (!filters.TryGetValue(filterId, out FilterDescriptor descriptor))
                return null;

            if (!(Activator.CreateInstance(descriptor.FilterType) is SchemeFilter filter)) return null;
            return filter;
        }

        public static SchemeFilter CreateInstance([NotNull] FilterConfiguration config) {
            SchemeFilter filter = CreateSimpleInstance(config.FilterId);
            if (filter == null) return null;

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

        public static IReadOnlyDictionary<Guid, FilterDescriptor> GetAvailableFilters() => filters;

        [CanBeNull]
        public static FilterDescriptor GetFilter(Guid id) {
            return filters.TryGetValue(id, out FilterDescriptor descriptor) ? descriptor : null;
        }

        public static void ClearFilterCaches() {
            foreach (SchemeFilter filter in filters.Keys.Select(CreateSimpleInstance)) {
                filter?.ClearCache();
            }
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
