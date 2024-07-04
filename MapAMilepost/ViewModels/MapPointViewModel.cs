using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ArcGIS.Desktop.Internal.Reports;

namespace MapAMilepost.ViewModels
{
    public class MapPointViewModel:ViewModelBase
    {
        /// <summary>
        /// Private variables with associated public variables, granting access to the INotifyPropertyChanged command via ViewModelBase.
        /// </summary>
        private SOEResponseModel _soeResponse;
        private SOEArgsModel _soeArgs;
        private ObservableCollection<SOEResponseModel> _soePointResponses;
        private ICommand _savePointResultCommand;
        private ICommand _initializeMapToolSession;
        private bool _isSaved = false;
        private bool _sessionActive = false;
        private MapAMilepostMaptool _pointMapTool;
        private string _mapButtonLabel = "Start Mapping";
        private bool _showResultsTable = false;
        private List<GraphicElement> _selectedGraphics;
        public MapPointViewModel()//constructor
        {
            _soeResponse = new SOEResponseModel();
            _soeArgs = new SOEArgsModel();
            _soePointResponses = new ObservableCollection<SOEResponseModel>();
            PointMapTool = new MapAMilepost.MapAMilepostMaptool();
        }

        /// <summary>
        /// -   The label of the MapPointExecuteButton element in MapPointView.xaml. Used as the content of that element via data binding.
        /// </summary>
        public string MapButtonLabel
        {
            get { return _mapButtonLabel; }
            set
            {
                _mapButtonLabel = value;
                OnPropertyChanged("MapButtonLabel");
            }
        }

        /// <summary>
        /// -   Whether or not the currently maped route point feature already exists in the array of saved SOE responses (SOEPointResponses)
        /// </summary>
        public bool IsSaved
        {
            get { return _isSaved; }
            set
            {
                _isSaved = value;
                OnPropertyChanged(nameof(IsSaved));
            }
        }

        public bool ShowResultsTable
        {
            get { return _showResultsTable; }
            set
            {
                _showResultsTable = value;
                OnPropertyChanged(nameof(ShowResultsTable));
            }
        }

        /// <summary>
        /// -   The SOE response of the currently mapped route point feature.
        /// </summary>
        public SOEResponseModel SOEResponse
        {
            get { return _soeResponse; }
            set { _soeResponse = value; OnPropertyChanged("SOEResponse"); }
        }

        /// <summary>
        /// -   Arguments passed to the SOE HTTP query.
        /// </summary>
        public SOEArgsModel SOEArgs
        {
            get { return _soeArgs; }
            set { _soeArgs = value; OnPropertyChanged("SOEArgs"); }
        }

        /// <summary>
        /// -   Array of saved SOEResponse data objects.
        /// </summary>
        public ObservableCollection<SOEResponseModel> SoePointResponses
        {
            get { return _soePointResponses; }
            set { _soePointResponses = value; OnPropertyChanged("SOEPointResponses"); }
        }

        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public List<SOEResponseModel> SelectedItems { get; set; } = new List<SOEResponseModel>();
        
        /// <summary>
        /// -   Instance of MapTool used to interact with map.
        /// </summary>
        public MapAMilepostMaptool PointMapTool
        {
            get { return _pointMapTool; }
            set { _pointMapTool = value; OnPropertyChanged("MapTool"); }
        }

        /// <summary>
        /// -   Select map graphics using indices of selected records.
        /// </summary>
        public ICommand SelectGraphicsCommand
        {
            get
            {
                return new Commands.RelayCommand(list =>
                {
                    
                    GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                    if (_sessionActive)
                    {
                        InitializeSession(null);
                    }
                    QueuedTask.Run(() =>
                    {
                        var pointSessionGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == "point");
                        foreach (GraphicElement item in pointSessionGraphics)
                        {
                            //remove unsaved graphics
                            if (item.GetCustomProperty("saved") == "false" && item.GetCustomProperty("sessionType") == "point")
                            {
                                graphicsLayer.RemoveElement(item);
                            }
                        }
                        for (int i = 0; i < SoePointResponses.Count-1; i++)
                        {
                            if (SelectedItems.Contains(SoePointResponses[i]))
                            {
                                //update graphic at index
                            }
                        }
                    });
                });
            }
        }

