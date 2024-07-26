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
        public static List<SOEResponseModel> CastToList(List<SOEResponseModel> SelectedItems, object list)
        {
            System.Collections.IList items = (System.Collections.IList)list;
            var collection = items.Cast<SOEResponseModel>();
            return collection.ToList();
        }

        ///// <summary>
        ///// -   Update the selected items array based on the rows selected in the DataGrid in ResultsView.xaml via data binding.
        ///// </summary>
        public static void UpdateSelection(DataGrid grid, Utils.ViewModelBase VM)
        {
            Commands.GraphicsCommands.DeleteUnsavedGraphics();
            VM.SOEArgs.X = 0;
            VM.SOEArgs.Y = 0;
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
                //clear selected items
                VM.SelectedItems.Clear();
                //clear selected rows
                myGrid.SelectedItems.Clear();
                //clear selected graphics
                Commands.GraphicsCommands.SetPointGraphicsSelected(VM.SelectedItems, VM.SoePointResponses, "point");
            }
            //if a row is clicked, select the row and graphic
            else
            {
                VM.SelectedItems.Clear();
                //clear selected graphics
                Commands.GraphicsCommands.SetPointGraphicsSelected(VM.SelectedItems, VM.SoePointResponses, "point");
                //update selected items
                VM.SelectedItems = CastToList(VM.SelectedItems, myGrid.SelectedItems);
                //update selected graphics
                Commands.GraphicsCommands.SetPointGraphicsSelected(VM.SelectedItems, VM.SoePointResponses, "point");
                if (VM.SelectedItems.Count == 1)
                {
                    VM.SOEResponse = VM.SelectedItems[0];
                }
            }
        }
        public static void DeleteItems(Utils.ViewModelBase VM)
        {
            if (VM.SoePointResponses.Count > 0 && VM.SelectedItems.Count > 0)
            {
                if (MessageBox.Show(
                    $"Are you sure you wish to delete these {VM.SelectedItems.Count} records?",
                    "Delete Rows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes
                )
                {
                    List<int> deleteIndices = new List<int>();
                    for (int i = VM.SoePointResponses.Count - 1; i >= 0; i--)
                    {
                        if (VM.SelectedItems.Contains(VM.SoePointResponses[i]))
                        {
                            VM.SoePointResponses.Remove(VM.SoePointResponses[i]);
                            deleteIndices.Add(i);
                        }
                    }
                    Commands.GraphicsCommands.DeleteGraphics(deleteIndices, "point");
                    if (VM.SoePointResponses.Count == 0)
                    {
                        VM.ShowResultsTable = false;
                    };
                }
            }
        }

        public static void ClearItems(Utils.ViewModelBase VM)
        {
            Commands.GraphicsCommands.DeleteUnsavedGraphics();
            VM.SOEArgs.X = 0;
            VM.SOEArgs.Y = 0;
            if (VM.SoePointResponses.Count > 0)
            {
                if (MessageBox.Show(
                    $"Are you sure you wish to clear all {VM.SoePointResponses.Count} point records?",
                    "Clear Results",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes
                )
                {
                    List<int> deleteIndices = new List<int>();
                    for (int i = VM.SoePointResponses.Count - 1; i >= 0; i--)
                    {
                        deleteIndices.Add(i);
                    }
                    Commands.GraphicsCommands.DeleteGraphics(deleteIndices, "point");
                    VM.SoePointResponses.Clear();
                    VM.SOEResponse = new SOEResponseModel();//clear the SOE response info panel
                }
            }
            if (VM.SoePointResponses.Count == 0)
            {
                VM.ShowResultsTable = false;
            };
        }
    }
}
