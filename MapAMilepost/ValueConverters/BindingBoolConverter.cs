using ArcGIS.Desktop.Internal.Framework.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MapAMilepost.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BindingBoolConverter : IValueConverter // returns the inverse of a boolean
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BindingDecreaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string valueString = "null";
            if (value == null || (value.GetType() != typeof(bool)))
            {
                if (parameter == null)
                {
                    valueString = "N/A";
                }
                else
                {
                    valueString = "";
                }
            }
            else
            {
                switch (value.ToString().ToLower())
                {
                    case "true":
                        valueString = "Decreasing";
                        break;
                    case "false":
                        valueString = "Increasing";
                        break;
                }
            }

            return valueString;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BindingBoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true)
            {
                if (parameter.ToString() == "Map")
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 207, 216, 255));
                }
                else
                {
                    return Brushes.White;
                }
            }
            if ((bool)value == false)
            {
                if (parameter.ToString() == "Map")
                {
                    return Brushes.White;
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 207, 216, 255));
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BindingBoolToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true)
            {
                return 0;
            }
            if ((bool)value == false)
            {
                return 1;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BindingBoolToElementHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BindingBoolToMapButtonLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
