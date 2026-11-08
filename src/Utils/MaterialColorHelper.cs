using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static FormatConvertedBitmap GetFormatConvertedBitmap(string filePath) {
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
