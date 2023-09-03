using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetPrintsEditor.Converters
{
    // From https://stackoverflow.com/a/5182660/4332314
    public class BooleanConverter<T>(T trueValue, T falseValue) : IValueConverter
    {
        public T True { get; set; } = trueValue;
        public T False { get; set; } = falseValue;

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T convertedValue && EqualityComparer<T>.Default.Equals(convertedValue, True);
        }
    }

    public sealed class BoolToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BoolToVisibilityConverter() :
            base(Visibility.Visible, Visibility.Collapsed)
        {
        }
    }

    public sealed class BoolToDoubleConverter : BooleanConverter<double>
    {
        public BoolToDoubleConverter() :
            base(0, 0)
        {
        }
    }

    public sealed class BoolToThicknessConverter : BooleanConverter<Thickness>
    {
        public BoolToThicknessConverter() :
            base(new Thickness(0), new Thickness(0))
        {
        }
    }
}
