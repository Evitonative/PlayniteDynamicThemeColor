using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialColorUtilities.Palettes;
using MaterialColorUtilities.Quantize;
using MaterialColorUtilities.Score;
using MaterialColorUtilities.Utils;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DynamicThemeColor
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DynamicThemeColor : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private DynamicThemeColorSettingsViewModel Settings { get; set; }
        
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly ConcurrentDictionary<Guid, (CorePalette Palette, uint SeedColor)> _paletteCache = new ConcurrentDictionary<Guid, (CorePalette, uint)>();
        
        private CancellationTokenSource _cts;

        public override Guid Id { get; } = Guid.Parse("a7a76873-8465-4273-a8a6-d460550ce627");

        public DynamicThemeColor(IPlayniteAPI api) : base(api)
        {
            Settings = new DynamicThemeColorSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new DynamicThemeColorSettingsView();
        }
        
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            var game = args.NewValue?.FirstOrDefault();
            UpdateTheme(game);
        }

        public void RefreshTheme()
        {
            var game = PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
            UpdateTheme(game);
        }

        public void ClearCache(bool silent = false)
        {
            _paletteCache.Clear();

            if (silent) return;
            PlayniteApi.Dialogs.ShowMessage(
                ResourceProvider.GetString("LOCDynamicThemeColorCacheCleared"), 
                "Dynamic Theme Color"
            );
        }

        private void UpdateTheme(Game game)
        {
            var activeView = PlayniteApi.MainView.ActiveDesktopView;
            var settings = Settings.Settings;
            
            if (activeView == DesktopView.List && !settings.ApplyInListView ||
                activeView == DesktopView.Details && !settings.ApplyInDetailsView ||
                activeView == DesktopView.Grid && !settings.ApplyInGridView ||
                game == null)
            {
                ApplyFallbackPalette();
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            if (_paletteCache.TryGetValue(game.Id, out var cachedPalette))
            {
                ApplyPalette(cachedPalette.Palette, cachedPalette.SeedColor);
                return;
            }
            
            var preferCover = Settings.Settings.PreferCoverArt;
            var imageRelPath = preferCover 
                ? !string.IsNullOrEmpty(game.CoverImage) ? game.CoverImage : game.BackgroundImage
                : !string.IsNullOrEmpty(game.BackgroundImage) ? game.BackgroundImage : game.CoverImage;
            
            if (string.IsNullOrEmpty(imageRelPath) ||
                imageRelPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imageRelPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) 
            {
                ApplyFallbackPalette();
                return;
            }

            var filePath = PlayniteApi.Database.GetFullFilePath(imageRelPath);
            if (!File.Exists(filePath))
            {
                ApplyFallbackPalette();
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    if (token.IsCancellationRequested) return;

                    var maxColors = Settings.Settings.ColorSelectionRange;
                    var seedArgb = GetColorFromImage(filePath, game.Id, maxColors);

                    if (seedArgb == 0)
                    {
                        if (!token.IsCancellationRequested) ApplyFallbackPalette();
                        return;
                    }

                    if (token.IsCancellationRequested) return;

                    var palette = GetPalette(seedArgb);
                    
#if !DEBUG
                        _paletteCache[game.Id] = (palette, seedArgb);
#endif

                    PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        ApplyPalette(palette, seedArgb);
                    });
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed to extract color for {game.Name}");
                }

            }, token);
        }

        private void ApplyFallbackPalette()
        {
            var hexString = Settings?.Settings?.DefaultSeedColorHex;
            if (string.IsNullOrWhiteSpace(hexString)) hexString = "#1cd8fc";

            if (ColorConverter.ConvertFromString(hexString) is Color color)
            {
                var seedArgb = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                var palette = GetPalette(seedArgb);
                PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                {
                    ApplyPalette(palette, seedArgb);
                });
            }
            else
            {
                Logger.Warn($"Invalid fallback color format: {hexString}");
            }
        }

        private void ApplyPalette(CorePalette palette, uint seedArgb)
        {
            // Accent Color
            var accentColor = palette.Primary[80];
            var onAccentColor = palette.Primary[10];
            
            if (Settings.Settings.UseSeedColorForGlyph || Settings.Settings.AlwaysEnabledContrastImprovements)
            {
                accentColor = seedArgb;
                
                var lightness = ColorUtils.LStarFromArgb(accentColor);
                onAccentColor = lightness <= 40 ? palette.Primary[90] : palette.Primary[10];
            }
            
            var satMult = Settings.Settings.SaturationSlider;
            var lightMult = Settings.Settings.LightnessSlider;
            accentColor = ApplyColorAdjustments(accentColor, satMult, lightMult);
            //onAccentColor = ApplyColorAdjustments(onAccentColor, satMult, lightMult); -- this didn't look good, but I'll leave it here
            
            // Surfaces & Window Backdrops
            var windowBackground = palette.Neutral[8];
            var tooltipBackground  = palette.NeutralVariant[8];
            var windowOutline =  palette.NeutralVariant[35];
            var cardBackground = palette.NeutralVariant[15];
            
            // Typography
            var primaryText = palette.Neutral[90];
            var secondaryText = palette.NeutralVariant[70];
            
            // Buttons & Inputs
            var buttonBackground = palette.Neutral[25];
            var buttonOutline = palette.Primary[70];
            var altPropertiesButton = palette.NeutralVariant[30];
            
            // Other Colors
            var scrollbarInactiveThumb = palette.Neutral[20];
            var progressbarBackground = palette.NeutralVariant[15];
            
            // Notes are for the default theme
            ApplyBrushes(
                // --- Surfaces & Window Backdrops ---
                // ReSharper disable once StringLiteralTypo - Playnite dev made a typo
                ("WindowBackgourndBrush", CreateFrozenBrush(windowBackground)),             // Primary window backgrounds
                ("WindowBackgroundBrush", CreateFrozenBrush(windowBackground)),             // Primary window backgrounds
                ("PopupBackgroundBrush", CreateFrozenBrush(tooltipBackground)),             // Popup Background
                ("TooltipBackgroundBrush", CreateFrozenBrush(tooltipBackground)),           // Tooltip Background
                ("PopupBorderBrush", CreateFrozenBrush(windowOutline)),                     // Window Outline
                ("GridItemBackgroundBrush", CreateFrozenBrush(tooltipBackground)),          // Background under letterboxed/odd-aspect covers in Grid View
                
                // --- Typography ---
                ("TextBrush", CreateFrozenBrush(primaryText)),                              // Primary text across labels, titles, and grid tiles
                ("TextBrushDarker", CreateFrozenBrush(secondaryText)),                      // The search icon, Harmony uses it in more places
                ("TextBrushDark", CreateFrozenBrush(onAccentColor)),                        // The Button on the play button
                
                // --- Interactive Accents, Glyphs & State Indicators ---
                ("GlyphBrush", CreateFrozenBrush(accentColor)),                             // Used by a lot, play button, active elements, some Glyphs
                ("HighlightGlyphBrush", CreateFrozenBrush(accentColor)),                    // Background of the progress in sliders
                ("CheckBoxCheckMarkBrush", CreateFrozenBrush(accentColor)),                 // Checkbox check glyph
                ("HoverBrush", CreateFrozenBrush(accentColor, 80)),                       // Hovering on sidebar
                
                // --- Controls, Inputs & Buttons ---
                ("ButtonBackgroundBrush", CreateFrozenBrush(buttonBackground)),             // Button Background
                ("NormalBorderBrush", CreateFrozenBrush(buttonOutline, 32)),              // Outline of buttons
                ("ControlBackgroundBrush", CreateFrozenBrush(buttonBackground, 64)),      // Sidebar, Searchbar, Search Menu, All kinds of inputs
                ("NormalBrush", CreateFrozenBrush(scrollbarInactiveThumb)),                 // Inactive Scrollbar & Filters Button
                ("NormalBrushDark", CreateFrozenBrush(progressbarBackground)),              // Progress bar gutter / recessed tracks
                
                // --- Layout Framing & Dividers ---
                ("PanelSeparatorBrush", Brushes.Transparent),                       // Framing in main window
                ("WindowPanelSeparatorBrush", Brushes.Transparent),                 // Framing in the settings windows
                ("ExpanderBackgroundBrush", Brushes.Transparent)                    // Expandable section backgrounds
            );

            // Theme Specific
            switch (PlayniteApi.ApplicationSettings.DesktopTheme ?? string.Empty)
            { 
                case var theme when IsTheme("Harmony", theme):
                    ApplyBrushes(
                        // --- Surfaces & Window Backdrops ---
                        ("MainWindowBackgroundBrush", CreateFrozenBrush(windowBackground)),                 // Main Window Background
                        
                        // --- Controls, Inputs & Buttons ---
                        ("PlayButtonTextBrush", CreateFrozenBrush(onAccentColor)),                          // Text play button
                        ("ButtonMouseOverBackgroundBrush", CreateFrozenBrush(buttonBackground, 128)),     // Hover over game quick actions
                        ("SecondaryBrush", CreateFrozenBrush(altPropertiesButton)),                         // Unused but I use it as alternative disabled Properties Button
                        ("MainPanelBackgroundBrush", CreateFrozenBrush(accentColor))                        // Used by borders from checkboxes, but I can't see it
                        
                        // This is most likely not used, I just keep it to remember it
                        // ("SidebarItemBackgroundBrush", CreateFrozenBrush(palette.Neutral[90], 13))
                    );

                    if (Settings.Settings.EnableHarmonyDetailsPanelBrush)
                        ApplyBrushes(("DetailsViewPanelBackgroundBrush", CreateFrozenBrush(cardBackground)));
                    
                    break;
            }
        }

        private uint GetColorFromImage(string imagePath, Guid gameId, int maxColors)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
            bitmap.DecodePixelWidth = 128;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            
            var convertedBitmap = new FormatConvertedBitmap();
            convertedBitmap.BeginInit();
            convertedBitmap.Source = bitmap;
            convertedBitmap.DestinationFormat = PixelFormats.Bgra32;
            convertedBitmap.EndInit();
            convertedBitmap.Freeze();

            var width = convertedBitmap.PixelWidth;
            var height = convertedBitmap.PixelHeight;
            var pixels = new uint[width * height];
            convertedBitmap.CopyPixels(pixels, width * 4, 0);

            var quantiserResult = QuantizerCelebi.Quantize(pixels, 128);
            var rankedColors = Scorer.Score(quantiserResult);
            
            if (rankedColors.Count == 0) return 0;
            
            if (maxColors <= 1) return rankedColors.FirstOrDefault();
            
            var topColors = rankedColors.Take(maxColors).ToList();
            var index = (int)((uint)gameId.GetHashCode() % (uint)topColors.Count);

            var pickedColor = topColors[index];
            
            return ApplyColorAdjustments(pickedColor, Settings.Settings.SaturationSlider, Settings.Settings.LightnessSlider);
        }

        private CorePalette GetPalette(uint seedArgb)
        {
            return CorePalette.Of(seedArgb, Settings.Settings.PaletteStyle);
        }

        private static uint ApplyColorAdjustments(uint argb, double satSlider, double lightSlider)
        {
            if (Math.Abs(satSlider) < 0.005 && Math.Abs(lightSlider) < 0.005) return argb;

            var a = (byte)(argb >> 24);
            var r = (byte)(argb >> 16);
            var g = (byte)(argb >> 8);
            var b = (byte)argb;

            var luma = 0.299 * r + 0.587 * g + 0.114 * b;
            
            var satMult = 1.0 + satSlider;

            var outR = luma + (r - luma) * satMult;
            var outG = luma + (g - luma) * satMult;
            var outB = luma + (b - luma) * satMult;

            if (lightSlider < 0)
            {
                var darkenMult = 1.0 + lightSlider;
                outR *= darkenMult;
                outG *= darkenMult;
                outB *= darkenMult;
            }
            else if (lightSlider > 0)
            {
                outR += (255 - outR) * lightSlider;
                outG += (255 - outG) * lightSlider;
                outB += (255 - outB) * lightSlider;
            }

            var finalR = (byte)Math.Max(0, Math.Min(255, Math.Round(outR)));
            var finalG = (byte)Math.Max(0, Math.Min(255, Math.Round(outG)));
            var finalB = (byte)Math.Max(0, Math.Min(255, Math.Round(outB)));

            return (uint)((a << 24) | (finalR << 16) | (finalG << 8) | finalB);
        }

        private static void ApplyBrushes(params (string resource, Brush brush)[] brushes)
        {
            foreach (var (resource, brush) in brushes)
            {
                Application.Current.Resources[resource] = brush;
            }
        }

        private static SolidColorBrush CreateFrozenBrush(uint argb, byte a = 255)
        {
            var brush = new SolidColorBrush(Color.FromArgb(
                a,
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF)
            ));
            brush.Freeze();
            return brush;
        }

        private static bool IsTheme(string theme, string currentTheme)
        {
            return !(currentTheme.IndexOf(theme, StringComparison.OrdinalIgnoreCase) < 0);
        }
    }
}
