using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DynamicThemeColor.Filters;
using MaterialColorUtilities.Quantize;
using MaterialColorUtilities.Score;
using Playnite.SDK;

namespace DynamicThemeColor {
    public class SeedColorEngine {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly List<IColorFilter> filters = new List<IColorFilter>();
        private uint seedColor;

        private SeedColorEngine(uint seedColor) {
            this.seedColor = seedColor;
        }

        public static SeedColorEngine From(string imagePath, uint fallbackColor, int hashBase, int colorsToPickFrom = 1, int width = 128) {
            logger.Trace($"Loading image {imagePath}");

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
            bitmap.DecodePixelWidth = width;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            logger.Trace($"Loaded image {bitmap.UriSource}");

            return From(bitmap, fallbackColor, hashBase, colorsToPickFrom);
        }

        private static SeedColorEngine From(BitmapImage bitmap, uint fallbackColor, int hashBase, int colorsToPickFrom = 1) {
            logger.Trace($"Loading bitmap {bitmap.UriSource}");

            FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap();
            convertedBitmap.BeginInit();
            convertedBitmap.Source = bitmap;
            convertedBitmap.DestinationFormat = PixelFormats.Bgra32;
            convertedBitmap.EndInit();
            convertedBitmap.Freeze();

            int width = convertedBitmap.PixelWidth;
            int height = convertedBitmap.PixelHeight;

            logger.Trace($"Decoded and converted bitmap to {width}x{height} Bgra32");

            uint[] pixels = new uint[width * height];
            convertedBitmap.CopyPixels(pixels, width * 4, 0);

            Dictionary<uint, uint> quantiserResult = QuantizerCelebi.Quantize(pixels, 128);
            List<uint> rankedColors = Scorer.Score(quantiserResult);

            if (rankedColors.Count == 0) {
                logger.Warn("No ranked colors found");
                return new SeedColorEngine(fallbackColor);
            }

            if (colorsToPickFrom <= 0) {
                logger.Warn("At least one color required to pick from. Using 1");
                colorsToPickFrom = 1;
            }

            List<uint> topColors = rankedColors.Take(colorsToPickFrom).ToList();
            int index = Math.Abs(hashBase % topColors.Count);

            return new SeedColorEngine(topColors[index]);
        }

        public SeedColorEngine AddFilter(IColorFilter filter) {
            logger.Debug($"Adding filter {filter.Name}");

            if (!filter.ProcessingStage.Contains(ColorProcessingStage.Seed)) {
                logger.Warn($"Filter not compatible with seed stage {filter.Name}");
                return this;
            }

            filters.Add(filter);
            return this;
        }

        public uint Build() {
            foreach (IColorFilter filter in filters) {
                logger.Debug($"Applying filter {filter.Name}");
                seedColor = filter.ApplyFilter(seedColor);
            }

            return this.seedColor;
        }
    }
}
