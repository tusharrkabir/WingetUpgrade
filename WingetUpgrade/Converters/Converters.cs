using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WingetUpgrade.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = value is bool b && b;
            if (Invert) val = !val;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }

    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count) return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is string s) return !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveTabToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string current = value?.ToString() ?? "";
            string target = parameter?.ToString() ?? "";

            if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromRgb(0x1F, 0x24, 0x2B)); // Highlighted dark background
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveTabToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string current = value?.ToString() ?? "";
            string target = parameter?.ToString() ?? "";

            if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            {
                return Brushes.White;
            }
            return new SolidColorBrush(Color.FromRgb(0x8C, 0x96, 0xA5)); // Muted icon & text
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveTabToIndicatorVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string current = value?.ToString() ?? "";
            string target = parameter?.ToString() ?? "";

            return string.Equals(current, target, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CategoryChipBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string current = value?.ToString() ?? "";
            string target = parameter?.ToString() ?? "";
            if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)); // Vibrant Accent Green
            }
            return new SolidColorBrush(Color.FromRgb(0x19, 0x1E, 0x26)); // Subtle Dark Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class CategoryChipFgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string current = value?.ToString() ?? "";
            string target = parameter?.ToString() ?? "";
            if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromRgb(0x07, 0x1F, 0x11)); // High contrast dark green text
            }
            return new SolidColorBrush(Color.FromRgb(0x9E, 0xA8, 0xB6)); // Neutral gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasText = !string.IsNullOrEmpty(value as string);
            if (Invert) hasText = !hasText;
            return hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZeroToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isZero = false;
            if (value is int count) isZero = count == 0;
            else if (value == null) isZero = true;

            if (Invert) isZero = !isZero;
            return isZero ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IconKeyToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value?.ToString() ?? "";
            string resourceKey = key switch
            {
                "Chrome" => "IconChrome",
                "Firefox" => "IconFirefox",
                "VSCode" => "IconVSCode",
                "Git" => "IconGit",
                "VLC" => "IconVLC",
                "Terminal" => "IconTerminal",
                "Python" => "IconPython",
                _ => "IconStatPackage"
            };

            if (Application.Current?.Resources?.Contains(resourceKey) == true)
            {
                return (Geometry)Application.Current.Resources[resourceKey];
            }
            return (Geometry)Application.Current!.FindResource("IconStatPackage");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IconKeyToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(0x42, 0x85, 0xF4));
        private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xFF, 0x71, 0x39));
        private static readonly SolidColorBrush CodeBlueBrush = new(Color.FromRgb(0x00, 0x7A, 0xCC));
        private static readonly SolidColorBrush GitRedBrush = new(Color.FromRgb(0xF0, 0x50, 0x32));
        private static readonly SolidColorBrush VlcOrangeBrush = new(Color.FromRgb(0xF9, 0x73, 0x16));
        private static readonly SolidColorBrush TerminalGreenBrush = new(Color.FromRgb(0x4A, 0xDE, 0x80));
        private static readonly SolidColorBrush PythonBlueBrush = new(Color.FromRgb(0x38, 0xBD, 0xF8));
        private static readonly SolidColorBrush DefaultGreenBrush = new(Color.FromRgb(0x22, 0xC5, 0x5E));

        static IconKeyToBrushConverter()
        {
            BlueBrush.Freeze();
            OrangeBrush.Freeze();
            CodeBlueBrush.Freeze();
            GitRedBrush.Freeze();
            VlcOrangeBrush.Freeze();
            TerminalGreenBrush.Freeze();
            PythonBlueBrush.Freeze();
            DefaultGreenBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value?.ToString() ?? "";
            return key switch
            {
                "Chrome" => BlueBrush,
                "Firefox" => OrangeBrush,
                "VSCode" => CodeBlueBrush,
                "Git" => GitRedBrush,
                "VLC" => VlcOrangeBrush,
                "Terminal" => TerminalGreenBrush,
                "Python" => PythonBlueBrush,
                _ => DefaultGreenBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

