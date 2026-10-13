using System.Windows.Media;
using Playnite.SDK;

namespace DynamicThemeColor.Theming {
    public static class ColorUtilities {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static uint? GetColorFromHex(string hex) {
            if (string.IsNullOrWhiteSpace(hex)) hex = "#ffffff";

            if (ColorConverter.ConvertFromString(hex) is Color color) {
                return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
            }

            logger.Warn($"Invalid hex color format: {hex}");
            return null;
        }
    }
}
