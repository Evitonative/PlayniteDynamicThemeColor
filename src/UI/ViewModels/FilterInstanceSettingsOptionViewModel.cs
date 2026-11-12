using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using Playnite.SDK;

namespace DynamicThemeColor.UI.ViewModels {
    public sealed class FilterInstanceSettingsOptionViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly object targetObject;
        private readonly PropertyInfo prop;
        private readonly FilterSettingAttribute attr;

        public Type SettingType => attr.GetType();
        public string Label => !string.IsNullOrEmpty(attr.Label) ? attr.Label : prop.Name;
        public string Tooltip => attr.Tooltip;
        public bool HasTooltip => !string.IsNullOrEmpty(Tooltip);

        public object Value {
            get {
                object val = prop.GetValue(targetObject);
                switch (val) {
                    case null:
                        return null;
                    case int intVal:
                        return Convert.ToDouble(intVal);
                    default:
                        return prop.GetValue(targetObject);
                }
            }
            set {
                switch (value) {
                    case null:
                        if (prop.PropertyType.IsClass || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                            prop.SetValue(targetObject, null);
                        OnPropertyChanged();
                        return;
                    case string s:
                        Type underlyingPropType = Nullable.GetUnderlyingType(prop.PropertyType)
                                              ?? prop.PropertyType;

                        if (underlyingPropType == typeof(int)) {
                            int.TryParse(s, out int intVal);
                            prop.SetValue(targetObject, intVal);
                            OnPropertyChanged();
                            return;
                        }

                        if (underlyingPropType == typeof(double)) {
                            double.TryParse(s, out double doubleVal);
                            prop.SetValue(targetObject, doubleVal);
                            OnPropertyChanged();
                            return;
                        }

                        prop.SetValue(targetObject, s);
                        OnPropertyChanged();
                        return;
                }

                if (!prop.PropertyType.IsAssignableFrom(value.GetType())) {
                    value = Convert.ChangeType(value, prop.PropertyType);
                }

                prop.SetValue(targetObject, value);
                OnPropertyChanged();
            }
        }

        public double? Min {
            get {
                Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType)
                                      ?? prop.PropertyType;
                switch (attr) {
                    case IntSliderSetting setting when underlyingType == typeof(int):
                        return setting.Min;
                    case DoubleSliderSetting settings when underlyingType == typeof(double):
                        return settings.Min;
                    default:
                        return null;
                }
            }
        }

        public double? Max {
            get {
                Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType)
                                      ?? prop.PropertyType;
                switch (attr) {
                    case IntSliderSetting setting when underlyingType == typeof(int):
                        return setting.Max;
                    case DoubleSliderSetting settings when underlyingType == typeof(double):
                        return settings.Max;
                    default:
                        return null;
                }
            }
        }

        public double? Step {
            get {
                Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType)
                                      ?? prop.PropertyType;
                switch (attr) {
                    case IntSliderSetting setting when underlyingType == typeof(int):
                        return setting.Step;
                    case DoubleSliderSetting settings when underlyingType == typeof(double):
                        return settings.Step;
                    default:
                        return null;
                }
            }
        }

        public Array EnumValues {
            get {
                Array enumValues = null;
                if (prop.PropertyType.IsEnum) {
                    enumValues = Enum.GetValues(prop.PropertyType);
                }

                return enumValues;
            }
        }

        public ICommand ResetCommand { get; }
        public ICommand PickColorCommand { get; }

        public FilterInstanceSettingsOptionViewModel(object targetObject, PropertyInfo prop, FilterSettingAttribute attr) {
            this.targetObject = targetObject;
            this.prop = prop;
            this.attr = attr;
            ResetCommand = new RelayCommand(Reset);
            PickColorCommand = new RelayCommand(ExecuteColorPicker);
        }

        public void Reset() {
            object newInstance = Activator.CreateInstance(targetObject.GetType());
            Value = prop.GetValue(newInstance);
        }

        private void ExecuteColorPicker() {
            if (prop.PropertyType != typeof(string) || SettingType != typeof(ColorSetting)) return;

            if(!MaterialColorHelper.ExecutePickColor(out string color, defaultColor: Value as string)) return;
            Value = color;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SettingTemplateSelector : DataTemplateSelector {
        public DataTemplate IntSliderTemplate { get; set; }
        public DataTemplate DoubleSliderTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (!(item is FilterInstanceSettingsOptionViewModel vm)) return null;

            if (vm.SettingType == typeof(IntSliderSetting))
                return IntSliderTemplate;
            if (vm.SettingType == typeof(DoubleSliderSetting))
                return DoubleSliderTemplate;
            if (vm.SettingType == typeof(BoolToggleSetting))
                return BoolTemplate;
            if (vm.SettingType == typeof(StringSetting))
                return StringTemplate;
            if (vm.SettingType == typeof(ColorSetting))
                return ColorTemplate;
            return vm.SettingType == typeof(EnumDropdownSetting)
                ? EnumTemplate
                : throw new ArgumentOutOfRangeException();
        }
    }
}