        /// <summary>
        /// -   Update the selected items array based on the rows selected in the DataGrid in ResultsView.xaml via data binding.
        /// </summary>
        public ICommand UpdateSelection
        {
            get
            {
                return new Commands.RelayCommand(grid =>
                {
                    List<DataGridRow> selectedRows = new();
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
                        SelectedItems.Clear();
                        myGrid.SelectedItems.Clear();
                    }
                    else
                    {
                        SelectedItems.Clear();
                        SelectedItems = Commands.DataGridCommands.UpdateSelection(SelectedItems, myGrid.SelectedItems);
                    }
                });
            }
        }

        /// <summary>
        /// -   Delete the selected saved SOEPointResponses array. Accessed by the DeleteItemsButton in ResultsView.xaml via data binding.
        /// </summary>
        public ICommand DeleteItemsCommand
        {
            get
            {
                return new Commands.RelayCommand((list) =>
                {
                    if (SoePointResponses.Count > 0 && SelectedItems.Count > 0)
                    {
                        if (MessageBox.Show(
                            $"Are you sure you wish to delete these {SelectedItems.Count} records?",
                            "Delete Rows",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            List<int> deleteIndices = new List<int>();
                            for (int i = SoePointResponses.Count - 1; i >= 0; i--)
                            {
                                if (SelectedItems.Contains(SoePointResponses[i]))
                                {
                                    SoePointResponses.Remove(SoePointResponses[i]);
                                    deleteIndices.Add(i);
                                }
                            }
                            Commands.GraphicsCommands.DeleteGraphics(deleteIndices, "point");
                            if (SoePointResponses.Count == 0)
                            {
                                ShowResultsTable = false;
                            };
                        }
                    }

                });
            }
        }

        /// <summary>
        /// -   Clear the saved SOEPointResponses array. Accessed by the ClearItemsButton in ResultsView.xaml via data binding.
        /// </summary>
        public ICommand ClearItemsCommand
        {
            get
            {
                return new Commands.RelayCommand(list =>
                {
                    if (SoePointResponses.Count > 0)
                    {
                        if (MessageBox.Show(
                            $"Are you sure you wish to clear all {SoePointResponses.Count} point records?",
                            "Clear Results",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes
                        )
                        {
                            SoePointResponses.Clear();
                        }
                    }
                    if(SoePointResponses.Count == 0)
                    {
                        ShowResultsTable = false;
                    };
                });
            }
        }

        /// <summary>
        /// -   Icommand granting UI access to the SavePointResult method via data binding to the MapPointSaveButton element in the MapPointView.
        /// </summary>
        public ICommand SavePointResultCommand
        {
            get
            {
                if (_savePointResultCommand == null)
                    _savePointResultCommand = new Commands.RelayCommand(SavePointResult,
                        null);
                return _savePointResultCommand;
            }
            set
            {
                _savePointResultCommand = value;
            }
        }

        /// <summary>
        /// -   Icommand granting UI access to the InitializeSession method via data binding to the MapPointExecuteButton element in the MapPointView.
        /// </summary>
        public ICommand InitializeMapToolSession
        {
            get
            {
                if (_initializeMapToolSession == null)
                    _initializeMapToolSession = new Commands.RelayCommand(InitializeSession, null);
                return _initializeMapToolSession;
            }
            set
            {
                _initializeMapToolSession = value;
            }
        }

        /// <summary>
        /// -   Update the SOEArgs that will be passed
        ///     to the HTTP query based on the clicked point on the map.
        /// -   Query the SOE and if the query executes successfully, update the IsSaved public property to determine whether
        ///     or not a dialog box will be displayed, confirming that the user wants to save a duplicate point.
        ///     
        /// ##TODO## 
        /// look at using overlays to display these graphics rather than a graphics layer.
        /// </summary>
        /// <param name="mapPoint"></param>
        public async void Submit(object mapPoint)
        {
           if ((MapPoint)mapPoint != null)
            {
                SOEArgs.X = ((MapPoint)mapPoint).X;
                SOEArgs.Y = ((MapPoint)mapPoint).Y;
                SOEArgs.SR = ((MapPoint)mapPoint).SpatialReference.Wkid;
            }
            Dictionary<string, object> response = await Utils.HTTPRequest.QuerySOE(SOEArgs);
            if ((string)response["message"] == "success")
            {
                IsSaved = false;
                CopyProps.CopyProperties(response["soeResponse"], SOEResponse);
                if (SoePointResponses.Contains(SOEResponse))
                {
                    IsSaved = true;
                }
                else
                {
                    IsSaved = false;
                }
                await QueuedTask.Run(() =>
                {
                    Commands.GraphicsCommands.CreateClickRoutePointGraphics(SOEArgs, SOEResponse);
                });
            }
        }
        /// <summary>
        /// -   Check if the point has already been saved to the saved responses array, and if so,
        ///     present a dialog box to confirm the decision to save a duplicate
        /// -   If it has not already been saved, create new instance of the SOEResponseModel data object,
        ///     duplicating the properties of the target response model, and add the new instance to the 
        ///     saved response model array.
        /// </summary>
        /// <param name="state"></param>
        public void SavePointResult(object state)
        {
            //if a point has been mapped
            if (Utils.CheckObject.HasBeenUpdated(SOEResponse))
            {
                if (SoePointResponses.Contains(SOEResponse))
                {
                    MessageBox.Show("This route location has already been saved.");
                }
                else
                //create a duplicate responsemodel object and add it to the array of response models that will persist
                {
                    SoePointResponses.Add(new SOEResponseModel()
                    {
                        Angle = SOEResponse.Angle,
                        Arm = SOEResponse.Arm,
                        Back = SOEResponse.Back,
                        Decrease = SOEResponse.Decrease,
                        Distance = SOEResponse.Distance,
                        Route = SOEResponse.Route,
                        RouteGeometry = SOEResponse.RouteGeometry,
                        Srmp = SOEResponse.Srmp,
                    });
                    Commands.GraphicsCommands.UpdateSaveGraphicInfos();
                    //Commands.GraphicsCommands.CreateLabel(SOEResponse,SOEArgs);
                    IsSaved = true;
                    if (SoePointResponses.Count > 0)
                    {
                        ShowResultsTable = true;
                    };
                }
            }
            else
            {
                MessageBox.Show("Create a point to save it to the results tab.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// -   Initialize a mapping session (using the setsession method in MapAMilepostMaptool viewmodel)
        /// -   Update the public MapButtonLabel property to reflect the behavior of the MapPointExecuteButton.
        ///     This value is bound to the content of the button as a label.
        /// -   Update the private _setSession property to change the behavior of the method.
        /// </summary>
        /// <param name="state"></param>
        public void InitializeSession(object state)
        {
            if (!_sessionActive)
            {
                _sessionActive = true;
                MapButtonLabel = "Stop Mapping";
                PointMapTool.SetSession(mapPointViewModel: this);
            }
            else
            {
                _sessionActive = false;
                MapButtonLabel = "Start Mapping";
                //  Calls the EndSession method from the MapAMilepostMapTool viewmodel, setting the active tool
                //  to whatever was selected before the mapping session was initialized.
                PointMapTool.EndSession();
            }
        }
    }
}
