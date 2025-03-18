using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Models;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using static MapAMilepost.Models.PointResponseModel;

namespace MapAMilepost.Commands
{
    class DataGridCommands
    {
        public static List<PointResponseModel> CastPointsToList(object list)
        {
            System.Collections.IList items = (System.Collections.IList)list;
            var collection = items.Cast<PointResponseModel>();
            return collection.ToList();
        }
        public static List<LineResponseModel> CastLinesToList(object list)
        {
            System.Collections.IList items = (System.Collections.IList)list;
            var collection = items.Cast<LineResponseModel>();
            return collection.ToList();
        }
        ///// <summary>
        ///// -   Update the selected items array based on the rows selected in the DataGrid in ResultsView.xaml via data binding.
        ///// </summary>
        public static async Task UpdatePointSelection(DataGrid grid, Utils.ViewModelBase VM)
        {
            if (Utils.UIUtils.MapViewActive())
            {
                await GraphicsCommands.DeleteUnsavedGraphics();
                VM.PointArgs.X = 0;
                VM.PointArgs.Y = 0;
                var selItems = grid.SelectedItems;
                bool dataGridRowSelected = false;
                foreach (var item in selItems)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (dgr.IsMouseOver)
                    {
                        dataGridRowSelected = true;
                    }
                }
                //if no row is clicked, clear the selection
                if (dataGridRowSelected == false)
                {
                    //clear the response
                    if (VM.IsMapMode)
                    {
                        VM.PointResponse = new PointResponseModel();
                    }
                    else
                    {
                        VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                    }
                    //clear selected items
                    VM.SelectedPoints.Clear();
                    //clear selected rows
                    grid.SelectedItems.Clear();
                    //clear selected graphics
                    GraphicsCommands.SetPointGraphicsSelected(VM.SelectedPoints, VM.PointResponses, "point");
                }
                //if a row is clicked, select the row and graphic
                else
                {
                    VM.SelectedPoints.Clear();
                    //clear selected graphics
                    GraphicsCommands.SetPointGraphicsSelected(VM.SelectedPoints, VM.PointResponses, "point");
                    //update selected items
                    VM.SelectedPoints = CastPointsToList(grid.SelectedItems);
                    //update selected graphics
                    GraphicsCommands.SetPointGraphicsSelected(VM.SelectedPoints, VM.PointResponses, "point");
                    if (VM.SelectedPoints.Count == 1 && VM.IsMapMode == true)
                    {
                        VM.PointResponse = VM.SelectedPoints[0];
                    }
                }
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please switch to a map view before attempting this selection.");
            }
        }

