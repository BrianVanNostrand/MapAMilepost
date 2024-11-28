using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MapAMilepost.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BindingBoolConverter : IValueConverter
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
            if (value==null||(value.GetType() != typeof(bool)))
            {
                if(parameter == null)
                {
                    valueString= "N/A";
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
                        valueString= "Decreasing";
                        break;
                    case "false":
                        valueString= "Increasing";
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
}
