using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialColorUtilities.Palettes;
using MaterialColorUtilities.Quantize;
using MaterialColorUtilities.Score;

namespace DynamicThemeColor.Utils {
    public static class MaterialColorHelper {
        public static uint ExtractSeedColor(string filePath, int maxColors, int hasCode) {
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

            int width = convertedBitmap.PixelWidth;
            int height = convertedBitmap.PixelHeight;
            uint[] pixels = new uint[width * height];
            convertedBitmap.CopyPixels(pixels, width * 4, 0);

            Dictionary<uint, uint> quantiserResult = QuantizerCelebi.Quantize(pixels, 128);
            List<uint> rankedColors = Scorer.Score(quantiserResult);

            if (rankedColors.Count == 0) return 0;

            if (maxColors <= 1) return rankedColors.FirstOrDefault();

            List<uint> topColors = rankedColors.Take(maxColors).ToList();
            int index = (int)(Math.Abs((long)hasCode) % topColors.Count);
            return topColors[index];
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
}
