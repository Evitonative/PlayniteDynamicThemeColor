using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DynamicThemeColor.Filters;
using Playnite.SDK;

namespace DynamicThemeColor.Theming {
    public class PaletteEngine {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly List<IColorFilter> filters =  new List<IColorFilter>();
        private readonly ColorPalette colorPalette;

        private ThemeRegistry themeRegistry;
        private Dictionary<string, string> rawVariables = new Dictionary<string, string>();
        private Dictionary<string, string> rawBrushes = new Dictionary<string, string>();
        private Dictionary<string, (uint color, byte alpha)> brushColors = new Dictionary<string, (uint color, byte alpha)>();

        private PaletteEngine(ColorPalette colorPalette) {
            this.colorPalette = colorPalette;
        }

        public static PaletteEngine From(ColorPalette palette) {
            return new PaletteEngine(palette);
        }

        public PaletteEngine WithThemeRegistry(ThemeRegistry registry, string themeName) {
            themeRegistry = registry;
            themeRegistry.ExtractThemeDefinitions(themeName, out rawVariables, out rawBrushes);
            return this;
        }

        public PaletteEngine AddFilter(IColorFilter filter) {
            logger.Debug($"Adding filter {filter.Name}");

            if (!(filter.ProcessingStage.Contains(ColorProcessingStage.Variable) || filter.ProcessingStage.Contains(ColorProcessingStage.Brush))) {
                logger.Debug($"Filter not compatible with palette stage {filter.Name}");
                return this;
            }

            filters.Add(filter);
            return this;
        }

        public PaletteEngine Build() {
            if (themeRegistry == null) {
                logger.Warn("No theme registry found added.");
                return this;
            }

            Dictionary<string, (uint color, byte alpha)> resolvedVariables = themeRegistry.ResolveDictionary(rawVariables, colorPalette, null, rawVariables);

            foreach (IColorFilter filter in filters.Where(filter => filter.ProcessingStage.Contains(ColorProcessingStage.Variable))) {
                logger.Debug($"Applying variable filter {filter.Name}");
                filter.ApplyFilter(colorPalette, resolvedVariables, ColorProcessingStage.Variable);
            }

            brushColors = themeRegistry.ResolveDictionary(rawBrushes, colorPalette, resolvedVariables);

            foreach (IColorFilter filter in filters.Where(filter => filter.ProcessingStage.Contains(ColorProcessingStage.Brush))) {
                logger.Debug($"Applying brush filter {filter.Name}");
                filter.ApplyFilter(colorPalette, brushColors, ColorProcessingStage.Brush);
            }

            return this;
        }

        public void Draw() {
            foreach (KeyValuePair<string, (uint color, byte alpha)> brushColor in brushColors) {
                string resource = brushColor.Key;
                uint color = brushColor.Value.color;
                byte alpha = brushColor.Value.alpha;

                logger.Debug($"Applying brush {brushColor.Key}");
                if (alpha == 0) {
                    Application.Current.Resources[resource] = Brushes.Transparent;
                    continue;
                }

                Brush brush = CreateFrozenBrush(color, alpha);
                Application.Current.Resources[resource] = brush;
            }
        }

        private static Brush CreateFrozenBrush(uint color, byte alpha = 255) {
            Brush brush = new SolidColorBrush(Color.FromArgb(
                alpha,
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)(color & 0xFF)
            ));
            brush.Freeze();
            return brush;
        }
    }
}
