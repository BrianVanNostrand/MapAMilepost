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
using System.Linq;

namespace MapAMilepost.ViewModels
{
    public class MapLineViewModel:ViewModelBase
    {
        private LineResponseModel _lineResponse = new();
        private LineArgsModel _lineArgs = new();
        private ObservableCollection<LineResponseModel> _lineResponses = new();
        private bool _showResultsTable = true;
        private bool _sessionActive = false;
        private bool _sessionEndActive = false;
        private bool _isMapMode = true;
        private bool _srmpIsSelected = true;
        public MapLineViewModel()//constructor
        {
            MappingTool = new MapAMilepostMaptool();
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
            await Commands.GraphicsCommands.DeleteUnsavedGraphics();
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
                else 
                { 
                    LineResponse.StartResponse = new PointResponseModel();
                    LineResponse.EndResponse = new PointResponseModel();

                };
            };
        });

        public Commands.RelayCommand<object> ClearItemsCommand => new(async (parms) => {
            if (MapView.Active != null && MapView.Active.Map != null)
            {
                await Commands.DataGridCommands.ClearDataGridItems(this);
                if (!IsMapMode)
                {

                }
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
                this.LineResponse.EndResponse.Arm = null;
                this.LineResponse.EndResponse.Srmp = 0;
            }
            else
            {
                SRMPIsSelected = false;
                this.LineResponse.StartResponse.Arm = 0;
                this.LineResponse.StartResponse.Srmp = null;
                this.LineResponse.EndResponse.Arm = 0;
                this.LineResponse.EndResponse.Srmp = null;
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
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();//delete unsaved lines
                LineResponse.StartResponse = await ProcessPoint(LineResponse.StartResponse, LineArgs.StartArgs, "start");
                LineResponse.EndResponse = await ProcessPoint(LineResponse.EndResponse, LineArgs.EndArgs, "end");
                if (LineResponse.StartResponse.RouteGeometry != null && LineResponse.EndResponse.RouteGeometry != null)
                {
                    lineGeometryResponse = await Utils.MapToolUtils.GetLine(LineResponse.StartResponse, LineResponse.EndResponse, LineArgs.StartArgs.SR, LineArgs.StartArgs.ReferenceDate);
                    if (lineGeometryResponse.Count > 0)
                    {
                        await Commands.GraphicsCommands.CreateLineGraphics(LineResponse.StartResponse, LineResponse.EndResponse, lineGeometryResponse);
                        await CameraUtils.ZoomToCoords(lineGeometryResponse.First()[0], lineGeometryResponse.Last()[1]);
                    }
                }
            }
        });

        public Commands.RelayCommand<object> RouteChangedCommand => new((startEnd) =>
        {
            if (!IsMapMode)
            {
                LineResponse.EndResponse.Route = LineResponse.StartResponse.Route;
                if (this.SRMPIsSelected)
                {
                    LineResponse.StartResponse.Arm = null;
                    LineResponse.EndResponse.Arm = null;
                }
                else
                {
                    LineResponse.StartResponse.Srmp = null;
                    LineResponse.EndResponse.Srmp = null;
                }
            }
        });

        public Commands.RelayCommand<object> MPChangedCommand => new((startEnd) =>
        {
            if (!IsMapMode)
            {
                if ((string)startEnd == "start")
                {
                    if (this.SRMPIsSelected)
                    {
                        LineResponse.StartResponse.Arm = null;
                    }
                    else
                    {
                        LineResponse.StartResponse.Srmp = null;
                    }
                }
                else
                {
                    if (this.SRMPIsSelected)
                    {
                        LineResponse.EndResponse.Arm = null;
                    }
                    else
                    {
                        LineResponse.EndResponse.Srmp = null;
                    }
                }
            }
        });

        public Commands.RelayCommand<object> DirectionChangedCommand => new((startEnd) =>
        {
            LineResponse.EndResponse.Decrease = LineResponse.StartResponse.Decrease;
        });

         private async Task<PointResponseModel> ProcessPoint(PointResponseModel Point, PointArgsModel Args, string startEnd)
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
                if (Point.Route.Length < 3)
                {
                    while (Point.Route.Length < 3)
                    {
                        Point.Route = "0" + Point.Route;
                    }
                }
                LocationInfo formLocation = new LocationInfo(Point);
                if (SRMPIsSelected)
                {
                    formLocation.Arm = null;
                }
                else
                {
                    formLocation.Srmp = null;
                }
                PointResponseModel newPointResponse = await Utils.HTTPRequest.FindRouteLocation(formLocation, Args) as PointResponseModel;
                if (newPointResponse != null && newPointResponse.RouteGeometry != null)
                {
                    Point = newPointResponse;
                    await Commands.GraphicsCommands.CreatePointGraphics(Args, Point, startEnd);
                }
            }
            return Point;
        }

        private async Task ToggleSession(string startEnd)
        {
            if(SelectedLines.Count > 0)
            {
                if(SelectedLines.First().StartResponse==LineResponse.StartResponse|| SelectedLines.First().EndResponse == LineResponse.EndResponse)
                {
                    LineResponse = new LineResponseModel();
                }
            }
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
