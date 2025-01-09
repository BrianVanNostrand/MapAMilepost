using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MapAMilepost.ValueConverters
{
    class MultiValueConverters
    {
    }
    public class ExecuteButtonToolTipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool sessionActive = (bool)values[0];
            bool isMapMode = (bool)values[1];
            string sessionType = (string)values[2];
            if (isMapMode)//if tool mode is set to map
            {
                if (sessionActive)//session is active
                {
                    return "Stop mapping session.";
                }
                else
                {
                    if (sessionType == "Start")
                    {
                        return "Start mapping session for beginning of line.";
                    }
                    if (sessionType == "End")
                    {
                        return "Start mapping session for end of line.";
                    }
                    if (sessionType == "Point")
                    {
                        return "Start mapping session.";
                    }
                }
            }

            else //if tool mode is set to form
            {
                if (sessionType == "Start")
                {
                    return "Create start milepost using the information above.";
                }
                if (sessionType == "End")
                {
                    return "Create end milepost using the information above.";
                }
                if (sessionType == "Point")
                {
                    return "Create milepost using the information above.";
                }
            }
            return null;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

