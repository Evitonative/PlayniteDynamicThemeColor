using System;

namespace DynamicThemeColor.Model.Filters {
    public abstract class FilterSettingAttribute : Attribute {
        public string Label { get; set; }
        public string Tooltip { get; set; }
    }

    public class IntSliderSetting : FilterSettingAttribute {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Step { get; set; } = 1;
    }

    public class DoubleSliderSetting : FilterSettingAttribute {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Step { get; set; } = 1.0;
    }

    public class BoolToggleSetting : FilterSettingAttribute {
    }

    public class StringSetting : FilterSettingAttribute {
    }

    public class ColorSetting : FilterSettingAttribute {
    }

    public class EnumDropdownSetting : FilterSettingAttribute {
    }
}
