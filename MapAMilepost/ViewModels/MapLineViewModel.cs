using MapAMilepost.Models;
using MapAMilepost.Utils;
using System.Windows.Input;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Controls;
using ArcGIS.Desktop.Mapping;
using System;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace MapAMilepost.ViewModels
{
    public class MapLineViewModel:ViewModelBase
    {
        private LineResponseModel _lineResponse = new();
        private LineArgsModel _lineArgs = new();
        private bool _isEnabled = false;
        private ObservableCollection<LineResponseModel> _lineResponses = new();
        private bool _showResultsTable = true;
        private bool _sessionActive = false;
        private bool _sessionEndActive = false;
        private bool _isMapMode = false;
        private bool _srmpIsSelected = true;
        public MapLineViewModel()//constructor
        {
            MappingTool = new MapAMilepostMaptool();
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
        public override bool SessionEndActive
        {
            get { return _sessionEndActive; }
            set
            {
                _sessionEndActive = value;
                OnPropertyChanged(nameof(SessionEndActive));
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

        public override LineResponseModel LineResponse
        {
            get { return _lineResponse; }
            set { 
                _lineResponse = value; 
                OnPropertyChanged(nameof(LineResponse)); 
            }
        }
        public override LineArgsModel LineArgs 
        {
            get { return _lineArgs; }
            set { _lineArgs = value; OnPropertyChanged(nameof(LineArgs)); }
        }
        public override ObservableCollection<LineResponseModel> LineResponses
        {
            get { return _lineResponses; }
            set { _lineResponses = value; OnPropertyChanged(nameof(LineResponses)); }
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
        public override List<LineResponseModel> SelectedLines { get; set; } = new List<LineResponseModel>();

        public Commands.RelayCommand<object> UpdateSelectionCommand => new (async(grid) => await Commands.DataGridCommands.UpdateLineSelection(grid as DataGrid, this));

        public Commands.RelayCommand<object> DeleteItemsCommand => new (async(parms) => await Commands.DataGridCommands.DeleteLineItems(this));

        public Commands.RelayCommand<object> ChangeModeCommand => new(async(param) =>
        {
            IsMapMode = !IsMapMode;
            await Utils.MapToolUtils.DeactivateSession(this, "start");
            await Utils.MapToolUtils.DeactivateSession(this, "end");
            if (!IsMapMode)
            {
                LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this);
                LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this);
            }
            else
            {
                if (SelectedLines.Count == 1)
                {
                    LineResponse.StartResponse = SelectedLines[0].StartResponse;
                    LineResponse.EndResponse = SelectedLines[0].EndResponse;
                }
            };
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

        public Commands.RelayCommand<object> SaveLineResultCommand => new ((grid) => Commands.GraphicsCommands.SaveLineResult(grid as DataGrid, this));
        public Commands.RelayCommand<object> ChangeMPTypeCommand => new((val) =>
        {
            if ((string)val == "SRMP")
            {
                SRMPIsSelected = true;
                this.LineResponse.StartResponse.Arm = null;
                this.LineResponse.StartResponse.Srmp = 0;
            }
            else
            {
                SRMPIsSelected = false;
                this.LineResponse.StartResponse.Arm = 0;
                this.LineResponse.StartResponse.Srmp = null;
            }
        });
        public Commands.RelayCommand<object> InteractionCommand => new (async(MapToolSessionType) =>
        {
            if (IsMapMode)
            {
                await ToggleSession((string)MapToolSessionType);
            }
            else
            {
                List<List<double>> lineGeometryResponse = new();
                await Commands.GraphicsCommands.DeleteUnsavedGraphics((string)MapToolSessionType);//delete unsaved start 
                if ((string)MapToolSessionType == "start")
                {
                    await ProcessPoint(LineResponse.StartResponse, LineArgs.StartArgs, "start");
                    lineGeometryResponse = await Utils.MapToolUtils.GetLine(LineResponse.StartResponse, LineResponse.EndResponse, LineArgs.StartArgs.SR, LineArgs.StartArgs.ReferenceDate);
                }
                else 
                {
                    await ProcessPoint(LineResponse.EndResponse, LineArgs.EndArgs, "end");
                    lineGeometryResponse = await Utils.MapToolUtils.GetLine(LineResponse.StartResponse, LineResponse.EndResponse, LineArgs.StartArgs.SR, LineArgs.StartArgs.ReferenceDate);
                }
            }
        });

        private async Task ProcessPoint(PointResponseModel Point, PointArgsModel Args, string startEnd)
        {
            if (Args.SR == 0)
            {
                Args.SR = MapView.Active.Map.SpatialReference.Wkid;
            }
            Point.ReferenceDate = Args.ReferenceDate;
            var errorDialog = startEnd == "start" ? "Start point" : "End point";
            bool formDataValid = HTTPRequest.CheckFormData(Point, errorDialog);
            if (formDataValid)
            {

                LocationInfo formLocation = new LocationInfo(Point);
                if (formLocation.Route.Length < 3)
                {
                    while (formLocation.Route.Length < 3)
                    {
                        formLocation.Route = "0" + formLocation.Route;
                    }
                }
                if (SRMPIsSelected)
                {
                    formLocation.Arm = null;
                }
                else
                {
                    formLocation.Srmp = null;
                }
                var newPointResponse = await Utils.HTTPRequest.FindRouteLocation(formLocation, Args) as PointResponseModel;
                if (newPointResponse != null && newPointResponse.RouteGeometry != null)
                {
                    Point = newPointResponse;
                    await Commands.GraphicsCommands.CreatePointGraphics(Args, Point, startEnd);
                    await QueuedTask.Run(() =>
                    {
                        Camera newCamera = MapView.Active.Camera;
                        newCamera.X = Point.RouteGeometry.x;
                        newCamera.Y = Point.RouteGeometry.y;
                        MapView.Active.ZoomToAsync(newCamera, TimeSpan.FromSeconds(.5));
                    });
                }
            }
        }

        private async Task ToggleSession(string startEnd)
        {
            if (startEnd == "start")//if start button is clicked
            {
                if (this.SessionEndActive == true || this.SessionActive == false)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "end");
                    await Utils.MapToolUtils.InitializeSession(this, "start");
                }
                else if (this.SessionActive == true)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "start");
                }
            }
            if (startEnd == "end")
            {
                if (this.SessionActive == true || this.SessionEndActive == false)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "start");
                    await Utils.MapToolUtils.InitializeSession(this, "end");
                }
                else if (this.SessionEndActive == true)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "end");
                }
            }
        }
    }
}
