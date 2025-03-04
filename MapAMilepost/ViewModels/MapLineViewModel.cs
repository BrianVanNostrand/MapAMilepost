using MapAMilepost.Models;
using MapAMilepost.Utils;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Controls;
using ArcGIS.Desktop.Mapping;
using System;
using System.Threading.Tasks;
using System.Linq;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;

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
        private bool _srmpIsSelected;
        private int _routeComboIndex;
        private ObservableCollection<RouteIDInfo> _routeIDInfos = new();
        private ObservableCollection<string> _routeQualifiers = new();
        public MapLineViewModel()//constructor
        {
            MappingTool = new();//initialize map tool here or it won't run on first click. Weird bug? This should be intitializing in the base class, but alas...
            SRMPIsSelected = true;
            RouteComboIndex = -1;
        }
        public override bool SRMPIsSelected
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
        public override ObservableCollection<RouteIDInfo> RouteIDInfos
        {
            get { return _routeIDInfos; }
            set
            {
                _routeIDInfos = value;
                OnPropertyChanged(nameof(RouteIDInfos));
            }
        }
        public override ObservableCollection<string> RouteQualifiers
        {
            get { return _routeQualifiers; }
            set
            {
                _routeQualifiers = value;
                OnPropertyChanged(nameof(RouteQualifiers));
            }
        }
        public override int RouteComboIndex
        {
            get { return _routeComboIndex; }
            set
            {
                _routeComboIndex = value;
                OnPropertyChanged(nameof(RouteComboIndex));
            }
        }
        public override List<LineResponseModel> SelectedLines { get; set; } = new List<LineResponseModel>();
        public Commands.RelayCommand<object> UpdateSelectionCommand => new (async(grid) => {
            if (!IsMapMode)
            {
                Dictionary<string, int> RouteResponses = await Utils.HTTPRequest.GetVMRouteLists(this);
                Utils.UIUtils.SetRouteInfos(RouteResponses, this);
                RouteQualifiers = new() { };
            }
            await Commands.DataGridCommands.UpdateLineSelection(grid as DataGrid, this);
        });
        public Commands.RelayCommand<object> RouteChangedCommand => new((object comboBox) =>
        {
            RouteComboIndex = (comboBox as ComboBox).SelectedIndex;
            RouteIDInfo val = (comboBox as ComboBox).SelectedItem as RouteIDInfo;
            if (!IsMapMode)
            {
                if (this.SRMPIsSelected)
                {
                    LineResponse.EndResponse.Arm = null;
                    LineResponse.StartResponse.Arm = null;
                }
                else
                {
                    LineResponse.EndResponse.Srmp = null;
                    LineResponse.StartResponse.Srmp = null;
                }
            }
            if (val != null)
            {
                LineResponse.EndResponse.Route = val.Title;
                LineResponse.StartResponse.Route = val.Title;
                RouteQualifiers = Utils.UIUtils.GetRouteQualifiers(val);
                //ObservableCollection<string> NewRouteQualifiers = Utils.UIUtils.GetRouteQualifiers(val);
                //RouteQualifiers = NewRouteQualifiers;
            }
        });
        public Commands.RelayCommand<object> RouteQualifierChangedCommand => new((object selectedValue) =>
        {
            ComboBox cBox = (selectedValue as ComboBox);
            if (cBox.SelectedItem != null && cBox.SelectedItem.ToString() != "Mainline")
            {
                LineResponse.StartResponse.Route = $"{LineResponse.StartResponse.Route}{(selectedValue as ComboBox).SelectedItem}";
                LineResponse.EndResponse.Route = $"{LineResponse.EndResponse.Route}{(selectedValue as ComboBox).SelectedItem}";
            }
            else
            {
                cBox.SelectedIndex = 0;
            }
           (selectedValue as ComboBox).UpdateLayout();
        });
        public Commands.RelayCommand<object> ChangeModeCommand => new(async(param) =>
        {
            IsMapMode = !IsMapMode;
            await Utils.UIUtils.ResetUI(this);
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
            if (IsMapMode == false && RouteIDInfos.Count == 0)
            {
                Dictionary<string, int> RouteResponses = await Utils.HTTPRequest.GetVMRouteLists(this);
                Utils.UIUtils.SetRouteInfos(RouteResponses, this);
            }
        });
        public Commands.RelayCommand<object> ZoomToRecordCommand => new(async (grid) =>
        {
            if(SelectedLines.Count > 0 && MapView.Active!=null && MapView.Active.Map!=null)
            {
                ArcGIS.Core.Geometry.Envelope envelope = await Commands.GraphicsCommands.GetLineGeometryFromSelection(SelectedLines.First());
                if(envelope != null)
                {
                    Geometry buffer = GeometryEngine.Instance.Buffer(envelope, envelope.Width>envelope.Length ? envelope.Width * .2: envelope.Length * .2);
                    await MapView.Active.ZoomToAsync(buffer, TimeSpan.FromSeconds(.5));
                };
            }
        });
        public Commands.RelayCommand<object> DeleteItemsCommand => new (async(parms) => { 
            await Commands.DataGridCommands.DeleteLineItems(this);
            await Utils.UIUtils.ResetUI(this);
        });

        public Commands.RelayCommand<object> SaveLineResultCommand => new (async(grid) => {
            await Commands.GraphicsCommands.SaveLineResult(grid as DataGrid, this);
            await Utils.UIUtils.ResetUI(this);
        });
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
                LineResponse.StartResponse.RouteGeometry = null;
                LineResponse.EndResponse.RouteGeometry = null;
                List<List<double>> lineGeometryResponse = new();
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();//delete unsaved lines
                LineResponse.StartResponse = await ProcessPoint(LineResponse.StartResponse, LineArgs.StartArgs, "start");
                LineResponse.EndResponse = await ProcessPoint(LineResponse.EndResponse, LineArgs.EndArgs, "end");
                GraphicInfoModel StartInfo = await Commands.GraphicsCommands.CreatePointGraphics(LineArgs.StartArgs, LineResponse.StartResponse, "start", IsMapMode);
                GraphicInfoModel EndInfo = await Commands.GraphicsCommands.CreatePointGraphics(LineArgs.EndArgs, LineResponse.EndResponse, "end", IsMapMode);
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);//look for layer
                foreach (GraphicInfoModel item in new List<GraphicInfoModel> { StartInfo, EndInfo })
                {
                    if (item.CGraphic != null && item.EInfo != null)//EInfo is he array of custom properties, CGraphic is the CIM piint graphic definition.
                    {
                        await QueuedTask.Run(() =>
                        {
                            graphicsLayer.AddElement(cimGraphic: item.CGraphic, elementInfo: item.EInfo, select: false);
                        });
                    }
                }
                if (LineResponse.StartResponse.RouteGeometry != null && LineResponse.EndResponse.RouteGeometry != null)
                {
                    lineGeometryResponse = await Utils.MapToolUtils.GetLine(LineResponse.StartResponse, LineResponse.EndResponse, LineArgs.StartArgs.SR, LineArgs.StartArgs.ReferenceDate);
                    if (lineGeometryResponse.Count > 0)
                    {
                        await Commands.GraphicsCommands.CreateLineGraphics(LineResponse.StartResponse, LineResponse.EndResponse, lineGeometryResponse);
                        await CameraUtils.ZoomToEnvelope(lineGeometryResponse);
                    }
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
            if(!IsMapMode)
            {
                LineResponse.EndResponse.Decrease = LineResponse.StartResponse.Decrease;
            }
        });

         private async Task<PointResponseModel> ProcessPoint(PointResponseModel Point, PointArgsModel Args, string startEnd)
        {
            //Point.ReferenceDate = Args.ReferenceDate;
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
                PointResponseModel newPointResponse = await Utils.HTTPRequest.FindRouteLocation(formLocation, Args, directionText:errorDialog) as PointResponseModel;
                if (newPointResponse != null && newPointResponse.RouteGeometry != null)
                {
                    Point = newPointResponse;
                }
                else
                {
                    if(newPointResponse.Error != null)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(newPointResponse.Error);
                    }
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
