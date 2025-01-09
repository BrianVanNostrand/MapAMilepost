using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using Microsoft.VisualBasic;
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
                    var projectMapItems = Project.Current.GetItems<MapProjectItem>();
                    if(projectMapItems != null && (projectMapItems.Count()==0)) {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please create a map to use with the tool.");
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please wait for map to finish loading.");
                    }
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
        public static async Task<bool> CreateMilepostMappingLayer(Map map)
        {
            string title = Interaction.InputBox("Please enter a title for your milepost graphics layer.", "Create Milepost Layer", "My Milepost Layer");
            if (title != "")
            {
                GraphicsLayerCreationParams gl_param = new() { Name = title };
                await QueuedTask.Run(() =>
                {
                    GraphicsLayer graphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(gl_param, map);

                    CIMBaseLayer newDefinition = graphicsLayer.GetDefinition();
                    CIMStringMap[] CustomLayerProps = new CIMStringMap[]//hidden ID for map layer
                    {
                    new() { Key = "MilepostMappingLayer", Value = "true" },
                    };
                    newDefinition.CustomProperties = CustomLayerProps;
                    graphicsLayer.SetDefinition(newDefinition);//add custom prop to layer
                });
                await FrameworkApplication.SetCurrentToolAsync("MapAMilepost_MapTool");
            }
            return (title == "" ? false : true);
        }
    }
}
