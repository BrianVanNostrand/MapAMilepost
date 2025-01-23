using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Internal.Mapping.CommonControls;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Commands;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using MapAMilepost.ViewModels;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MapAMilepost
{
    public class MapAMilepostMaptool : ArcGIS.Desktop.Mapping.MapTool
    {
        private static Utils.ViewModelBase CurrentViewModel {  get; set; }
        private static string MapToolSessionType {  get; set; } //"point", "start", or "end"
        
        public MapAMilepostMaptool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Map;
        }
        /// <summary>
        /// -   Unmodified ESRI methods
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }
        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            return base.OnToolDeactivateAsync(hasMapViewChanged);
        }
        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            return base.OnSketchCompleteAsync(geometry);
        }

        /// <summary>
        /// -   Create a map point from the clicked point on the map.
        /// -   If the ViewModel exists (Either the point or line VM always will because it's assigned when the 
        ///     mapping session is initialized in SetSession), then submit that map point to the viewmodel
        ///     to use it in an SOE request.
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                MapPoint mapPoint = await QueuedTask.Run(() =>
                {
                    return MapView.Active.ClientToMap(e.ClientPoint);
                });
                if(mapPoint != null)
                {
                    await Commands.GraphicsCommands.DeleteUnsavedGraphics(MapToolSessionType);//delete all unsaved graphics
                    List<List<double>> lineGeometryResponse = new();
                    if (MapToolSessionType == "start")
                    {
                        if(CurrentViewModel.LineResponse.EndResponse != null && CurrentViewModel.LineResponse.EndResponse.Route!=null && CurrentViewModel.LineResponse.EndResponse.Decrease != null)//if end point already exists, use its route
                        {
                            CurrentViewModel.LineResponse.StartResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.LineArgs.StartArgs, CurrentViewModel.LineResponse.EndResponse.Route) as PointResponseModel);
                        }
                        else
                        {
                            CurrentViewModel.LineResponse.StartResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint,CurrentViewModel.LineArgs.StartArgs) as PointResponseModel);
                            if (CurrentViewModel.LineResponse != null && CurrentViewModel.LineResponse.StartResponse != null && CurrentViewModel.LineResponse.StartResponse.Route != null)
                            {
                                CurrentViewModel.LineResponse.EndResponse.Route = CurrentViewModel.LineResponse.StartResponse.Route;
                            }
                        }
                        lineGeometryResponse = await Utils.MapToolUtils.GetLine(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, CurrentViewModel.LineArgs.StartArgs.SR, CurrentViewModel.LineArgs.StartArgs.ReferenceDate);
                        await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.LineArgs.StartArgs, CurrentViewModel.LineResponse.StartResponse, MapToolSessionType);
                    }
                    else if (MapToolSessionType == "end")
                    {
                        if (CurrentViewModel.LineResponse.StartResponse!=null && CurrentViewModel.LineResponse.StartResponse.Route != null && CurrentViewModel.LineResponse.StartResponse.Decrease != null)//if start point already exists, use its route
                        {
                            CurrentViewModel.LineResponse.EndResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.LineArgs.EndArgs, CurrentViewModel.LineResponse.StartResponse.Route) as PointResponseModel);
                        }
                        else
                        {
                            CurrentViewModel.LineResponse.EndResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.LineArgs.EndArgs) as PointResponseModel);
                            if (CurrentViewModel.LineResponse != null && CurrentViewModel.LineResponse.EndResponse != null && CurrentViewModel.LineResponse.EndResponse.Route != null)
                            {
                                CurrentViewModel.LineResponse.StartResponse.Route = CurrentViewModel.LineResponse.EndResponse.Route;
                            }
                        }
                        lineGeometryResponse = await Utils.MapToolUtils.GetLine(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, CurrentViewModel.LineArgs.StartArgs.SR, CurrentViewModel.LineArgs.StartArgs.ReferenceDate);
                        await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.LineArgs.EndArgs, CurrentViewModel.LineResponse.EndResponse, MapToolSessionType);
                    }
                    else//point session
                    {
                        CurrentViewModel.PointResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.PointArgs) as PointResponseModel);
                        await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.PointArgs, CurrentViewModel.PointResponse, MapToolSessionType);
                    }
                    if (lineGeometryResponse.Count > 0)
                    {
                        await Commands.GraphicsCommands.CreateLineGraphics(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, lineGeometryResponse);
                    }
                }
                
            }
        }
        //e.Handled = true; //Handle the event args to get the call to the corresponding async method
        #region Methods for selecting and delesecting the point creation map tool

        /// <summary>
        /// -   Assigns the viewmodels (one will be null and the other will be populated).
        /// -   Whichever viewmodel is populated will be used to handle the map point that is
        ///     generated by the tool.
        /// -   If the graphics layer doesn't exist yet, create it.
        /// -   Set the active tool in ArcGIS Pro to this map tool.
        /// </summary>
        /// <param name="currentVM"></param>
        /// <param name="sessionType">
        /// The type of point mapping session that should be used.
        /// Options: point, start, or end</param>
        public async Task StartSession(Utils.ViewModelBase currentVM, string sessionType)
        {
            CurrentViewModel = currentVM;
            MapToolSessionType = sessionType;
            //var graphicsLayer = map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
            if (graphicsLayer != null)//if layer exists
            {
                MapView.Active.Map.TargetGraphicsLayer = graphicsLayer;
                await FrameworkApplication.SetCurrentToolAsync("MapAMilepost_MapTool");
                string currentTool = FrameworkApplication.CurrentTool;
                if (FrameworkApplication.CurrentTool != "MapAMilepost_MapTool")
                {
                    await FrameworkApplication.SetCurrentToolAsync("MapAMilepost_MapTool");
                }
            }
            else // else create layer
            {
                string title = Interaction.InputBox("Please enter a title for your milepost graphics layer.", "Create Milepost Layer", "My Milepost Layer");
                if (title != "")
                {
                    CreateLayer(title, MapView.Active.Map);
                    await FrameworkApplication.SetCurrentToolAsync("MapAMilepost_MapTool");
                }
                
            };
        }
        private void CreateLayer(string title, Map map)
        {
            GraphicsLayerCreationParams gl_param = new() { Name = title };
            QueuedTask.Run(() =>
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
        }

        /// <summary>
        /// -   Deactivates the mapping session
        /// -   Set the current tool to whatever was selected before the mapping session was initialized.
        /// </summary>
        public void EndSession()
        {
            OnToolDeactivateAsync(true);    
            FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
        }
        #endregion
    }
}
