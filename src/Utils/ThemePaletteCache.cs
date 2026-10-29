using System;
using System.Collections.Concurrent;

namespace DynamicThemeColor.Utils {
    public class ThemePaletteCache {
        private readonly ConcurrentDictionary<Guid, ThemePalette> cache =
            new ConcurrentDictionary<Guid, ThemePalette>();

        public bool TryGet(Guid gameId, out ThemePalette palette) {
            if (cache.TryGetValue(gameId, out ThemePalette cached)) {
                palette = cached;
                return true;
            }

            palette = null;
            return false;
        }

        public void Set(Guid gameId, ThemePalette palette) {
            // TODO: Settings hook to disable
            cache[gameId] = palette;
        }

        public ThemePalette Remove(Guid gameId) {
            cache.TryRemove(gameId, out ThemePalette palette);
            return palette;
        }

        public void Clear() {
            cache.Clear();
        }

        public int Count => cache.Count;
    }
}
