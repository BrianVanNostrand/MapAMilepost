using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;
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
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Map view is not active.");
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please wait for map to finish loading.");
                }
            }
            return MapViewActive;
        }

        public static async Task<GraphicsLayer> GetMilepostMappingLayer(Map map)
        {
            GraphicsLayer targetLayer = null;
            await QueuedTask.Run(() =>
            {
                foreach (var item in map.Layers)
                {
                    CIMBaseLayer baseLayer = item.GetDefinition();
                    if (baseLayer.CustomProperties != null && baseLayer.CustomProperties.Length > 0)
                    {
                        foreach (var prop in baseLayer.CustomProperties)
                        {
                            if (prop.Key == "MilepostMappingLayer" && prop.Value == "true")
                            {
                                targetLayer = item as GraphicsLayer;
                            }
                        };
                    }

                };
            });
            return targetLayer;
        }

    }
}
