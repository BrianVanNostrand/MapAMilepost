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
using ArcGIS.Desktop.Framework;
using ActiproSoftware.Windows.Controls.DataGrid;

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
        private string _mapButtonLabel = "Start Mapping";
        private bool _showResultsTable = false;
        private MapToolInfo _mapToolInfos;
        public MapPointViewModel()//constructor
        {
            _soeResponse = new SOEResponseModel();
            _soeArgs = new SOEArgsModel();
            _soePointResponses = new ObservableCollection<SOEResponseModel>();
            _mapToolInfos = new MapToolInfo {
                SessionActive = false,
                MapButtonLabel = "Start Mapping",
                MapButtonToolTip = "Start mapping session.",
                VM = this,
                MappingTool = new MapAMilepost.MapAMilepostMaptool(this),
            };
        }

        public override MapToolInfo MapToolInfos
        {
            get { return _mapToolInfos; }
            set
            {
                _mapToolInfos = value;
                OnPropertyChanged(nameof(MapToolInfos));
            }
        }

        public override string MapButtonLabel
        {
            get { return _mapButtonLabel; }
            set
            {
                _mapButtonLabel = value;
                OnPropertyChanged("MapButtonLabel");
            }
        }

        public override bool ShowResultsTable
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
        public override SOEResponseModel SOEResponse
        {
            get { return _soeResponse; }
            set { _soeResponse = value; OnPropertyChanged("SOEResponse"); }
        }

        /// <summary>
        /// -   Arguments passed to the SOE HTTP query.
        /// </summary>
        public override SOEArgsModel SOEArgs
        {
            get { return _soeArgs; }
            set { _soeArgs = value; OnPropertyChanged("SOEArgs"); }
        }

        /// <summary>
        /// -   Array of saved SOEResponse data objects.
        /// </summary>
        public override ObservableCollection<SOEResponseModel> SoePointResponses
        {
            get { return _soePointResponses; }
            set { _soePointResponses = value; OnPropertyChanged("SOEPointResponses"); }
        }

        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public override List<SOEResponseModel> SelectedItems { get; set; } = new List<SOEResponseModel>();
   
        public Commands.RelayCommand<object> UpdateSelectionCommand => new Commands.RelayCommand<object>((grid) => Commands.DataGridCommands.UpdateSelection(grid as DataGrid, this));

        public Commands.RelayCommand<object> DeleteItemsCommand => new Commands.RelayCommand<object>((parms) => Commands.DataGridCommands.DeleteItems(this));
        
        public Commands.RelayCommand<object> ClearItemsCommand => new Commands.RelayCommand<object>((parms) => Commands.DataGridCommands.ClearItems(this));

        public Commands.RelayCommand<object> SavePointResultCommand => new Commands.RelayCommand<object>((grid) => Commands.GraphicsCommands.SavePointResult(grid as DataGrid, this));       

        public Commands.RelayCommand<object> ToggleMapToolSessionCommand => new Commands.RelayCommand<object>((parms) => { 
            if (!this.MapToolInfos.SessionActive) {
                Utils.MapToolUtils.InitializeSession(this);
            }
            else{
                Utils.MapToolUtils.DeactivateSession(this);
            }
        });

       

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
        public override async void MapPoint2RoutePoint(object mapPoint)
        {
            SOEResponse = new SOEResponseModel();
            if ((MapPoint)mapPoint != null)
            {
                SOEArgs.X = ((MapPoint)mapPoint).X;
                SOEArgs.Y = ((MapPoint)mapPoint).Y;
                SOEArgs.SR = ((MapPoint)mapPoint).SpatialReference.Wkid;
            }
            object response = await Utils.HTTPRequest.QuerySOE(SOEArgs);
            if (response!=null)
            {
                CustomGraphics customPointSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
                SOEResponseUtils.CopyProperties(response, SOEResponse);
                await QueuedTask.Run(() =>
                {
                    Commands.GraphicsCommands.DeleteUnsavedGraphics();
                    Commands.GraphicsCommands.CreateClickRoutePointGraphics(SOEArgs, SOEResponse, customPointSymbols);
                });
            }
        }

    }
}
