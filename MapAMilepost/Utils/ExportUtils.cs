using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
    class ExportUtils
    {
        public static async Task CreateFC(string type, long SR)
        {
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
            if (!ok.HasValue || openDlg.Items.Count() == 0)
                return;   //nothing selected
            string title = Regex.Replace(Interaction.InputBox($"Please enter a title for your {type} feature class.", "Create Feature Class", $"Milepost {type}s"), @"[^\w\-_]","_");
           
            await QueuedTask.Run(() =>
            {
                try
                {
                    FileGeodatabaseConnectionPath fileConnection = new FileGeodatabaseConnectionPath(new Uri(openDlg.Items.First().Path));
                    using (Geodatabase geodatabase = new Geodatabase(fileConnection))
                    {
                        IReadOnlyList<FeatureClassDefinition> fcs = geodatabase.GetDefinitions<FeatureClassDefinition>();
                        List<string> features = fcs.Select(c => c.GetName()).ToList();
                        if (fcs.Select(c => c.GetName()).ToList().Contains(title))
                        {
                            MessageBox.Show($"Feature class named {title} already exists in geodatabase {openDlg.Title}");
                            return;
                        }           
                        ArcGIS.Core.Data.DDL.FieldDescription globalIDFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription.CreateGlobalIDField();

                        // This static helper routine creates a FieldDescription for an ObjectID field with default values
                        ArcGIS.Core.Data.DDL.FieldDescription objectIDFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription.CreateObjectIDField();

                        // This static helper routine creates a FieldDescription for a string field
                        ArcGIS.Core.Data.DDL.FieldDescription nameFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription.CreateStringField("Name", 255);

                        // This static helper routine creates a FieldDescription for an integer field
                        ArcGIS.Core.Data.DDL.FieldDescription populationFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription.CreateIntegerField("Population");

                        // Assemble a list of all of our field descriptions
                        List<ArcGIS.Core.Data.DDL.FieldDescription> fieldDescriptions = new List<ArcGIS.Core.Data.DDL.FieldDescription>()
                        { globalIDFieldDescription, objectIDFieldDescription, nameFieldDescription, populationFieldDescription };

                        // Create a ShapeDescription object
                        ShapeDescription shapeDescription = new ShapeDescription(GeometryType.Point, SpatialReferenceBuilder.CreateSpatialReference(int.Parse(SR.ToString())));

                        // Create a FeatureClassDescription object to describe the feature class to create
                        FeatureClassDescription featureClassDescription =
                          new FeatureClassDescription(title, fieldDescriptions, shapeDescription);

                        // Create a SchemaBuilder object
                        SchemaBuilder schemaBuilder = new SchemaBuilder(geodatabase);

                        // Add the creation of the Cities feature class to our list of DDL tasks
                        schemaBuilder.Create(featureClassDescription);

                        // Execute the DDL
                        bool success = schemaBuilder.Build();

                        // Inspect error messages
                        if (!success)
                        {
                            IReadOnlyList<string> errorMessages = schemaBuilder.ErrorMessages;
                            MessageBox.Show("Failed to create feature class.");
                        }
                        else
                        {
                            if (MapView.Active!=null && MapView.Active.Map != null)
                            {
                                using (FeatureClass addFeatureClass = geodatabase.OpenDataset<FeatureClass>(title))
                                {
                                    Uri fcURI = new Uri(Uri.EscapeDataString($"{geodatabase.GetPath()}/{title}"));
                                    LayerFactory.Instance.CreateLayer(fcURI, MapView.Active.Map, 0, title);
                                }
                            }
                        }
                    }
                }
                catch (GeodatabaseNotFoundOrOpenedException exception)
                {
                    MessageBox.Show(exception.Message);
                }
            });
        }
        public static async Task SaveFC()
        {
            SaveItemDialog saveLayerFileDialog = new SaveItemDialog()
            {
                Title = "Save Layer File",
                InitialLocation = @"C:\Data\ProLayers\Geographic\Streets",
                Filter = ItemFilters.Files_All
            };
            //show the browse dialog
            bool? ok = saveLayerFileDialog.ShowDialog();

        }
        public static async void CreateFields()
        {
           
        }
    }
}
