using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor {
    // TODO: per game overrides for at least seed from settings/other gui
    public class ThemingEngine {
        private readonly IPlayniteAPI playniteApi;
        private readonly Func<AddonSettings> addonSettings;
        private readonly SchemeEngine schemeEngine;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly Dictionary<string, object> originalResources =  new Dictionary<string, object>();
        public readonly ThemePaletteCache PaletteCache;

        public ThemingEngine(IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings, SchemeEngine schemeEngine) {
            this.playniteApi = playniteApi;
            this.addonSettings = addonSettings;
            this.schemeEngine = schemeEngine;
            PaletteCache = new ThemePaletteCache(addonSettings);
        }

        public void ApplyPalette(Game game) {
            AddonSettings settings = addonSettings();
            DesktopView activeView = playniteApi.MainView.ActiveDesktopView;
            bool isFullscreen = playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;

            if (ApplyGameFilters(game) || isFullscreen && !addonSettings().ApplyInFullscreen || game == null) {
                ApplyFallbackPalette();
                return;
            }

            if (!isFullscreen) {
                if ((activeView == DesktopView.List && !settings.ApplyInListView) ||
                    (activeView == DesktopView.Details && !settings.ApplyInDetailsView) ||
                    (activeView == DesktopView.Grid && !settings.ApplyInGridView)) {
                    ApplyFallbackPalette();
                    return;
                }
            }

            if (PaletteCache.TryGet(game.Id, out ThemePalette cached)) {
                logger.Trace($"Cache hit for game '{game.Name}' ({game.Id}). Applying cached palette.");
                ApplyResolvedTheme(cached,  game);
                return;
            }

            string imgPath = ResolveAssetPath(game, addonSettings(), playniteApi);
            if (string.IsNullOrEmpty(imgPath) || !File.Exists(imgPath)) {
                ApplyFallbackPalette();
                return;
            }

            ColorExtractionResults colorExtractionResults =
                MaterialColorHelper.ExtractSeedColor(imgPath, settings.ColorSelectionRange, game.Id.GetHashCode());
            if (colorExtractionResults.SeedColor == 0) {
                ApplyFallbackPalette();
                return;
            }

            uint filteredSeed = ApplySeedFilters(colorExtractionResults, game);

            CorePalette palette = MaterialColorHelper.GetPalette(filteredSeed, settings.PaletteStyle);
            ThemePalette themePalette = new ThemePalette(palette, filteredSeed);
            ApplyPaletteFilters(themePalette, colorExtractionResults, game);

            PaletteCache.Set(game.Id, themePalette);
            ApplyResolvedTheme(themePalette, game, colorExtractionResults);
        }

        public void ApplyFallbackPalette() {
            AddonSettings settings = addonSettings();
            if (settings.UseFallbackOnMissing) {
                uint fallbackSeed = ConvertHexToArgb(settings.FallbackSeedColor);

                CorePalette palette = MaterialColorHelper.GetPalette(fallbackSeed, settings.PaletteStyle);
                ApplyResolvedTheme(new ThemePalette(palette, fallbackSeed));
                return;
            }

            UnapplyPalette();
        }

        public void UnapplyPalette() {
            foreach (KeyValuePair<string, object> keyValuePair in originalResources) {
                Application.Current.Resources[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        private void ApplyResolvedTheme(ThemePalette palette,
            [CanBeNull] Game game = null,
            ColorExtractionResults colorExtractionResults = null) {
            Dictionary<string, uint> resolvedVariables = ResolveVariables(palette);

            playniteApi.MainView.UIDispatcher.Invoke(() => {
                foreach (KeyValuePair<string, ColorScheme.IBrushDefinition> kvp in schemeEngine.ActiveBrushes) {
                    string brushKey = kvp.Key;
                    ColorScheme.IColorReference brushDef = kvp.Value;

                    HashSet<string> recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    uint finalArgb = EvaluateColorReference(brushDef, palette, resolvedVariables, recursionStack);
                    finalArgb = ApplyBrushFilters(brushKey, finalArgb, colorExtractionResults, game);

                    byte alpha = (byte)((brushDef.Alpha * 255) / 100);
                    SolidColorBrush brush = MaterialColorHelper.CreateFrozenBrush(finalArgb, alpha);

                    if (!originalResources.ContainsKey(brushKey) && Application.Current.Resources.Contains(brushKey)) {
                        originalResources[brushKey] = Application.Current.Resources[brushKey];
                    }

                    Application.Current.Resources[brushKey] = brush;
                }
            });
        }

        private Dictionary<string, uint> ResolveVariables(ThemePalette palette) {
            Dictionary<string, uint> resolved = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, ColorScheme.IColorReference> kvp in schemeEngine.ActiveVariables) {
                ResolveSingleVariable(kvp.Key, palette, resolved, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            return resolved;
        }

        private uint ResolveSingleVariable(string varName, ThemePalette palette, Dictionary<string, uint> resolved, HashSet<string> recursionStack) {
            if (resolved.TryGetValue(varName, out uint cachedValue)) {
                return cachedValue;
            }

            AddonSettings settings = addonSettings();
            if (recursionStack.Contains(varName)) {
                logger.Warn($"Circular variable reference detected involving variable '{varName}'");
                return ConvertHexToArgb(settings.FallbackSeedColor);
            }

            if (!schemeEngine.ActiveVariables.TryGetValue(varName, out ColorScheme.IColorReference colorRef)) {
                logger.Warn($"Variable '{varName}' was referenced but could not be found in active variables.");
                return ConvertHexToArgb(settings.FallbackSeedColor);
            }

            recursionStack.Add(varName);

            uint evaluatedValue = EvaluateColorReference(colorRef, palette, resolved, recursionStack);
            recursionStack.Remove(varName);
            resolved[varName] = evaluatedValue;
            return evaluatedValue;
        }

        private uint EvaluateColorReference(ColorScheme.IColorReference colorRef, ThemePalette palette, Dictionary<string, uint> resolved, HashSet<string> recursionStack) {
            AddonSettings settings = addonSettings();
            switch (colorRef) {
                case ColorScheme.HexColor hex:
                    return ConvertHexToArgb(hex.Value);
                case ColorScheme.TransparentColor _:
                    return 0x00000000;
                case ColorScheme.MaterialColor mat:
                    return GetMaterialColorValue(mat, palette);
                case ColorScheme.VariableReference varRef:
                    return ResolveSingleVariable(varRef.Variable, palette, resolved, recursionStack);
                default: {
                    logger.Error($"Unknown material color reference {colorRef}");
                    return ConvertHexToArgb(settings.FallbackSeedColor);
                }
            }
        }

        private static uint ConvertHexToArgb(string hex) {
            return MaterialColorHelper.ConvertHexToArgb(hex) ?? 0xFFFFFFFF;
        }

        private static uint GetMaterialColorValue(ColorScheme.MaterialColor mat, ThemePalette palette) {
            return palette.GetColor(mat.Palette, (uint)mat.Value);
        }

        private static string ResolveAssetPath(Game game, AddonSettings settings, IPlayniteAPI playniteApi) {
            foreach (GameAssetType assetType in settings.AssetPriority) {
                string relPath;
                switch(assetType) {
                    case GameAssetType.Background: relPath = game.BackgroundImage; break;
                    case GameAssetType.Cover: relPath = game.CoverImage; break;
                    case GameAssetType.Icon: relPath = game.Icon; break;
                    default: relPath = null; break;
                }

                if (string.IsNullOrEmpty(relPath) ||
                    relPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    relPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string fullPath = playniteApi.Database.GetFullFilePath(relPath);
                if (File.Exists(fullPath)) return fullPath;
            }
            return null;
        }

        private bool ApplyGameFilters(Game game) {
            AddonSettings settings = addonSettings();
            return settings.GameFilters
                .Where(f => f.IsEnabled)
                .Select(FilterRegistry.CreateInstance)
                .Where(f => f != null)
                .Aggregate(false, (keepGame, filter) => {
                    bool? result = filter.ApplyGameFilter(game);
                    return result ?? keepGame;
                });
        }

        private uint ApplySeedFilters(ColorExtractionResults colorExtractionResults, Game game) {
            uint seedArgb = colorExtractionResults.SeedColor;
            AddonSettings settings = addonSettings();
            return settings.SeedFilters
                .Where(f => f.IsEnabled)
                .Select(FilterRegistry.CreateInstance)
                .Where(f => f != null)
                .Aggregate(seedArgb, (current, filter) =>
                    filter.ApplySeedFilter(current, colorExtractionResults, game, playniteApi, addonSettings));
        }

        private void ApplyPaletteFilters(ThemePalette palette, ColorExtractionResults colorExtractionResults, Game game) {
            AddonSettings settings = addonSettings();
            foreach (FilterConfiguration config in settings.PaletteFilters.Where(f => f.IsEnabled)) {
                SchemeFilter filter = FilterRegistry.CreateInstance(config);
                filter?.ApplyPaletteFilter(palette, colorExtractionResults, game, playniteApi, addonSettings);
            }
        }

        private uint ApplyBrushFilters(string brushKey, uint brushArgb, [CanBeNull] ColorExtractionResults colorExtractionResults, [CanBeNull] Game game) {
            AddonSettings settings = addonSettings();
            return settings.BrushFilters
                .Where(f => f.IsEnabled)
                .Select(FilterRegistry.CreateInstance)
                .Where(f => f != null)
                .Aggregate(brushArgb, (current, filter) =>
                    filter.ApplyBrushFilter(brushKey, current, colorExtractionResults, game, playniteApi, addonSettings));
        }
    }
}
