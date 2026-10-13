using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DynamicThemeColor.Filters;
using DynamicThemeColor.Settings;
using DynamicThemeColor.Theming;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor {
    public class ThemeController {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly IPlayniteAPI playniteApi;
        private readonly PaletteCache paletteCache;
        private readonly ThemeRegistry themeRegistry;
        private readonly Func<AddonSettings> settingsProvider;

        private CancellationTokenSource cts;

        public ThemeController(
            IPlayniteAPI playniteApi,
            PaletteCache paletteCache,
            ThemeRegistry themeRegistry,
            Func<AddonSettings> settingsProvider) {
            this.playniteApi = playniteApi;
            this.paletteCache = paletteCache;
            this.themeRegistry = themeRegistry;
            this.settingsProvider = settingsProvider;
        }

        public void UpdateTheme(Game game) {
            AddonSettings settings = settingsProvider();
            if (settings == null) return;

            if (ShouldApplyForCurrentView(game, settings)) {
                ApplyFallbackPalette(settings);
                return;
            }

            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            if (paletteCache.GetPalette(game.Id, out ColorPalette cachedPlatte)) {
                ApplyPalette(cachedPlatte, settings);
                return;
            }

            if (!TryGetImagePath(game, settings.PreferCoverArt, out string filePath)) {
                ApplyFallbackPalette(settings);
                return;
            }

            Task.Run(() => {
                try {
                    if (token.IsCancellationRequested) return;

                    uint seedColor = ColorUtilities.GetColorFromHex(settings.DefaultSeedColorHex) ?? 0;
                    uint seedArgb = SeedColorEngine
                        .From(
                            filePath,
                            seedColor,
                            game.Id.GetHashCode(),
                            colorsToPickFrom: settings.ColorSelectionRange
                        )
                        .AddFilter(new SaturationFilter(settings))
                        .AddFilter(new LightnessFilter(settings))
                        .Build();

                    if (token.IsCancellationRequested) return;

                    ColorPalette finalPalette = CreateColorPalette(seedArgb, settings.PaletteStyle);
                    paletteCache.AddPalette(game.Id, finalPalette);

                    playniteApi.MainView.UIDispatcher.Invoke(() => {
                        if (token.IsCancellationRequested) return;
                        ApplyPalette(finalPalette, settings);
                    });
                } catch (Exception e) {
                    logger.Error(e, $"Failed to extract color for {game.Name}");
                }
            }, token);
        }

        private bool ShouldApplyForCurrentView(Game game, AddonSettings settings) {
            if (game == null) return false;

            DesktopView activeView = playniteApi.MainView.ActiveDesktopView;
            return activeView == DesktopView.List && !settings.ApplyInListView ||
                   activeView == DesktopView.Details && !settings.ApplyInDetailsView ||
                   activeView == DesktopView.Grid && !settings.ApplyInGridView;
        }

        private bool TryGetImagePath(Game game, bool preferCover, out string fullPath) {
            fullPath = null;

            string imageRelPath = preferCover
                ? !string.IsNullOrEmpty(game.CoverImage) ? game.CoverImage : game.BackgroundImage
                : !string.IsNullOrEmpty(game.BackgroundImage) ? game.BackgroundImage : game.CoverImage;

            if (string.IsNullOrEmpty(imageRelPath) ||
                imageRelPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imageRelPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            string candidatePath = this.playniteApi.Database.GetFullFilePath(imageRelPath);
            if (!File.Exists(candidatePath)) {
                return false;
            }

            fullPath = candidatePath;
            return true;
        }

        private void ApplyFallbackPalette(AddonSettings settings) {
            string hexString = settings.DefaultSeedColorHex;
            if (string.IsNullOrWhiteSpace(hexString)) hexString = "#1cd8fc";

            uint? seedArgb = ColorUtilities.GetColorFromHex(hexString);
            if (seedArgb != null) {
                ColorPalette palette = CreateColorPalette(seedArgb.Value, settings.PaletteStyle);
                playniteApi.MainView.UIDispatcher.Invoke(() => {
                    ApplyPalette(palette, settings);
                });
            } else {
                logger.Warn($"Invalid fallback color format: {hexString}");
            }
        }

        private void ApplyPalette(ColorPalette colorPalette, AddonSettings settings) {
            string desktopTheme = playniteApi.ApplicationSettings.DesktopTheme ?? string.Empty;

            PaletteEngine engine = PaletteEngine
                .From(colorPalette)
                .WithThemeRegistry(themeRegistry, desktopTheme);

            if (settings.UseSeedColorForGlyph) {
                engine.AddVariableFilter(new SeedAccentFilter());
            }

            engine.AddVariableFilter(new SaturationFilter(settings));
            engine.AddVariableFilter(new LightnessFilter(settings));

            if (settings.UseSeedColorForGlyph || settings.AlwaysEnabledContrastImprovements) {
                engine.AddVariableFilter(new AccentContrastFilter());
            }

            engine.Build().Draw();
        }

        private static ColorPalette CreateColorPalette(uint seedArgb, Style style) {
            return new ColorPalette(CorePalette.Of(seedArgb, style), seedArgb);
        }
    }
}
