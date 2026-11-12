using System.Windows.Forms;
using System.Windows.Media;

namespace DynamicThemeColor.Utils {
    public static class ColorPicker {
        public static bool ExecutePickColor(out string newColor, string defaultColor = null) {
            using (ColorDialog dialog = new ColorDialog()) {
                try {
                    if (defaultColor != null && ColorConverter.ConvertFromString(defaultColor) is Color c) {
                        dialog.Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                    }
                } catch {
                    // ignored
                }

                if (dialog.ShowDialog() == DialogResult.OK) {
                    newColor = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                    return true;
                }
            }

            newColor = string.Empty;
            return false;
        }
    }
}
