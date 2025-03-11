using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Catalog.Wizards.CreateFeatureClass;
using ArcGIS.Desktop.Internal.Mapping.Locate;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using Flurl.Util;
using MapAMilepost.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace MapAMilepost.Utils
{
    public class FeatureClassInfo
    {
        public string GDBPath { get; set; }
        public string FCTitle { get; set; }
        public string SType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fcTitle">Title of the feature class</param>
        /// <param name="gdbPath">Path of the geodatabase</param>
        /// <param name="sType">Session Type</param>
        public FeatureClassInfo(string fcTitle, string gdbPath, string sType) {
            this.GDBPath = gdbPath;
            this.FCTitle = fcTitle;
            this.SType = sType;
        }
    }
    class ExportUtils
    {
        public static async Task<FeatureClassInfo> CreatePointFC(string type, long SR)
        {
            FeatureClassInfo FCInfo = null;
            var openDlg = new OpenItemDialog
            {
                Title = "Select a Geodatabase",
                InitialLocation = @"C:\Data",
                MultiSelect = false,
                //BrowseFilter = BrowseProjectFilter.GetFilter(ArcGIS.Desktop.Catalog.ItemFilters.Geodatabases),
                Filter = ItemFilters.Geodatabases
            };
            //show the browse dialog
            bool? ok = openDlg.ShowDialog();
            if (ok.HasValue && (openDlg.Items.Count() > 0 && openDlg.Items[0].Path != null))//something was selected
            {
                string title = Regex.Replace(Interaction.InputBox($"Please enter a title for your {(type == "point" ? type : "endpoint")} feature class.", "Create Feature Class", $"Milepost {(type=="point"?type:"Endpoint")}s"), @"[^\w\-_]", "_");
                FCInfo = new(title, openDlg.Items[0].Path.ToString(), type);
                await CreateFC("point", FCInfo, SR);
            }
            return FCInfo;
        }
        public static async Task<FeatureClassInfo> CreateLineFC(string type, long SR, string gdbPath)
        {
            string title = Regex.Replace(Interaction.InputBox($"Please enter a title for your line feature class.", "Create Feature Class", $"Milepost lines"), @"[^\w\-_]", "_");
            FeatureClassInfo FCInfo = new(title, gdbPath, type);
            await CreateFC("line", FCInfo, SR);
            return FCInfo;
        }
        private static async Task CreateFC(string geometryType, FeatureClassInfo FCInfo, long SR)
        {
            await QueuedTask.Run(() =>
            {
                try
                {
                    FileGeodatabaseConnectionPath fileConnection = new FileGeodatabaseConnectionPath(new Uri(FCInfo.GDBPath));
                    using (Geodatabase geodatabase = new Geodatabase(fileConnection))
                    {
                        // Create a SchemaBuilder object
                        SchemaBuilder schemaBuilder = new SchemaBuilder(geodatabase);
                        IReadOnlyList<FeatureClassDefinition> fcs = geodatabase.GetDefinitions<FeatureClassDefinition>();
                        List<string> featureNames = fcs.Select(c => c.GetName()).ToList();
                        if (featureNames.Contains(FCInfo.FCTitle))
                        {
                            if (System.Windows.MessageBox.Show($"A featureclass named {FCInfo.FCTitle} already exists in the geodatabase {FCInfo.GDBPath}. Would you like to overwite the feature class?", "Feature Class Exists", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                //##TODO fix this delete to work if layer is in map.
                                try
                                {
                                    FeatureClassDefinition targetFC = geodatabase.GetDefinition<FeatureClassDefinition>(FCInfo.FCTitle);
                                    FeatureClassDescription DeleteFCDefinition = new FeatureClassDescription(targetFC);
                                    schemaBuilder.Delete(DeleteFCDefinition);
                                    bool deleted = schemaBuilder.Build();
                                }
                                catch (Exception e)
                                {
                                    System.Windows.MessageBox.Show(e.Message, "Failed to delete feature class.");
                                }
                            }
                        }
                        List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptionList = new List<ArcGIS.Core.Data.DDL.FieldDescription>();
                        // Create a ShapeDescription object
                        ShapeDescription shapeDescription = new ShapeDescription((geometryType=="point"?GeometryType.Point:GeometryType.Polyline), SpatialReferenceBuilder.CreateSpatialReference(int.Parse(SR.ToString())));
                        // Get field descriptions
                        List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptions = new();
                        if (geometryType == "point") { 
                            fieldDescriptions = CreatePointFieldDescriptions(FCInfo.SType); 
                        }
                        else { 
                            fieldDescriptions = CreateLineFieldDescriptions(FCInfo.SType); 
                        }
                        // Create a FeatureClassDescription object to describe the feature class to create
                        FeatureClassDescription featureClassDescription = new FeatureClassDescription(FCInfo.FCTitle, fieldDescriptions, shapeDescription);
                        // Add the creation of the feature class to our list of DDL tasks
                        schemaBuilder.Create(featureClassDescription);
                        // Execute the DDL
                        bool success = schemaBuilder.Build();
                        //Inspect error messages
                        if (!success)
                        {
                            IReadOnlyList<string> errorMessages = schemaBuilder.ErrorMessages;
                            System.Windows.MessageBox.Show("Failed to create feature class.");
                        }
                    }
                }
                catch (GeodatabaseNotFoundOrOpenedException exception)
                {
                    System.Windows.MessageBox.Show(exception.Message);
                }
            });
        }
        public static List<ArcGIS.Core.Data.DDL.FieldDescription> CreatePointFieldDescriptions(string type)
        {
            List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptionList = new List<ArcGIS.Core.Data.DDL.FieldDescription>() {
                new ArcGIS.Core.Data.DDL.FieldDescription("FeatureID",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("FeatureType",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("Arm",FieldType.Double),
                new ArcGIS.Core.Data.DDL.FieldDescription("Srmp",FieldType.Double),
                new ArcGIS.Core.Data.DDL.FieldDescription("Back",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("Decrease",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("RouteId",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("ReferenceDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("ResponseDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("RealignmentDate",FieldType.DateOnly),
            };
            return fieldDescriptionList;
        }
        public static List<ArcGIS.Core.Data.DDL.FieldDescription> CreateLineFieldDescriptions(string type)
        {
            List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptionList = new List<ArcGIS.Core.Data.DDL.FieldDescription>() {
                new ArcGIS.Core.Data.DDL.FieldDescription("FeatureID",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("FeatureType",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("Arm",FieldType.Double),
                new ArcGIS.Core.Data.DDL.FieldDescription("EndArm",FieldType.Double),
                new ArcGIS.Core.Data.DDL.FieldDescription("Srmp",FieldType.Double),
                new ArcGIS.Core.Data.DDL.FieldDescription("EndSrmp",FieldType.Double),
                new ArcGIS.Core.Data.DDL.FieldDescription("Back",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("Decrease",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("RouteId",FieldType.String),
                new ArcGIS.Core.Data.DDL.FieldDescription("ReferenceDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("EndReferenceDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("ResponseDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("EndResponseDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("RealignmentDate",FieldType.DateOnly),
                new ArcGIS.Core.Data.DDL.FieldDescription("EndRealignmentDate",FieldType.DateOnly),
            };
            return fieldDescriptionList;
        }
        public static async Task PopulateFC(FeatureClassInfo fcInfo,string sessionType, string geoType)
        {
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
            IEnumerable<GraphicElement> graphicItems = graphicsLayer.GetElementsAsFlattenedList();
            await QueuedTask.Run(() =>
            {
                FileGeodatabaseConnectionPath fileConnection = new FileGeodatabaseConnectionPath(new Uri(fcInfo.GDBPath));
                List<Exception> exceptionList = new();
                using (Geodatabase geodatabase = new Geodatabase(fileConnection))
                {
                    FeatureClass targetFC = geodatabase.OpenDataset<FeatureClass>(fcInfo.FCTitle);
                    foreach (GraphicElement item in graphicItems)
                    {
                        //if the session type (point or line) matches
                        //the custom property describing the session
                        //type the graphic was originally mapped from
                        //AND the geometry of the feature is point
                        if (item.GetCustomProperty("sessionType") == sessionType && item.GetGeometry().GeometryType == GeometryType.Point && geoType=="point")
                        {
                            var cimGraphic = item.GetGraphic() as CIMPointGraphic;
                            FeatureClassDefinition MPPointDefinition = targetFC.GetDefinition();
                            try
                            {
                                geodatabase.ApplyEdits(() =>
                                {
                                    using (RowBuffer rowBuffer = targetFC.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer["FeatureID"] = item.GetCustomProperty("FeatureID");
                                        rowBuffer["FeatureType"] = item.GetCustomProperty("StartEnd");
                                        rowBuffer["Arm"] = Convert.ToDouble(item.GetCustomProperty("Arm"));
                                        rowBuffer["Srmp"] = Convert.ToDouble(item.GetCustomProperty("Srmp"));
                                        rowBuffer["Back"] = item.GetCustomProperty("Back");
                                        rowBuffer["Decrease"] = item.GetCustomProperty("Decrease");
                                        rowBuffer["RouteId"] = item.GetCustomProperty("Route");
                                        rowBuffer["ReferenceDate"] = item.GetCustomProperty("ReferenceDate");
                                        rowBuffer["ResponseDate"] = item.GetCustomProperty("ResponseDate");
                                        rowBuffer["RealignmentDate"] = item.GetCustomProperty("RealignmentDate");
                                        rowBuffer[MPPointDefinition.GetShapeField()] = MapPointBuilderEx.CreateMapPoint(x: cimGraphic.Location.X, y: cimGraphic.Location.Y, cimGraphic.Location.SpatialReference);
                                        targetFC.CreateRow(rowBuffer);
                                    };
                                });
                            }
                            catch(Exception ex)
                            {
                                exceptionList.Add(ex);
                            }
                        }
                        else
                        {
                            //if the session type (point or line) matches
                            //the custom property describing the session
                            //type the graphic was originally mapped from
                            //AND the geometry of the feature is polyline
                            if (item.GetCustomProperty("sessionType") == sessionType && item.GetGeometry().GeometryType == GeometryType.Polyline && geoType=="line")
                            {
                                var cimGraphic = item.GetGraphic() as CIMLineGraphic;
                                FeatureClassDefinition MPPointDefinition = targetFC.GetDefinition();
                                try
                                {
                                    List<Coordinate2D> plCoords = new List<Coordinate2D>();
                                    ICollection<ArcGIS.Core.Geometry.Segment> collection = new List<ArcGIS.Core.Geometry.Segment>();
                                    cimGraphic.Line.GetAllSegments(ref collection);
                                    IList<ArcGIS.Core.Geometry.Segment> iList = collection as IList<ArcGIS.Core.Geometry.Segment>;
                                    for (int i = 0; i < collection.Count; i++)
                                    {
                                        plCoords.Add(iList[i].StartCoordinate);
                                        if (i == collection.Count - 1)
                                        {
                                            plCoords.Add(iList[i].EndCoordinate);
                                        }
                                    }
                                    geodatabase.ApplyEdits(() =>
                                    {
                                        using (RowBuffer rowBuffer = targetFC.CreateRowBuffer())
                                        {
                                            // Either the field index or the field name can be used in the indexer.
                                            rowBuffer["FeatureID"] = item.GetCustomProperty("FeatureID");
                                            rowBuffer["FeatureType"] = item.GetCustomProperty("StartEnd");
                                            rowBuffer["Arm"] = Convert.ToDouble(item.GetCustomProperty("Arm"));
                                            rowBuffer["EndArm"] = Convert.ToDouble(item.GetCustomProperty("EndArm"));
                                            rowBuffer["Srmp"] = Convert.ToDouble(item.GetCustomProperty("Srmp"));
                                            rowBuffer["EndSrmp"] = Convert.ToDouble(item.GetCustomProperty("EndSrmp"));
                                            rowBuffer["Back"] = item.GetCustomProperty("Back");
                                            rowBuffer["Decrease"] = item.GetCustomProperty("Decrease");
                                            rowBuffer["RouteId"] = item.GetCustomProperty("Route");
                                            rowBuffer["ReferenceDate"] = item.GetCustomProperty("ReferenceDate");
                                            rowBuffer["ReferenceDate"] = item.GetCustomProperty("EndReferenceDate");
                                            rowBuffer["ResponseDate"] = item.GetCustomProperty("ResponseDate");
                                            rowBuffer["ResponseDate"] = item.GetCustomProperty("EndResponseDate");
                                            rowBuffer["RealignmentDate"] = item.GetCustomProperty("RealignmentDate");
                                            rowBuffer["RealignmentDate"] = item.GetCustomProperty("EndRealignmentDate");
                                            rowBuffer[MPPointDefinition.GetShapeField()] = PolylineBuilderEx.CreatePolyline(coordinates:plCoords, spatialReference: cimGraphic.Line.SpatialReference);
                                            targetFC.CreateRow(rowBuffer);
                                        };
                                    });
                                }
                                catch (Exception ex)
                                {
                                    exceptionList.Add(ex);
                                }
                            }
                        }
                    }
                }
                if (exceptionList.Count==0)
                {
                    if (System.Windows.MessageBox.Show($"Feature class {fcInfo.FCTitle} has been created.\n Would you like to add it to your map?", "Feature Class Created", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        try 
                        {
                            for (int i = MapView.Active.Map.Layers.Count() - 1; i >= 0; i--)
                            {
                                if (MapView.Active.Map.Layers[i].GetPath() == new Uri(fcInfo.GDBPath + "\\" + fcInfo.FCTitle))//if map has a layer with the same datasource as the feature class overwitten, remove it and add the new layer.
                                {
                                    MapView.Active.Map.RemoveLayer(MapView.Active.Map.Layers[i]);
                                }
                            }
                            using (Geodatabase geodatabase = new Geodatabase(fileConnection))
                            {
                                var flyrCreatnParam = new FeatureLayerCreationParams(new Uri($"{fcInfo.GDBPath}\\{fcInfo.FCTitle}"))
                                {
                                    Name = fcInfo.FCTitle,
                                    IsVisible = true
                                };
                                FeatureClass targetFC = geodatabase.OpenDataset<FeatureClass>(fcInfo.FCTitle);
                                if (targetFC.GetDefinition().GetShapeType() == GeometryType.Point)
                                {
                                    flyrCreatnParam.RendererDefinition = fcInfo.SType == "point" ? new SimpleRendererDefinition()
                                    {
                                        SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol(
                                        CIMColor.CreateRGBColor(0, 0, 255), 8, SimpleMarkerStyle.Circle).MakeSymbolReference()
                                    } : null;
                                    var featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(flyrCreatnParam, MapView.Active.Map);
                                    if (fcInfo.SType != "point")
                                    {
                                        featureLayer.SetRenderer(Commands.GraphicsCommands.GetEndpointRenderer());
                                    }
                                    featureLayer.SetShowLayerAtAllScales(true);
                                }
                                else
                                {
                                    flyrCreatnParam.RendererDefinition = new SimpleRendererDefinition()
                                    {
                                        SymbolTemplate = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.BlueRGB, 2, SimpleLineStyle.Solid).MakeSymbolReference()
                                    };
                                    var featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(flyrCreatnParam, MapView.Active.Map);
                                    featureLayer.SetShowLayerAtAllScales(true);
                                }
                            }
                            MapView.Active.Redraw(true);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Failed to add feature class to map. You can find your feature class at {fcInfo.GDBPath}\\{fcInfo.FCTitle}.\n {ex.Message}", "Failed To Create Map Layer");
                        }
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show($"Failed to create feature class.\n {exceptionList[0].Message}","Feature Class Creation Failed");
                }
            });
            
        }
    }
}
