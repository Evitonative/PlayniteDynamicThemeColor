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

        private CorePalette GetPalette(uint seedArgb)
        {
            return CorePalette.Of(seedArgb, Settings.Settings.PaletteStyle);
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
            var accentColor = palette.Primary[80];
            var onAccentColor = palette.Primary[10];
            
            if (Settings.Settings.UseSeedColorForGlyph)
            {
                accentColor = seedArgb;
                
                var lightness = ColorUtils.LStarFromArgb(accentColor);
                onAccentColor = lightness <= 40 ? palette.Primary[90] : palette.Primary[10];
            }
            
            ApplyBrushes(
                ("ControlBackgroundBrush", CreateFrozenBrush(palette.Neutral[20], 64)),
                ("TextBrush", CreateFrozenBrush(palette.Neutral[90])),
                ("TextBrushDarker", CreateFrozenBrush(palette.NeutralVariant[70])),
                ("TextBrushDark", CreateFrozenBrush(onAccentColor)),
                ("NormalBrush", CreateFrozenBrush(palette.Neutral[20])),
                ("NormalBrushDark", CreateFrozenBrush(palette.Neutral[12])),
                ("NormalBorderBrush", Brushes.Transparent),
                ("HoverBrush", CreateFrozenBrush(palette.Primary[40], 80)),
                ("GlyphBrush", CreateFrozenBrush(accentColor)),
                ("HighlightGlyphBrush", CreateFrozenBrush(accentColor)),
                ("PopupBorderBrush", CreateFrozenBrush(palette.NeutralVariant[35])),
                ("TooltipBackgroundBrush", CreateFrozenBrush(palette.Neutral[15])),
                ("ButtonBackgroundBrush", CreateFrozenBrush(palette.Neutral[25])),
                ("GridItemBackgroundBrush", Brushes.Transparent),
                ("PanelSeparatorBrush", CreateFrozenBrush(palette.Neutral[90], 13)),
                ("WindowPanelSeparatorBrush", Brushes.Transparent),
                ("PopupBackgroundBrush", CreateFrozenBrush(palette.Neutral[10])),
                ("CheckBoxCheckMarkBrush", CreateFrozenBrush(accentColor)),
                ("ExpanderBackgroundBrush", Brushes.Transparent),
                // ReSharper disable once StringLiteralTypo
                ("WindowBackgourndBrush", CreateFrozenBrush(palette.Neutral[8])),
                ("WindowBackgroundBrush", CreateFrozenBrush(palette.Neutral[8]))
            );

            // Harmony Theme Specific
            var currentTheme = PlayniteApi.ApplicationSettings.DesktopTheme ?? string.Empty;
            if (currentTheme.IndexOf("Harmony", StringComparison.OrdinalIgnoreCase) < 0) return;
            ApplyBrushes(
                ("WindowBackgroundBrush", CreateFrozenBrush(palette.Neutral[8])),
                ("MainWindowBackgroundBrush", CreateFrozenBrush(palette.Neutral[8])),
                ("PlayButtonTextBrush", CreateFrozenBrush(onAccentColor)),
                ("ButtonMouseOverBackgroundBrush", CreateFrozenBrush(palette.Neutral[35])),
                ("MainPanelBackgroundBrush", CreateFrozenBrush(palette.Neutral[8])),
                ("SidebarBackgroundBrush", CreateFrozenBrush(palette.Neutral[90], 13)),
                ("SidebarItemBackgroundBrush", CreateFrozenBrush(palette.Neutral[90], 13)),
                ("TopPanelSearchBoxBackgroundBrush", CreateFrozenBrush(palette.Neutral[15])),
                ("DetailsViewBarBackgroundBrush", CreateFrozenBrush(palette.Neutral[90], 15)),
                ("OpaqueHighlightBrush", CreateFrozenBrush(accentColor, 64)),
                ("OpaqueGrayHighlightBrush", CreateFrozenBrush(accentColor, 64)),
                ("OpaqueHighlightBrushBright", CreateFrozenBrush(palette.Primary[90], 64)),
                ("SecondaryBrush", CreateFrozenBrush(palette.NeutralVariant[30])),
                ("BottomButtonHighlightBrush", CreateFrozenBrush(palette.Primary[70]))
            );

            if (Settings.Settings.EnableHarmonyDetailsPanelBrush)
                ApplyBrushes(("DetailsViewPanelBackgroundBrush", CreateFrozenBrush(palette.NeutralVariant[15])));
        }
        
        private static void ApplyBrushes(params (string resource, Brush brush)[] brushes)
        {
            foreach (var (resource, brush) in brushes)
            {
                Application.Current.Resources[resource] = brush;
            }
        }

        private static uint GetColorFromImage(string imagePath, Guid gameId, int maxColors)
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

            return topColors[index];
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

        public void ClearCache(bool silent = false)
        {
            _paletteCache.Clear();

            if (silent) return;
            PlayniteApi.Dialogs.ShowMessage(
                ResourceProvider.GetString("LOCDynamicThemeColorCacheCleared"), 
                "Dynamic Theme Color"
            );
        }
        
        public void RefreshTheme()
        {
            var game = PlayniteApi.MainView.SelectedGames?.FirstOrDefault();
            UpdateTheme(game);
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
                    _paletteCache[game.Id] = (palette, seedArgb);

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
    }
}
