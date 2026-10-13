using System.Collections.Generic;

namespace DynamicThemeColor.Theming {
    public class ThemeDefinition {
        public string Name { get; set; }
        public string[] InheritsFrom { get; set; } = { };
        public Dictionary<string, string> Brushes { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }
}
