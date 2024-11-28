using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MapAMilepost.ValueConverters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BindingNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            double outValue = 0;
           if( value != null&& (value.GetType()==typeof(double)))
           {
               outValue = Math.Round((double)value,6);
           }
           return outValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
