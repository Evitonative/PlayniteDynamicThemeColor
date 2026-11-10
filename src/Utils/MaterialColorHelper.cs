using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using MaterialColorUtilities.Palettes;
using MaterialColorUtilities.Quantize;
using MaterialColorUtilities.Score;
using Color = System.Windows.Media.Color;

namespace DynamicThemeColor.Utils {
    public static class MaterialColorHelper {
        public static ColorExtractionResults ExtractSeedColor(string filePath, int maxColors, int hasCode) {
            FormatConvertedBitmap convertedBitmap = GetFormatConvertedBitmap(filePath);

            int width = convertedBitmap.PixelWidth;
            int height = convertedBitmap.PixelHeight;
            uint[] pixels = new uint[width * height];
            convertedBitmap.CopyPixels(pixels, width * 4, 0);

            Dictionary<uint, uint> quantiserResult = QuantizerCelebi.Quantize(pixels, 128);
            List<uint> rankedColors = Scorer.Score(quantiserResult);

            if (rankedColors.Count == 0) return new ColorExtractionResults(quantiserResult);

            List<uint> topColors = rankedColors.Take(maxColors).ToList();
            if (maxColors <= 1) return new ColorExtractionResults(quantiserResult, rankedColors, maxColors, 0);
            int index = (int)(Math.Abs((long)hasCode) % topColors.Count);
            return new ColorExtractionResults(quantiserResult, rankedColors, maxColors, index);
        }

        private static FormatConvertedBitmap GetFormatConvertedBitmap(string filePath) {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(Path.GetFullPath(filePath));
            bitmap.DecodePixelWidth = 128;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap();
            convertedBitmap.BeginInit();
            convertedBitmap.Source = bitmap;
            convertedBitmap.DestinationFormat = PixelFormats.Bgra32;
            convertedBitmap.EndInit();
            convertedBitmap.Freeze();
            return convertedBitmap;
        }

        public static CorePalette GetPalette(uint seedArgb, Style style) {
            return CorePalette.Of(seedArgb, style);
        }

        public static SolidColorBrush CreateFrozenBrush(uint argb, byte alpha = 255) {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(
                alpha,
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF)
            ));
            brush.Freeze();
            return brush;
        }

        public static uint? ConvertHexToArgb(string hex) {
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#")) return null;

            string cleanHex = hex.Substring(1);

            if (!uint.TryParse(cleanHex, System.Globalization.NumberStyles.HexNumber, null, out uint argb)) return null;
            switch (cleanHex.Length) {
                case 6:
                    return 0xFF000000 | argb;
                case 8:
                    return argb;
                default:
                    return null;
            }
        }

        public static string ExecutePickColor(string color) {
            using (ColorDialog dialog = new ColorDialog()) {
                try {
                    if (ColorConverter.ConvertFromString(color) is Color c) {
                        dialog.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                    }
                } catch {
                    // ignored
                }

                if (dialog.ShowDialog() == DialogResult.OK) {
                    color = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                }
            }

            return color;
        }
    }

    public class ColorExtractionResults {
        [CanBeNull] public IReadOnlyDictionary<uint, uint> QuantiserResult { get; }
        [NotNull] public IReadOnlyList<uint> RankedColors  { get; }
        public int ColorIndex { get; }
        public int ColorCount { get; }
        [NotNull] public List<uint> TopColors => RankedColors.Take(ColorCount).ToList();
        public uint SeedColor => RankedColors.ElementAt(ColorIndex);

        public ColorExtractionResults(Dictionary<uint, uint> quantiserResult, List<uint> rankedColors,int colorCount, int colorIndex) {
            QuantiserResult = quantiserResult;
            RankedColors = rankedColors;
            ColorCount = colorCount;
            ColorIndex = colorIndex;
        }

        public ColorExtractionResults(Dictionary<uint, uint> quantiserResult) {
            QuantiserResult = quantiserResult;
            RankedColors = new List<uint> { 0 };
            ColorCount = 1;
            ColorIndex = 0;
        }

        public ColorExtractionResults(uint argb) {
            RankedColors = new List<uint> { argb };
            ColorCount = 1;
            ColorIndex = 0;
        }
    }
}
