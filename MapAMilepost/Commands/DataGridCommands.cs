using ArcGIS.Desktop.Framework.Contracts;
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
        public static void UpdatePointSelection(DataGrid grid, Utils.ViewModelBase VM)
        {
            Commands.GraphicsCommands.DeleteUnsavedGraphics();
            VM.SoeArgs.X = 0;
            VM.SoeArgs.Y = 0;
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
                VM.SoeResponse = new PointResponseModel();
                //clear selected items
                VM.SelectedPoints.Clear();
                //clear selected rows
                myGrid.SelectedItems.Clear();
                //clear selected graphics
                Commands.GraphicsCommands.SetPointGraphicsSelected(VM.SelectedPoints, VM.SoeResponses, "point");
            }
            //if a row is clicked, select the row and graphic
            else
            {
                VM.SelectedPoints.Clear();
                //clear selected graphics
                Commands.GraphicsCommands.SetPointGraphicsSelected(VM.SelectedPoints, VM.SoeResponses, "point");
                //update selected items
                VM.SelectedPoints = CastPointsToList(myGrid.SelectedItems);
                //update selected graphics
                Commands.GraphicsCommands.SetPointGraphicsSelected(VM.SelectedPoints, VM.SoeResponses, "point");
                if (VM.SelectedPoints.Count == 1)
                {
                    VM.SoeResponse = VM.SelectedPoints[0];
                }
            }
        }

        ///// <summary>
        ///// -   Update the selected items array based on the rows selected in the DataGrid in ResultsView.xaml via data binding.
        ///// </summary>
        public static void UpdateLineSelection(DataGrid grid, Utils.ViewModelBase VM)
        {
            Commands.GraphicsCommands.DeleteUnsavedGraphics();
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
                VM.LineResponse = new LineResponseModel();
                //clear selected items
                VM.SelectedLines.Clear();
                //clear selected rows
                myGrid.SelectedItems.Clear();
                //clear selected graphics
                Commands.GraphicsCommands.SetLineGraphicsSelected(VM.SelectedLines);
            }
            //if a row is clicked, select the row and graphic
            else
            {
                VM.SelectedLines.Clear();
                //clear selected graphics
                Commands.GraphicsCommands.SetLineGraphicsSelected(VM.SelectedLines);
                //update selected items
                VM.SelectedLines = CastLinesToList(myGrid.SelectedItems);
                //update selected graphics
                Commands.GraphicsCommands.SetLineGraphicsSelected(VM.SelectedLines);
                if (VM.SelectedLines.Count == 1)
                {
                    VM.LineResponse = VM.SelectedLines[0];
                }
            }
        }
        public static void DeletePointItems(Utils.ViewModelBase VM)
        {
            if (VM.SoeResponses.Count > 0 && VM.SelectedPoints.Count > 0)
            {
                if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Are you sure you wish to delete these {VM.SelectedPoints.Count} records?",
                    "Delete Rows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes
                )
                {
                    List<int> deleteIndices = new List<int>();
                    for (int i = VM.SoeResponses.Count - 1; i >= 0; i--)
                    {
                        if (VM.SelectedPoints.Contains(VM.SoeResponses[i]))
                        {
                            VM.SoeResponses.Remove(VM.SoeResponses[i]);
                            deleteIndices.Add(i);
                        }
                    }
                    Commands.GraphicsCommands.DeleteGraphics(deleteIndices, "point");
                    if (VM.SoeResponses.Count == 0)
                    {
                        VM.ShowResultsTable = false;
                    };
                    VM.SoeResponse = new PointResponseModel();
                }
            }
        }

        public static async Task DeleteLineItems(Utils.ViewModelBase VM)
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
                    string[] FeatureIDs = VM.SelectedLines.Select(Item => Item.featureID).ToArray();//Selected item IDs
                    foreach (var LineResponse in VM.LineResponses.ToList())
                    {
                        if (FeatureIDs.Contains(LineResponse.featureID))
                        {
                            VM.LineResponses.Remove(LineResponse);
                        }
                    }
                    await Commands.GraphicsCommands.DeleteLineGraphics(FeatureIDs);
                    if (VM.LineResponses.Count == 0)
                    {
                        VM.ShowResultsTable = false;
                    };
                    VM.LineResponse = new LineResponseModel();
                }
            }
        }

        public static void ClearItems(Utils.ViewModelBase VM)
        {
            Commands.GraphicsCommands.DeleteUnsavedGraphics();
            VM.SoeArgs.X = 0;
            VM.SoeArgs.Y = 0;
            if (VM.SoeResponses.Count > 0)
            {
                if (ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Are you sure you wish to clear all {VM.SoeResponses.Count} point records?",
                    "Clear Results",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes
                )
                {
                    List<int> deleteIndices = new List<int>();
                    for (int i = VM.SoeResponses.Count - 1; i >= 0; i--)
                    {
                        deleteIndices.Add(i);
                    }
                    Commands.GraphicsCommands.DeleteGraphics(deleteIndices, "point");
                    VM.SoeResponses.Clear();
                    VM.SoeResponse = new PointResponseModel();//clear the SOE response info panel
                }
            }
            if (VM.SoeResponses.Count == 0)
            {
                VM.ShowResultsTable = false;
            };
        }
    }
}
