using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapAMilepost.Utils
{
    class MapViewUtils
    {
        public static bool CheckMapView()
        {
            bool MapViewActive = false;
            if (MapView.Active != null && MapView.Active.Map != null)
            {
                MapViewActive = true;
            }
            else
            {
                if (LayoutView.Active != null)
                {
                    MessageBox.Show("Map view is not active.");
                }
                else
                {
                    MessageBox.Show("Please wait for map to finish loading.");
                }
            }
            return MapViewActive;
        }
    }
}
