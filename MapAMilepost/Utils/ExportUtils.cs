using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
    class ExportUtils
    {
        public static async Task SelectFC()
        {
            var openDlg = new OpenItemDialog
            {
                Title = "Select a Geodatabase",
                InitialLocation = @"C:\Data",
                MultiSelect = false,
                BrowseFilter = BrowseProjectFilter.GetFilter(ArcGIS.Desktop.Catalog.ItemFilters.Geodatabases),
            };

            //show the browse dialog
            bool? ok = openDlg.ShowDialog();
            if (!ok.HasValue || openDlg.Items.Count() == 0)
                return;   //nothing selected

            await QueuedTask.Run(() =>
            {
                // get the item
                var item = openDlg.Items.First();

                // see if the item has a dataset
                if (ItemFactory.Instance.CanGetDataset(item))
                {
                    // get it
                    using (var ds = ItemFactory.Instance.GetDataset(item))
                    {
                        // access some properties
                        var name = ds.GetName();
                        var path = ds.GetPath();
                        var type = ds.GetType();

                        // if it's a featureclass
                        if (ds is ArcGIS.Core.Data.FeatureClass fc)
                        {
                            // create a layer 
                            var featureLayerParams = new FeatureLayerCreationParams(fc)
                            {
                                MapMemberIndex = 0
                            };
                            var layer = LayerFactory.Instance.CreateLayer<FeatureLayer>(featureLayerParams, MapView.Active.Map);

                            // continue
                        }
                    }
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
    }
}
