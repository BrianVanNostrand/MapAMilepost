using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
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
    public class MapPointViewModel : ViewModelBase
    {
        /// <summary>
        /// Private variables with associated public variables, granting access to the INotifyPropertyChanged command via ViewModelBase.
        /// </summary>
        private PointResponseModel _pointResponse = new();
        private PointArgsModel _pointArgs = new();
        private ObservableCollection<PointResponseModel> _pointResponses = new();
        private bool _showResultsTable = false;
        private bool _sessionActive = false;
        private bool _isMapMode = true;
        private bool _srmpIsSelected;
        private int _routeComboIndex;
        private ObservableCollection<RouteIDInfo> _routeIDInfos = new();
        private ObservableCollection<string> _routeQualifiers = new();
        public MapPointViewModel()//constructor
        {
            MappingTool = new();//initialize map tool here or it won't run on first click. Weird bug?
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
            set { _pointResponse = value; OnPropertyChanged(nameof(PointResponse)); }
        }

        /// <summary>
        /// -   Arguments passed to the SOE HTTP query.
        /// </summary>
        public override PointArgsModel PointArgs
        {
            get { return _pointArgs; }
            set {
                _pointArgs = value;
                OnPropertyChanged(nameof(PointArgs));
            }
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
        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public override List<PointResponseModel> SelectedPoints { get; set; } = new List<PointResponseModel>();
        public Commands.RelayCommand<object> UpdateSelectionCommand => new(async (grid) => {
            if (!IsMapMode)
            {
                Dictionary<string, int> RouteResponses = await Utils.HTTPRequest.GetVMRouteLists(this);
                Utils.UIUtils.SetRouteInfos(RouteResponses, this);
                RouteQualifiers = new() { };
            }
            await Commands.DataGridCommands.UpdatePointSelection(grid as DataGrid, this);
        });
        public Commands.RelayCommand<object> RouteChangedCommand => new((object selectedValue) =>
        {
            //RouteQualifiers.Clear();
            RouteIDInfo val = selectedValue as RouteIDInfo;
            if (!IsMapMode)
            {
                if (this.SRMPIsSelected)
                {
                    PointResponse.Arm = null;
                }
                else
                {
                    PointResponse.Srmp = null;
                }
            }
            if (val != null)
            {
                PointResponse.Route = val.Title;
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
                PointResponse.Route = $"{PointResponse.Route}{(selectedValue as ComboBox).SelectedItem}";
            }
            else
            {
                cBox.SelectedIndex = 0;
            }
            (selectedValue as ComboBox).UpdateLayout();
        });

        public Commands.RelayCommand<object> ChangeModeCommand => new(async (param) =>
        {
            IsMapMode = !IsMapMode;
            await Utils.UIUtils.ResetUI(this);
            await Commands.GraphicsCommands.DeleteUnsavedGraphics();
            await Utils.MapToolUtils.DeactivateSession(this, "point");
            if (!IsMapMode)
            {
                PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this);
            }
            else
            {
                if (SelectedPoints.Count == 1)
                {
                    PointResponse = SelectedPoints[0];
                }
                else
                {
                    PointResponse = new PointResponseModel();
                }
            };
            if (IsMapMode == false && RouteIDInfos.Count == 0)
            {
                Dictionary<string, int> RouteResponses = await Utils.HTTPRequest.GetVMRouteLists(this);
                Utils.UIUtils.SetRouteInfos(RouteResponses, this);
            }
        });
        public Commands.RelayCommand<object> ZoomToRecordCommand => new(async (grid) =>
        {
            PointResponseModel.coordinatePair coordPair = Commands.DataGridCommands.GetSelectedGraphicInfoPoint(grid as DataGrid, this);
            if (coordPair != null)
            {
                await CameraUtils.ZoomToCoords(coordPair.x, coordPair.y, PointArgs.ZoomScale);
            }
        });

        public Commands.RelayCommand<object> DeleteItemsCommand => new(async (p) => {
            await Commands.DataGridCommands.DeletePointItems(this);
            await Utils.UIUtils.ResetUI(this);
        });

        public Commands.RelayCommand<object> SavePointResultCommand => new(async(grid) => {
            await Commands.GraphicsCommands.SavePointResult(grid as DataGrid, this);
            await Utils.UIUtils.ResetUI(this);
        });
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
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();//delete all unsaved graphics
                bool formDataValid = HTTPRequest.CheckFormData(PointResponse);
                if (formDataValid)
                {

                    LocationInfo formLocation = new LocationInfo(PointResponse);
                    if (formLocation.Route.Length < 3)
                    {
                        while (formLocation.Route.Length < 3)
                        {
                            formLocation.Route = "0"+formLocation.Route;
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
                    var newPointResponse = await Utils.HTTPRequest.FindRouteLocation(formLocation, PointArgs) as PointResponseModel;
                    if (newPointResponse != null && newPointResponse.RouteGeometry != null) {
                        PointResponse = newPointResponse;
                        GraphicInfoModel gInfo = await Commands.GraphicsCommands.CreatePointGraphics(PointArgs, PointResponse, "point", IsMapMode);
                        GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);//look for layer
                        if (gInfo.CGraphic != null && gInfo.EInfo != null)
                        {
                            await QueuedTask.Run(() =>
                            {
                                graphicsLayer.AddElement(cimGraphic: gInfo.CGraphic, elementInfo: gInfo.EInfo, select: false);
                            });
                        }
                        await CameraUtils.ZoomToCoords(PointResponse.RouteGeometry.x, PointResponse.RouteGeometry.y);
                    }
                    else
                    {
                        if (newPointResponse.Error != null)
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(newPointResponse.Error);
                        }
                    }
                   
                }
            }
        });

        public Commands.RelayCommand<object> MPChangedCommand => new((StartEnd) =>
        {
            if (!IsMapMode)
            {
                if (this.SRMPIsSelected)
                {
                    PointResponse.Arm = null;
                }
                else
                {
                    PointResponse.Srmp = null;
                }
            }
        });
        public Commands.RelayCommand<object> ExportFeatures => new(async (button) =>
        {
            FeatureClassInfo fcInfo = await Utils.ExportUtils.CreatePointFC("point",PointArgs.SR);
            if (fcInfo!=null && fcInfo.FCTitle != null && fcInfo.GDBPath!=null)
            {
                await Utils.ExportUtils.PopulateFC(fcInfo,"point", "point");//FeatureClassInfo and session type
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
