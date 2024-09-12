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
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Commands;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using MapAMilepost.ViewModels;
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
                    Commands.GraphicsCommands.DeleteUnsavedGraphics(MapToolSessionType);//delete all unsaved graphics
                    List<List<double>> lineGeometryResponse = new();
                    if (MapToolSessionType == "start")
                    {
                        CurrentViewModel.LineResponse.StartResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint,CurrentViewModel.LineArgs.StartArgs) as PointResponseModel);
                        lineGeometryResponse = await Utils.MapToolUtils.GetLine(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, CurrentViewModel.LineArgs.StartArgs.SR, CurrentViewModel.LineArgs.StartArgs.ReferenceDate);
                        await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.LineArgs.StartArgs, CurrentViewModel.LineResponse.StartResponse, MapToolSessionType);
                    }
                    else if (MapToolSessionType == "end")
                    {
                        CurrentViewModel.LineResponse.EndResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.LineArgs.EndArgs) as PointResponseModel);
                        lineGeometryResponse = await Utils.MapToolUtils.GetLine(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, CurrentViewModel.LineArgs.StartArgs.SR, CurrentViewModel.LineArgs.StartArgs.ReferenceDate);
                        await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.LineArgs.EndArgs, CurrentViewModel.LineResponse.EndResponse, MapToolSessionType);
                    }
                    else
                    {
                        CurrentViewModel.SoeResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.SoeArgs) as PointResponseModel);
                        await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.SoeArgs, CurrentViewModel.SoeResponse, MapToolSessionType);
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
        public async void StartSession(Utils.ViewModelBase currentVM, string sessionType)
        {
            CurrentViewModel = currentVM;
            MapToolSessionType = sessionType;
            Map map = MapView.Active.Map;
            var graphicsLayer = map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            if (graphicsLayer != null)//if layer exists
            {
                map.TargetGraphicsLayer = graphicsLayer;
            }
            else // else create layer
            {
                CreateLayer(map);
            };
            await FrameworkApplication.SetCurrentToolAsync("MapAMilepost_MapTool");
                
        }
        private void CreateLayer(Map map)
        {
            GraphicsLayerCreationParams gl_param = new() { Name = "MilepostMappingLayer" };
            QueuedTask.Run(() =>
            {
                GraphicsLayer graphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(gl_param, map);
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
