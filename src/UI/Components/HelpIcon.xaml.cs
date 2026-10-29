using System.Windows;

namespace DynamicThemeColor.UI.Components {
    public partial class HelpIcon {
        public HelpIcon() {
            InitializeComponent();
        }

        public static readonly DependencyProperty HelpToolTipProperty =
            DependencyProperty.Register(
                nameof(HelpToolTip),
                typeof(object),
                typeof(HelpIcon),
                new PropertyMetadata(null));

        public object HelpToolTip {
            get { return GetValue(HelpToolTipProperty); }
            set { SetValue(HelpToolTipProperty, value); }
        }
    }
}

