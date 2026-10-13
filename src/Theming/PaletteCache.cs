using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;

namespace DynamicThemeColor
{
    public class PaletteCache
    {
        public readonly ConcurrentDictionary<Guid, ColorPalette> Palettes =
            new ConcurrentDictionary<Guid, ColorPalette>();

        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly IPlayniteAPI playniteApi;
        private bool disableCache;

        public PaletteCache(IPlayniteAPI playniteApi, bool disabledCache = false)
        {
            this.playniteApi = playniteApi;
            disableCache = disabledCache;
        }

        public void AddPalette(Guid gameId, ColorPalette palette)
        {
            if (disableCache) return;
            Palettes[gameId] = palette;
        }

        public void RemovePalette(Guid gameId)
        {
            Palettes.TryRemove(gameId, out _);
        }

        public bool GetPalette(Guid gameId, out ColorPalette colorPalette)
        {
            return Palettes.TryGetValue(gameId, out colorPalette);
        }

        public int GetCacheCount()
        {
            return Palettes.Count;
        }

        public bool IsCacheEnabled()
        {
            return !disableCache;
        }

        public void ToggleCache()
        {
            if (!Debugger.IsAttached)
            {
                logger.Warn("This is only available in debug mode.");
            }

            disableCache = !disableCache;
        }

        public void Clear(bool notify = false)
        {
            Palettes.Clear();

            if (notify)
            {
                playniteApi.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCDynamicThemeColorCacheCleared"),
                    "Dynamic Theme Color"
                );
            }
        }
    }

    public readonly struct ColorPalette
    {
        public CorePalette Palette { get; }
        public uint SeedColor { get; }

        public ColorPalette(CorePalette palette, uint seedColor)
        {
            Palette = palette;
            SeedColor = seedColor;
        }
    }
}