        /// <summary>
        /// Used to zoom the map to the graphic represented by a row in the datagrid, when that row is double clicked.
        /// On double click of the datagrid:
        /// - Check that a data grid row has been selected
        /// - If so, return the geometry of the selected record
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="VM"></param>
        /// <returns></returns>
        public static coordinatePair GetSelectedGraphicInfoPoint(DataGrid grid, Utils.ViewModelBase VM)
        {
            coordinatePair coordPair = null;
            if (Utils.UIUtils.MapViewActive())
            {
                var selItems = grid.SelectedItems;
                bool dataGridRowSelected = false;
                foreach (var item in selItems)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (dgr.IsMouseOver)
                    {
                        dataGridRowSelected = true;
                    }
                }
                if(dataGridRowSelected)
                {
                    if(VM.SelectedPoints != null && VM.SelectedPoints.Count>0) 
                    {
                        coordPair = VM.SelectedPoints.First().RouteGeometry;
                    }
                }
            }
            return coordPair;
        }

        /// <summary>
        /// Used to select a line feature on the map when its datagrid row is clicked.
        /// On single click of a datagrid row of the line view:
        /// - Check that an active mapview exists and has a map in it
        /// - Delete unsaved graphics in the milepost graphics layer
        /// - Check that a datagrid row was clicked on
        ///     - If not
        ///         - Reset the start and end point response objects for the line response object conditionally, depending on the mode the tool is in (map or form)
        ///         - Clear the selected lines array in the VM.
        ///         - Clear the datagrid selection, removing the highlight from the rows.
        ///         - Use SetLineGraphics with the empty SelectedLines array to reset the symbology of the line graphics and their endpoints to the default.
        ///     - If so
        ///         - clear the selected lines array
        ///         - Use SetLineGraphics with the empty SelectedLines array to reset the symbology of the line graphics and their endpoints to the default.
        ///         - Create a list of LineResponseModels from the datagrid's multiselector object
        ///         - Update the graphic symbology for the selected lines and add the response objects from the datagrid to the selected lines array in the viewmodel
        ///         - If only one line is selected, clone its properties from the selectedItems array to the LineResponse object's start and end points, so its properties show up in the "Map Mode" properties text box grid.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="VM"></param>
        /// <returns></returns>
        public static async Task UpdateLineSelection(DataGrid grid, Utils.ViewModelBase VM)
        {
            if (Utils.UIUtils.MapViewActive())
            {
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();
                //VM.LineArgs = new LineArgsModel(VM.LineArgs.StartArgs.SearchRadius, VM.LineArgs.EndArgs.SearchRadius);
                DataGrid myGrid = grid as DataGrid;
                var selItems = myGrid.SelectedItems;
                bool dataGridRowSelected = false;
                foreach (var item in selItems)
                {
                    DataGridRow dgr = myGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (dgr.IsMouseOver)
                    {
                        dataGridRowSelected = true;
                    }
                }
                //if no row is clicked, clear the selection
                if (!dataGridRowSelected)
                {
                    VM.LineResponse = new LineResponseModel {
                        StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM),
                        EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM)
                    };
                    //clear selected items
                    VM.SelectedLines.Clear();
                    //clear selected rows
                    myGrid.SelectedItems.Clear();
                    //clear selected graphics
                    GraphicsCommands.SetLineGraphicsSelected(VM.SelectedLines);
                }
                //if a row is clicked, select the row and graphic
                else
                {
                    VM.SelectedLines.Clear();
                    //clear selected graphics
                    GraphicsCommands.SetLineGraphicsSelected(VM.SelectedLines);
                    //update selected items
                    VM.SelectedLines = CastLinesToList(myGrid.SelectedItems);
                    //update selected graphics
                    GraphicsCommands.SetLineGraphicsSelected(VM.SelectedLines);
                    if (VM.SelectedLines.Count == 1 && VM.IsMapMode == true)
                    {
                        VM.LineResponse.StartResponse = VM.SelectedLines[0].StartResponse;
                        VM.LineResponse.EndResponse = VM.SelectedLines[0].EndResponse;
                    }
                } 
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please switch to a map view before attempting this selection.");
            }
        }

        /// <summary>
        /// On click of the "Delete" button in the map point viewmodel:
        /// - If there is an active map and mapview:
        ///     - 
        /// </summary>
        /// <param name="VM"></param>
        /// <returns></returns>
        public static async Task DeletePointItems(Utils.ViewModelBase VM = null)
        {
            if (Utils.UIUtils.MapViewActive())
            {
                if (VM.PointResponses.Count > 0 && VM.SelectedPoints.Count > 0)
                {
                    if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        $"Are you sure you wish to delete these {VM.SelectedPoints.Count} records?",
                        "Delete Rows",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes
                    )
                    {
                        string[] FeatureIDs = VM.SelectedPoints.Select(Item => Item.PointFeatureID).ToArray();//Selected item IDs
                        foreach (var PointResponse in VM.PointResponses.ToList())
                        {
                            if (FeatureIDs.Contains(PointResponse.PointFeatureID))
                            {
                                VM.PointResponses.Remove(PointResponse);
                            }
                        }
                        await GraphicsCommands.DeleteGraphics("point", FeatureIDs);
                        if (VM.PointResponses.Count == 0)
                        {
                            VM.ShowResultsTable = false;
                        };
                        VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                        VM.SelectedPoints.Clear();
                    }
                }
                else
                {
                    if (VM.PointResponses.Count > 0)
                    {
                        if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            $"Are you sure you wish to delete all {VM.PointResponses.Count} records?",
                            "Delete Rows",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            VM.PointResponses.Clear();
                            await GraphicsCommands.DeleteGraphics("point");
                            VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                            VM.ShowResultsTable = false;
                        }
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            $"No saved points found.",
                            "No Saved Points");
                    }
                }
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please switch to a map view before attempting to delete.");
            }
        }

        public static async Task DeleteLineItems(Utils.ViewModelBase VM = null)
        {
            if (Utils.UIUtils.MapViewActive())
            {
                if (VM.LineResponses.Count > 0 && VM.SelectedLines.Count > 0)
                {
                    if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        $"Are you sure you wish to delete these {VM.SelectedLines.Count} records?",
                        "Delete Rows",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes
                    )
                    {
                        string[] FeatureIDs = VM.SelectedLines.Select(Item => Item.LineFeatureID).ToArray();//Selected item IDs
                        foreach (var LineResponse in VM.LineResponses.ToList())
                        {
                            if (FeatureIDs.Contains(LineResponse.LineFeatureID))
                            {
                                VM.LineResponses.Remove(LineResponse);
                            }
                        }
                        await GraphicsCommands.DeleteGraphics("line",FeatureIDs);
                        if (VM.LineResponses.Count == 0)
                        {
                            VM.ShowResultsTable = false;
                        };
                        VM.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                        VM.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                        VM.SelectedLines.Clear();
                    }
                }
                else
                {
                    if (VM.LineResponses.Count > 0)
                    {
                        if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            $"Are you sure you wish to delete all {VM.LineResponses.Count} records?",
                            "Delete Rows",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            VM.LineResponses.Clear();
                            await GraphicsCommands.DeleteGraphics("line");
                            VM.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                            VM.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                            VM.ShowResultsTable = false;
                        }
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            $"No saved lines found.",
                            "No Saved Lines");
                    }
                }
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please switch to a map view before attempting to delete.");
            }
        }
    }
}
