﻿using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
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
        private static string StartEnd {  get; set; } 
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
                MapToolUtils.MapPoint2RoutePoint(mapPoint, CurrentViewModel, StartEnd);
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
        public void StartSession(Utils.ViewModelBase currentVM, string startEnd)
        {
            CurrentViewModel = currentVM;
            StartEnd = startEnd;
            Map map = MapView.Active.Map;
            var graphicsLayer = map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            if (graphicsLayer != null)//if layer exists
            {
                map.TargetGraphicsLayer = graphicsLayer;
            }
            else // else create layer
            {
                GraphicsLayerCreationParams gl_param = new() { Name = "MilepostMappingLayer" };
                QueuedTask.Run(() =>
                {
                    GraphicsLayer graphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(gl_param, map);
                });
            };
            FrameworkApplication.SetCurrentToolAsync("MapAMilepost_MapTool");
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
