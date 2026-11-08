using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DynamicThemeColor.Model;

namespace DynamicThemeColor.Utils {
    public class ThemePaletteCache {
        private readonly Func<AddonSettings> addonSettings;
        private readonly ConcurrentDictionary<Guid, ThemePalette> cache =
            new ConcurrentDictionary<Guid, ThemePalette>();

        public ThemePaletteCache(Func<AddonSettings> addonSettings) {
            this.addonSettings = addonSettings;
        }

        public bool TryGet(Guid gameId, out ThemePalette palette) {
            if (addonSettings().CacheDisabled || !cache.TryGetValue(gameId, out ThemePalette cached)) {
                palette = null;
                return false;
            }

            palette = cached;
            return true;
        }

        public void Set(Guid gameId, ThemePalette palette) {
            if (addonSettings().CacheDisabled) return;
            cache[gameId] = palette;
        }

        public ThemePalette Remove(Guid gameId) {
            cache.TryRemove(gameId, out ThemePalette palette);
            return palette;
        }

        public void Clear() {
            cache.Clear();
        }

        public ICollection<Guid> GetCachedGameIds() {
            return cache.Keys;
        }

        public int Count => cache.Count;
    }
}
