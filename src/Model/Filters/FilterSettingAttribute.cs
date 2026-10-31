using System.Collections.Generic;

namespace DynamicThemeColor.Model.Filters {
    public abstract class FilterSettingAttribute {
        public string Label { get; set; }
        public string Tooltip { get; set; }
    }

    public class IntSliderSetting : FilterSettingAttribute {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Step { get; set; } = 1;
    }

    public class BoolToggleSetting : FilterSettingAttribute {
    }

    public class StringSetting : FilterSettingAttribute {
    }

    public class EnumDropdownSetting : FilterSettingAttribute {
    }
}
