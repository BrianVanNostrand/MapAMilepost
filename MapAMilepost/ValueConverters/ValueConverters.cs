using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using ArcGIS.Desktop.Internal.Mapping.Symbology;

namespace MapAMilepost.ValueConverters
{
    public class BooleanInverter : IValueConverter // returns the inverse of a boolean
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
    public class DirectionConverter : IValueConverter
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
    public class BrushConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">boolean as string, representing if the return value should be inverted. </param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true)//if value is true
            {
                if (parameter != null && (bool.Parse((string)parameter))) {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 207, 216, 255));
                }
                else
                {
                    return Brushes.White;
                }
            }
            else
            {
                if (parameter!=null && (bool.Parse((string)parameter)))//if "true" is passed as parameter, invert
                {
                    return Brushes.White;
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 207, 216, 255));
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BorderConverter : IValueConverter
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
    public class ElementHeightConverter : IValueConverter
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
    public class MapButtonLabelConverter : IValueConverter
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
    public class ComboBoxDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value == true)
                {
                    return "Decreasing";
                }
                if ((bool)value == false)
                {
                    return "Increasing";
                }
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((string)value == "Decreasing")
                {
                    return true;
                }
                if ((string)value == "Increasing")
                {
                    return false;
                }
            }
            return null;
        }
    }


    public class InteractionButtonLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value)//if session active
                {
                    return "Stop Mapping";
                }
                if (!(bool)value)//if session not active
                {
                    if ((string)parameter == "Point")
                    {
                        return "Start Mapping";
                    }
                    if ((string)parameter == "Start")
                    {
                        return "Map Start";
                    }
                    if ((string)parameter == "End")
                    {
                        return "Map End";
                    }
                }
            }
            return "test";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class InputModeButtonLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value)//if is Map Mode
                {
                    return "Map";
                }
                if (!(bool)value)//if session not active
                {
                    return "Form";
                }
            }
            return "test";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class VisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">string version of a boolean determining whether or not the visibility should be inverted.</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
            {
                flag = (bool)value;
            }
            else if (value is bool?)
            {
                var nullable = (bool?)value;
                flag = nullable.GetValueOrDefault();
            }
            if (parameter != null)
            {
                if (bool.Parse((string)parameter))
                {
                    flag = !flag;
                }
            }
            if (flag)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var back = ((value is Visibility) && (((Visibility)value) == Visibility.Visible));
            if (parameter != null)
            {
                if ((bool)parameter)
                {
                    back = !back;
                }
            }
            return back;
        }
    }
    public class NumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            double outValue = 0;
            if (value != null && (value.GetType() == typeof(double)))
            {
                outValue = Math.Round((double)value, 6);
            }
            return outValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    //public class DateStringConverter : IValueConverter 
    //{
    //    public object Convert(object value, Type targetType, object parameter,
    //            System.Globalization.CultureInfo culture)
    //    {
    //        return DateTime.Parse((string)value);
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        DateTime dt = DateTime.ParseExact(value, "ddMMyyyy",
    //                              CultureInfo.InvariantCulture);
    //        return $"{value.ToString("M/d/yyyy")}";
    //    }
    //}
}
