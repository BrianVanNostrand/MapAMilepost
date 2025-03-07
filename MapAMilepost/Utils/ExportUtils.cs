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
        public string PType { get; set; }
        public FeatureClassInfo(string title, string gdbPath, string pType) {
            this.GDBPath = gdbPath;
            this.FCTitle = title;
            this.PType = pType;
        }
    }
    class ExportUtils
    {
        public static async Task<FeatureClassInfo> CreateFC(string type, long SR)
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
                string title = Regex.Replace(Interaction.InputBox($"Please enter a title for your {type} feature class.", "Create Feature Class", $"Milepost {type}s"), @"[^\w\-_]", "_");
                FCInfo = new(title, openDlg.Items[0].Path.ToString(), type);
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
                            if (featureNames.Contains(title))
                            {
                                if (System.Windows.MessageBox.Show($"A featureclass named {title} already exists in the geodatabase {openDlg.Items.First().Title}. Would you like to overwite the feature class?", "Feature Class Exists", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                {
                                    //##TODO fix this delete to work if layer is in map.
                                    try
                                    {
                                        FeatureClassDefinition targetFC = geodatabase.GetDefinition<FeatureClassDefinition>(title);
                                        FeatureClassDescription DeleteFCDefinition = new FeatureClassDescription(targetFC);
                                        schemaBuilder.Delete(DeleteFCDefinition);
                                        bool deleted = schemaBuilder.Build();
                                        Console.WriteLine("test");
                                    }
                                    catch (Exception e)
                                    {
                                        System.Windows.MessageBox.Show(e.Message, "Failed to delete feature class.");
                                    }
                                }
                            }
                            List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptionList = new List<ArcGIS.Core.Data.DDL.FieldDescription>();
                            foreach (var prop in typeof(PointResponseModel).GetProperties())
                            {
                                System.Type type = prop.PropertyType == typeof(Nullable<>) ? prop.PropertyType.GetEnumUnderlyingType() : prop.PropertyType;
                                string name = prop.Name;
                            }
                            // Create a ShapeDescription object
                            ShapeDescription shapeDescription = new ShapeDescription(GeometryType.Point, SpatialReferenceBuilder.CreateSpatialReference(int.Parse(SR.ToString())));
                            // Get field descriptions
                            List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptions = CreatePointFieldDescriptions();
                            // Create a FeatureClassDescription object to describe the feature class to create
                            FeatureClassDescription featureClassDescription = new FeatureClassDescription(title, fieldDescriptions, shapeDescription);
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
            return FCInfo;
        }
        public static List<ArcGIS.Core.Data.DDL.FieldDescription> CreatePointFieldDescriptions()
        {
           
            List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptionList = new List<ArcGIS.Core.Data.DDL.FieldDescription>() {
                new ArcGIS.Core.Data.DDL.FieldDescription("FeatureID",FieldType.String),
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
        public static async Task PopulateFC(FeatureClassInfo fcInfo)
        {
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
            //FeatureLayer featureLayer = await Utils.MapViewUtils.GetFeatureLayerByTitle(MapView.Active.Map, fcInfo.FCTitle);
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
                        if (item.GetCustomProperty("sessionType") == "point")
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
                                        rowBuffer["FeatureType"] = item.GetCustomProperty("sessionType");
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
                    }
                }
                if (exceptionList.Count==0)
                {
                    if (System.Windows.MessageBox.Show($"Feature class has been created.\n Would you like to add it to your map?", "Feature Class Created", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        try 
                        {
                            //List<CIMUniqueValue> listUniqueValues = new List<CIMUniqueValue> { new CIMUniqueValue { FieldValues = new string[] { "point", "start","end" } } };
                            //CIMUniqueValueClass alabamaUniqueValueClass = new CIMUniqueValueClass
                            //{
                            //    Editable = true,
                            //    Label = "Alabama",
                            //    Patch = PatchShape.Default,
                            //    Symbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.RedRGB).MakeSymbolReference(),
                            //    Visible = true,
                            //    Values = listUniqueValuesAlabama.ToArray()

                            //};
                            //CIMUniqueValueRenderer uvr = new CIMUniqueValueRenderer
                            //{
                            //    UseDefaultSymbol = true,
                            //    DefaultLabel = "all other values",
                            //    DefaultSymbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreyRGB).MakeSymbolReference(),
                            //    Groups = listUniqueValueGroups.ToArray(),
                            //    Fields = new string[] { "STATE_NAME" }
                            //};
                            var flyrCreatnParam = new FeatureLayerCreationParams(new Uri($"{fcInfo.GDBPath}\\{fcInfo.FCTitle}"))
                            {
                                Name = fcInfo.FCTitle,
                                IsVisible = true,
                                RendererDefinition = new UniqueValueRendererDefinition()
                                {
                                    SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol(
                                    CIMColor.CreateRGBColor(0, 0, 255), 8, SimpleMarkerStyle.Circle).MakeSymbolReference()
                                }
                            };
                            var featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(flyrCreatnParam, MapView.Active.Map);
                            featureLayer.SetShowLayerAtAllScales(true);
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
