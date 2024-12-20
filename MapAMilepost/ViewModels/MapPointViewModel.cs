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
        private PointResponseModel _pointResponse = new();
        private PointArgsModel _pointArgs = new();
        private bool _isEnabled = false;
        private ObservableCollection<PointResponseModel> _pointResponses = new();
        private bool _showResultsTable = false;
        private bool _sessionActive = false;
        private bool _isMapMode = true;
        private bool _srmpIsSelected = true;
        public MapPointViewModel()//constructor
        {
        }

        public bool isEnabled// whether or not the map point view is enabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(isEnabled));
            }
        }
        public bool SRMPIsSelected
        {
            get { return _srmpIsSelected; }
            set
            {
                _srmpIsSelected = value;
                OnPropertyChanged(nameof(SRMPIsSelected));
            }
        }
        public override bool SessionActive
        {
            get { return _sessionActive; }
            set
            {
                _sessionActive = value;
                OnPropertyChanged(nameof(SessionActive));
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

        public Commands.RelayCommand<object> ChangeModeCommand => new(async (param) =>
        {
            IsMapMode = !IsMapMode;
            await Utils.MapToolUtils.DeactivateSession(this, "point");
            if (!IsMapMode)
            {
                PointResponse = new PointResponseModel() { 
                    Decrease = false, Back = false, Srmp=0
                };
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
        public Commands.RelayCommand<object> ChangeMPTypeCommand => new((val) =>
        {
            if ((string)val == "SRMP")
            {
                SRMPIsSelected = true;
                this.PointResponse.Arm = null;
                this.PointResponse.Srmp = 0;
            }
            else
            {
                SRMPIsSelected = false;
                this.PointResponse.Arm = 0;
                this.PointResponse.Srmp = null;
            }
        });
        public Commands.RelayCommand<object> InteractionCommand => new (async(p) => {
            if (IsMapMode)
            {
                await ToggleSession();
            }
            else
            {
                if (PointArgs.SR == 0)
                {
                    PointArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                }
                PointResponse.ReferenceDate = PointArgs.ReferenceDate;
                bool formDataValid = HTTPRequest.CheckFormData(PointResponse);
                if (formDataValid)
                {

                    LocationInfo formLocation = new LocationInfo(PointResponse);
                    PointResponse = await Utils.HTTPRequest.FindRouteLocation(formLocation, PointArgs) as PointResponseModel;
                    await Commands.GraphicsCommands.CreatePointGraphics(PointArgs, PointResponse, "Point");
                }
            }
        });

        private async Task ToggleSession()
        {
            if (!this.SessionActive)
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
