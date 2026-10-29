using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DynamicThemeColor.Model;
using DynamicThemeColor.Utils;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;
using Playnite.SDK.Models;
using Style = MaterialColorUtilities.Palettes.Style;

namespace DynamicThemeColor {
    // TODO: All 0xffffffff should refer to the settings for the fallback color
    // TODO: per game overrides for at least seed from settings/other gui
    public class ThemingEngine {
        private readonly IPlayniteAPI playniteApi;
        private readonly AddonSettings addonSettings;
        private readonly SchemeEngine schemeEngine;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly Dictionary<string, object> originalResources =  new Dictionary<string, object>();
        private readonly ThemePaletteCache paletteCache = new ThemePaletteCache();

        public ThemingEngine(IPlayniteAPI playniteApi, AddonSettings addonSettings, SchemeEngine schemeEngine) {
            this.playniteApi = playniteApi;
            this.addonSettings = addonSettings;
            this.schemeEngine = schemeEngine;
        }

        public void ApplyPalette(Game game) {
            DesktopView activeView = playniteApi.MainView.ActiveDesktopView;
            bool isFullscreen = playniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;

            if (isFullscreen && !addonSettings.ApplyInFullscreen || game == null) {
                ApplyFallbackPalette();
                return;
            }

            if (!isFullscreen) {
                if ((activeView == DesktopView.List && !addonSettings.ApplyInListView) ||
                    (activeView == DesktopView.Details && !addonSettings.ApplyInDetailsView) ||
                    (activeView == DesktopView.Grid && !addonSettings.ApplyInGridView)) {
                    ApplyFallbackPalette();
                    return;
                }
            }

            if (paletteCache.TryGet(game.Id, out ThemePalette cached)) {
                logger.Trace($"Cache hit for game '{game.Name}' ({game.Id}). Applying cached palette.");
                ApplyResolvedTheme(cached);
                return;
            }

            string imgPath = ResolveAssetPath(game);
            if (string.IsNullOrEmpty(imgPath) || !File.Exists(imgPath)) {
                ApplyFallbackPalette();
                return;
            }

            // TODO: Hook up max colors to settings
            uint rawSeed = MaterialColorHelper.ExtractSeedColor(imgPath, 5, game.Id.GetHashCode());
            if (rawSeed == 0) {
                ApplyFallbackPalette();
                return;
            }

            uint filteredSeed = ApplySeedFilters(rawSeed, game);

            // TODO: From addon settings
            CorePalette palette = MaterialColorHelper.GetPalette(filteredSeed, Style.Content);
            ThemePalette themePalette = new ThemePalette(palette, filteredSeed);
            ApplyPaletteFilters(themePalette, game);

            paletteCache.Set(game.Id, themePalette);
            ApplyResolvedTheme(themePalette);
        }

        // TODO: Settings toggle between reverting
        private void ApplyFallbackPalette() {
            if (true) {
                // TODO: From Settings
                const uint fallbackSeed = 0xFF1CD8FC;

                // TODO: From addon settings
                CorePalette palette = MaterialColorHelper.GetPalette(fallbackSeed, Style.Content);
                ApplyResolvedTheme(new ThemePalette(palette, fallbackSeed));
                return;
            }

            UnapplyPalette();
        }

        private void UnapplyPalette() {
            foreach (KeyValuePair<string, object> keyValuePair in originalResources) {
                Application.Current.Resources[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        private void ApplyResolvedTheme(ThemePalette palette) {
            Dictionary<string, uint> resolvedVariables = ResolveVariables(palette);

            playniteApi.MainView.UIDispatcher.Invoke(() => {
                foreach (KeyValuePair<string, ColorScheme.IBrushDefinition> kvp in schemeEngine.ActiveBrushes) {
                    string brushKey = kvp.Key;
                    ColorScheme.IColorReference brushDef = kvp.Value;

                    HashSet<string> recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    uint finalArgb = EvaluateColorReference(brushDef, palette, resolvedVariables, recursionStack);
                    finalArgb = ApplyBrushFilters(brushKey, finalArgb);

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

            if (recursionStack.Contains(varName)) {
                logger.Warn($"Circular variable reference detected involving variable '{varName}'");
                return 0xFFFFFFFF;
            }

            if (!schemeEngine.ActiveVariables.TryGetValue(varName, out ColorScheme.IColorReference colorRef)) {
                logger.Warn($"Variable '{varName}' was referenced but could not be found in active variables.");
                return 0xFFFFFFFF;
            }

            recursionStack.Add(varName);

            uint evaluatedValue = EvaluateColorReference(colorRef, palette, resolved, recursionStack);
            recursionStack.Remove(varName);
            resolved[varName] = evaluatedValue;
            return evaluatedValue;
        }

        private uint EvaluateColorReference(ColorScheme.IColorReference colorRef, ThemePalette palette, Dictionary<string, uint> resolved, HashSet<string> recursionStack) {
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
                    return 0xFFFFFFFF;
                }
            }
        }

        private static uint GetMaterialColorValue(ColorScheme.MaterialColor mat, ThemePalette palette) {
            return palette.GetColor(mat.Palette, (uint)mat.Value);
        }

        private static uint ConvertHexToArgb(string hex) {
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#")) return 0xFFFFFFFF;

            if (ColorConverter.ConvertFromString(hex) is Color color) {
                return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
            }

            return 0xFFFFFFFF;
        }

        private string ResolveAssetPath(Game game) {
            foreach (GameAssetType assetType in addonSettings.AssetPriority) {
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

        protected virtual uint ApplySeedFilters(uint seedArgb, Game game) {
            // TODO: seed filters
            return seedArgb;
        }

        protected virtual void ApplyPaletteFilters(ThemePalette palette, Game game) {
            // TODO: palette filters
        }

        protected virtual uint ApplyBrushFilters(string brushKey, uint argb) {
            // TODO: brush filters
            return argb;
        }
    }
}
