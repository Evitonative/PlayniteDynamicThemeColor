using System.Collections.Generic;
using MaterialColorUtilities.Palettes;

// ReSharper disable RedundantDefaultMemberInitializer
namespace DynamicThemeColor.Settings
{
    public class AddonSettings : ObservableObject
    {
        // View Targets
        private bool applyInListView = true;
        public bool ApplyInListView
        {
            get => applyInListView;
            set => SetValue(ref applyInListView, value);
        }

        private bool applyInDetailsView = true;
        public bool ApplyInDetailsView
        {
            get => applyInDetailsView;
            set => SetValue(ref applyInDetailsView, value);
        }

        private bool applyInGridView = true;
        public bool ApplyInGridView
        {
            get => applyInGridView;
            set => SetValue(ref applyInGridView, value);
        }

        // Color Generation
        private Style paletteStyle = Style.TonalSpot;
        public Style PaletteStyle
        {
            get => paletteStyle;
            set => SetValue(ref paletteStyle, value);
        }

        private bool preferCoverArt = true;
        public bool PreferCoverArt
        {
            get => preferCoverArt;
            set => SetValue(ref preferCoverArt, value);
        }

        private int colorSelectionRange = 1;
        public int ColorSelectionRange
        {
            get => colorSelectionRange;
            set => SetValue(ref colorSelectionRange, value);
        }

        private string defaultSeedColorHex = "#1cd8fc";
        public string DefaultSeedColorHex
        {
            get => defaultSeedColorHex;
            set => SetValue(ref defaultSeedColorHex, value);
        }

        private bool useSeedColorForGlyph = false;
        public bool UseSeedColorForGlyph
        {
            get => useSeedColorForGlyph;
            set => SetValue(ref useSeedColorForGlyph, value);
        }

        private double saturationSlider = 0.0;
        public double SaturationSlider
        {
            get => saturationSlider;
            set { saturationSlider = value; OnPropertyChanged(); }
        }

        private double lightnessSlider = 0.0;
        public double LightnessSlider
        {
            get => lightnessSlider;
            set { lightnessSlider = value; OnPropertyChanged(); }
        }

        // Theme Specific
        private bool enableHarmonyDetailsPanelBrush = false;
        public bool EnableHarmonyDetailsPanelBrush
        {
            get => enableHarmonyDetailsPanelBrush;
            set => SetValue(ref enableHarmonyDetailsPanelBrush, value);
        }

        // Advanced
        private bool alwaysEnabledContrastImprovements = false;
        public bool AlwaysEnabledContrastImprovements
        {
            get => alwaysEnabledContrastImprovements;
            set => SetValue(ref alwaysEnabledContrastImprovements, value);
        }
    }
}
