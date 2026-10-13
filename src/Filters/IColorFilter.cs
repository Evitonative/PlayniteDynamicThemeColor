using System.Collections.Generic;

namespace DynamicThemeColor.Filters {
    public interface IColorFilter {
        string Name { get; }
        ColorProcessingStage[] ProcessingStage { get; }

        uint ApplyFilter(uint input);
        void ApplyFilter(
            ColorPalette palette,
            Dictionary<string, (uint, byte)> colorMap,
            ColorProcessingStage currentStage);
    }

    public enum ColorProcessingStage {
        Seed,
        Variable,
        Brush,
    }
}
