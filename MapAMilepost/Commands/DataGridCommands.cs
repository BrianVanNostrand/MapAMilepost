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
            if (MapView.Active!=null && MapView.Active.Map!=null)
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
                MessageBox.Show("Please switch to a map view before attempting this selection.");
            }
        }

        public static coordinatePair GetSelectedGraphicInfoPoint(DataGrid grid, Utils.ViewModelBase VM)
        {
            coordinatePair coordPair = null;
            if (MapView.Active != null && MapView.Active.Map != null)
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
        //public static LineResponseModel GetSelectedGraphicInfoLine(DataGrid grid, Utils.ViewModelBase VM)
        //{
        //    LineResponseModel line;
        //    if (MapView.Active != null && MapView.Active.Map != null)
        //    {
        //        var selItems = grid.SelectedItems;
        //        bool dataGridRowSelected = false;
        //        foreach (var item in selItems)
        //        {
        //            DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
        //            if (dgr.IsMouseOver)
        //            {
        //                dataGridRowSelected = true;
        //            }
        //        }
        //        if (dataGridRowSelected)
        //        {
        //            if (VM.SelectedLines != null && VM.SelectedLines.Count > 0)
        //            {
        //                line = VM.SelectedLines.First();
        //            }
        //        }
        //    }
        //    return coordPair;
        //}

        ///// <summary>
        ///// -   Update the selected items array based on the rows selected in the DataGrid in ResultsView.xaml via data binding.
        ///// </summary>
        public static async Task UpdateLineSelection(DataGrid grid, Utils.ViewModelBase VM)
        {
            if (MapView.Active!=null && MapView.Active.Map != null)
            {
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();
                VM.LineArgs = new LineArgsModel(VM.LineArgs.StartArgs.SearchRadius, VM.LineArgs.EndArgs.SearchRadius);
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
                if (dataGridRowSelected == false)
                {
                    //clear the response
                    if (VM.IsMapMode)
                    {
                        VM.LineResponse = new LineResponseModel();
                    }
                    else
                    {
                        VM.LineResponse = new LineResponseModel {
                            StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM),
                            EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM)
                        };
                    }
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
                MessageBox.Show("Please switch to a map view before attempting this selection.");
            }
            
        }
        public static async Task DeletePointItems(Utils.ViewModelBase VM = null)
        {
            if (MapView.Active != null && MapView.Active.Map != null)
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
                        if (VM != null)//if individual points are being deleted
                        {
                            string[] FeatureIDs = VM.SelectedPoints.Select(Item => Item.PointFeatureID).ToArray();//Selected item IDs
                            foreach (var PointResponse in VM.PointResponses.ToList())
                            {
                                if (FeatureIDs.Contains(PointResponse.PointFeatureID))
                                {
                                    VM.PointResponses.Remove(PointResponse);
                                }
                            }
                            await GraphicsCommands.DeleteGraphics("point",FeatureIDs);
                        }
                        else//if all points are being cleared
                        {
                            VM.PointResponses.Clear();
                            await GraphicsCommands.DeleteGraphics("point");
                        }
                        if (VM.PointResponses.Count == 0)
                        {
                            VM.ShowResultsTable = false;
                        };
                        VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                        VM.SelectedPoints.Clear();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please switch to a map view before attempting to delete.");
            }
        }

        public static async Task DeleteLineItems(Utils.ViewModelBase VM = null)
        {
            if (MapView.Active !=null && MapView.Active.Map != null)
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
                        if (VM != null)//if individual lines are being deleted
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
                        }
                        else//if all lines are being cleared
                        {
                            VM.LineResponses.Clear();
                            await Commands.GraphicsCommands.DeleteGraphics("line");
                        }
                        if (VM.LineResponses.Count == 0)
                        {
                            VM.ShowResultsTable = false;
                        };
                        VM.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                        VM.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                        VM.SelectedLines.Clear();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please switch to a map view before attempting to delete.");
            }
        }

        public static async Task ClearDataGridItems(Utils.ViewModelBase VM, bool IgnorePrompt=false)
        {
            if (VM.GetType() == typeof(MapPointViewModel))
            {
                if (VM.PointResponses.Count > 0)
                {
                    ResetClickPointArgs("point", VM);
                    if (IgnorePrompt == false)//if this method was invoked from clicking the clear button
                    {
                        if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            $"Are you sure you wish to clear all {VM.PointResponses.Count} point records?",
                            "Clear Results",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            await Commands.GraphicsCommands.DeleteGraphics("point");
                            VM.PointResponses.Clear();
                            if (VM.IsMapMode)
                            {
                                VM.PointResponse = new PointResponseModel();//clear the SOE response info panel
                            }
                            else
                            {
                                VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);    
                            }
                        }
                    }
                    else//if this map was invoked by a map change event
                    {
                        VM.PointResponses.Clear();
                        VM.PointResponse = new PointResponseModel();//clear the SOE response info panel
                    }

                }
                if (VM.PointResponses.Count == 0)
                {
                    VM.ShowResultsTable = false;
                };
            }
            else
            {
                if (VM.LineResponses.Count > 0)
                {
                    ResetClickPointArgs("line", VM);
                    if (IgnorePrompt == false)//if this method was invoked from clicking the clear button
                    {
                        if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            $"Are you sure you wish to clear all {VM.LineResponses.Count} line records?",
                            "Clear Results",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            await Commands.GraphicsCommands.DeleteGraphics("line");
                            VM.LineResponses.Clear();
                            if (VM.IsMapMode)
                            {
                                VM.LineResponse = new LineResponseModel();//clear the SOE response info panel
                            }
                            else
                            {
                                VM.LineResponse = new LineResponseModel()
                                {
                                    StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM),
                                    EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM)
                                };
                            }
                        }
                    }
                    else//if this map was invoked by a map change event
                    {
                        VM.LineResponses.Clear();
                        VM.LineResponse = new LineResponseModel();//clear the SOE response info panel
                    }
                }
                if (VM.LineResponses.Count == 0)
                {
                    VM.ShowResultsTable = false;
                };
            }
            if (MapView.Active != null)
            {
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();
            }
        }
        
        private static void ResetClickPointArgs(string sessionType, Utils.ViewModelBase VM)
        {
            if (!string.IsNullOrEmpty(sessionType))
            {
                if(sessionType=="line") 
                {
                    VM.LineArgs.StartArgs.X = 0;
                    VM.LineArgs.StartArgs.Y = 0;
                    VM.LineArgs.EndArgs.X = 0;
                    VM.LineArgs.EndArgs.Y = 0;
                }
                else
                {
                    VM.PointArgs.X = 0;
                    VM.PointArgs.Y = 0;
                }
            }
        }
    }
}
