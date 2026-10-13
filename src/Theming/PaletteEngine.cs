using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DynamicThemeColor.Filters;
using Playnite.SDK;

namespace DynamicThemeColor.Theming {
    public class PaletteEngine {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly List<IColorFilter> variableFilters = new List<IColorFilter>();
        private readonly List<IColorFilter> brushFilters = new List<IColorFilter>();
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

        public PaletteEngine AddVariableFilter(IColorFilter filter) {
            return AddFilter(filter, ColorProcessingStage.Variable);
        }

        public PaletteEngine AddBrushFilter(IColorFilter filter) {
            return AddFilter(filter, ColorProcessingStage.Brush);
        }

        public PaletteEngine AddFilter(IColorFilter filter, ColorProcessingStage targetStage) {
            logger.Debug($"Adding filter '{filter.Name}' to stage '{targetStage}'");

            if (!filter.ProcessingStage.Contains(targetStage)) {
                logger.Warn($"Filter '{filter.Name}' does not support the {targetStage} stage.");
                return this;
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (targetStage) {
                case ColorProcessingStage.Variable:
                    variableFilters.Add(filter);
                    break;
                case ColorProcessingStage.Brush:
                    brushFilters.Add(filter);
                    break;
                default:
                    logger.Warn($"Stage '{targetStage}' is not handled by PaletteEngine.");
                    break;
            }

            return this;
        }

        public PaletteEngine Build() {
            if (themeRegistry == null) {
                logger.Warn("No theme registry found added.");
                return this;
            }

            Dictionary<string, (uint color, byte alpha)> resolvedVariables = themeRegistry.ResolveDictionary(rawVariables, colorPalette, null, rawVariables);

            foreach (IColorFilter filter in this.variableFilters) {
                logger.Debug($"Applying variable filter {filter.Name}");
                filter.ApplyFilter(colorPalette, resolvedVariables, ColorProcessingStage.Variable);
            }

            brushColors = themeRegistry.ResolveDictionary(rawBrushes, colorPalette, resolvedVariables);

            foreach (IColorFilter filter in this.brushFilters) {
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
