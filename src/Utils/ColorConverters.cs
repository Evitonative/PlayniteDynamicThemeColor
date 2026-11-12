namespace DynamicThemeColor.Utils
{
    public static class ColorConverters
    {
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
    }
}