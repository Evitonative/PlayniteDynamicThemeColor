using System.Collections.Generic;
using DynamicThemeColor.Model;
using MaterialColorUtilities.Palettes;
using Playnite.SDK;

namespace DynamicThemeColor.Utils {
    public class ThemePalette {
        private readonly ILogger logger =  LogManager.GetLogger();
        private readonly CorePalette corePalette;
        private readonly Dictionary<ColorScheme.MaterialPalette, Dictionary<uint, uint>> overriddenTones =
            new Dictionary<ColorScheme.MaterialPalette, Dictionary<uint, uint>>();

        public uint SeedColor { get; }

        public ThemePalette(CorePalette corePalette, uint seed) {
            this.corePalette = corePalette;
            SeedColor = seed;
        }

        public uint GetColor(ColorScheme.MaterialPalette palette, uint tone) {
            if (overriddenTones.TryGetValue(palette, out Dictionary<uint, uint> tones)) {
                if (tones.TryGetValue(tone, out uint color)) return color;
            }

            TonalPalette tonalPalette;
            switch (palette) {
                case ColorScheme.MaterialPalette.Neutral:
                    tonalPalette = corePalette.Neutral;
                    break;
                case ColorScheme.MaterialPalette.NeutralVariant:
                    tonalPalette = corePalette.NeutralVariant;
                    break;
                case ColorScheme.MaterialPalette.Primary:
                    tonalPalette = corePalette.Primary;
                    break;
                case ColorScheme.MaterialPalette.Secondary:
                    tonalPalette = corePalette.Secondary;
                    break;
                case ColorScheme.MaterialPalette.Tertiary:
                    tonalPalette = corePalette.Tertiary;
                    break;
                default:
                    logger.Error($"Unimplemented {palette} theme palette matching.");
                    tonalPalette = corePalette.Neutral;
                    break;
            }

            return tonalPalette[tone];
        }

        public void SetColor(ColorScheme.MaterialPalette palette, uint tone, uint argb) {
            if (!overriddenTones.TryGetValue(palette, out Dictionary<uint, uint> tones)) {
                overriddenTones[palette] = new Dictionary<uint, uint>();
                tones = overriddenTones[palette];
            }

            tones[tone] = argb;
        }
    }
}
