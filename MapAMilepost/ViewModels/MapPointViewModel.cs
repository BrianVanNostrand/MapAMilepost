using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Commands;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MapAMilepost.ViewModels
{
    public class MapPointViewModel:ViewModelBase
    {
        /// <summary>
        /// Private variables with associated public variables, granting access to the INotifyPropertyChanged command via ViewModelBase.
        /// </summary>
        private PointResponseModel _pointResponse;
        private PointArgsModel _pointArgs;
        private bool _isEnabled;
        private ObservableCollection<PointResponseModel> _pointResponses;
        private bool _showResultsTable = false;
        private MapToolInfo _mapToolInfos;
        private string _tabLabel = "TEST TAB LABEL";
        private bool _isMapMode;
        public MapPointViewModel()//constructor
        {
            _isEnabled = false;
            _isMapMode = true;
            _pointResponse = new PointResponseModel();
            _pointArgs = new PointArgsModel();
            _pointResponses = new ObservableCollection<PointResponseModel>();
            _mapToolInfos = new MapToolInfo {
                SessionActive = false,
                MapButtonLabel = "Start Mapping",
                MapButtonToolTip = "Start mapping session."
            };
        }

        public bool isEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(isEnabled));
            }
        }
        public string TabLabel
        {
            get { return _tabLabel; }
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
        public override PointResponseModel PointResponse
        {
            get { return _pointResponse; }
            set { _pointResponse = value; OnPropertyChanged(nameof(PointResponse));}
        }

        /// <summary>
        /// -   Arguments passed to the SOE HTTP query.
        /// </summary>
        public override PointArgsModel PointArgs
        {
            get { return _pointArgs; }
            set { _pointArgs = value; OnPropertyChanged(nameof(PointArgs)); }
        }

        /// <summary>
        /// -   Array of saved PointResponse data objects.
        /// </summary>
        public override ObservableCollection<PointResponseModel> PointResponses
        {
            get { return _pointResponses; }
            set { _pointResponses = value; OnPropertyChanged(nameof(PointResponses)); }
        }

        /// <summary>
        /// -   Indicates whether selected mode is "map click" or not.
        /// </summary>
        public override bool IsMapMode
        {
            get { return _isMapMode; }
            set { _isMapMode = value; OnPropertyChanged(nameof(IsMapMode)); }
        }

        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public override List<PointResponseModel> SelectedPoints { get; set; } = new List<PointResponseModel>();

        public Commands.RelayCommand<object> UpdateSelectionCommand => new (async(grid) => await Commands.DataGridCommands.UpdatePointSelection(grid as DataGrid, this));

        public Commands.RelayCommand<object> DeleteItemsCommand => new (async(p) => await Commands.DataGridCommands.DeletePointItems(this));

        public Commands.RelayCommand<object> ChangeModeCommand => new((param) =>
        {
            IsMapMode = !IsMapMode;
            if (!IsMapMode)
            {
                PointResponse = new PointResponseModel();
            }
            else
            {
                if (SelectedPoints.Count == 1)
                {
                    PointResponse = SelectedPoints[0];
                }
            }
        });

        public Commands.RelayCommand<object> ClearItemsCommand => new(async (parms) => {
            if (MapView.Active != null && MapView.Active.Map != null)
            {
                await Commands.DataGridCommands.ClearDataGridItems(this);
            }
            else
            {
                MessageBox.Show("Please switch to a map view before attempting to clear mileposts.");
            }
        });
        public Commands.RelayCommand<object> SavePointResultCommand => new ((grid) => Commands.GraphicsCommands.SavePointResult(grid as DataGrid, this));       

        public Commands.RelayCommand<object> ToggleMapToolSessionCommand => new (async(p) => {
            if (IsMapMode)
            {
                await ToggleSession();
            }
            else
            {
                //Console.WriteLine(PointResponse);
                //CurrentViewModel.LineResponse.StartResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.LineArgs.StartArgs) as PointResponseModel);
                //lineGeometryResponse = await Utils.MapToolUtils.GetLine(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, CurrentViewModel.LineArgs.StartArgs.SR, CurrentViewModel.LineArgs.StartArgs.ReferenceDate);
                //await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.LineArgs.StartArgs, CurrentViewModel.LineResponse.StartResponse, MapToolSessionType);
            }
        });

        private async Task ToggleSession()
        {
            if (!this.MapToolInfos.SessionActive)
            {
                await Utils.MapToolUtils.InitializeSession(this, "point");
            }
            else
            {
                await Utils.MapToolUtils.DeactivateSession(this, "point");
            }
        }
    }
}
