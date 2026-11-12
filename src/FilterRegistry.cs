using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicThemeColor.Model.Filters;
using JetBrains.Annotations;

namespace DynamicThemeColor {
    public static class FilterRegistry {
        private static readonly Dictionary<Guid, FilterDescriptorNew> filters =
            new Dictionary<Guid, FilterDescriptorNew>();

        private static readonly Dictionary<Guid, SchemeFilter> filterInstances =
            new Dictionary<Guid, SchemeFilter>();

        static FilterRegistry() {
            ReloadFilters();
        }

        internal static void ReloadFilters() {
            filters.Clear();
            IEnumerable<Type> filterTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract
                            && typeof(SchemeFilter).IsAssignableFrom(t)
                            && t.Namespace != null
                            && t.Namespace.StartsWith("DynamicThemeColor.Filters"));

            foreach (Type type in filterTypes) {
                SchemeFilterAttribute attr = type.GetCustomAttribute<SchemeFilterAttribute>();
                if (attr == null) continue;
                FilterDescriptorNew descriptor = new FilterDescriptorNew(type, attr);
                filters.Add(attr.FilterId, descriptor);
            }
        }

        /// <summary>
        /// Load a filter instance into memory.
        /// </summary>
        /// <param name="config">Configuration of describing an existing instance of a filter.</param>
        /// <returns>
        /// false: this instance was already loaded
        /// true: instance successfully loaded
        /// </returns>
        /// <exception cref="KeyNotFoundException">When the filter does not exist.</exception>
        /// <exception cref="InvalidOperationException">The provided filter type does not extend SchemeFilter</exception>
        public static bool TryCreateFilterInstance(FilterConfiguration config) {
            if (!filters.TryGetValue(config.FilterId, out FilterDescriptorNew descriptor)) throw new KeyNotFoundException();
            if (filterInstances.ContainsKey(config.InstanceId)) return false;
            if (!(Activator.CreateInstance(descriptor.FilterType) is SchemeFilter instance)) throw new InvalidOperationException();

            instance.FilterConfiguration = config;

            filterInstances.Add(config.InstanceId, instance);
            return true;
        }

        [CanBeNull]
        public static FilterConfiguration CreateNewFilterInstance(Guid filterId) {
            Guid instanceId = Guid.NewGuid();
            FilterConfiguration config = new FilterConfiguration {
                InstanceId = instanceId,
                FilterId = filterId,
                IsEnabled = true,
                Settings = null,
            };

            try {
                if (!TryCreateFilterInstance(config)) return null;
            } catch (KeyNotFoundException) {
                return null;
            } catch (InvalidOperationException) {
                return null;
            }

            if (TryGetFilterInstance(instanceId, out SchemeFilter instance)) {
                config.Settings = instance.FilterConfiguration?.Settings;
            }

            return config;
        }

        public static bool TryGetFilterInstance(Guid instanceId, out SchemeFilter instance) {
            return filterInstances.TryGetValue(instanceId, out instance);
        }

        internal static bool RemoveFilterInstance(Guid instanceId) {
            return filterInstances.Remove(instanceId);
        }

        public static IEnumerable<FilterDescriptorNew> GetAllFilters() {
            return filters.Values;
        }

        internal static void ClearFilterInstances() {
            filterInstances.Clear();
        }

        public static void ClearFilterCaches() {
            foreach (SchemeFilter filter in filterInstances.Values) {
                filter?.ClearCache();
            }
        }
    }

    public readonly struct FilterDescriptorNew {
        public Type FilterType { get; }
        public SchemeFilterAttribute Attribute { get; }

        public FilterDescriptorNew(Type filterType, SchemeFilterAttribute attribute) {
            FilterType = filterType;
            Attribute = attribute;
        }
    }
}
