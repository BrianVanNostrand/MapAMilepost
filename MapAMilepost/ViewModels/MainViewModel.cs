using ArcGIS.Core.CIM;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Core.Events;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Internal.Mapping.Events;
using ArcGIS.Desktop.Internal.Mapping.Locate;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Layouts.Events;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using MapAMilepost.Commands;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapAMilepost.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        /// <summary>
        /// Private variables with associated public variables, granting access to the INotifyPropertyChanged command via ViewModelBase.
        /// </summary>
        private ViewModelBase _selectedViewModel;
        private MapPointViewModel _mapPointVM;
        private MapLineViewModel _mapLineVM;
        bool _addInReady = false;
        private bool _settingsMenuVisible = false;
        private bool _isPaused = false;
        private double _zoomScale = 0;
        private string _searchRadius = "3000";
        private string _responseDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
        private string _referenceDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
        /// <summary>
        /// -   The currently selected viewmodel, used when a tab is selected in the controlsGrid in MilepostDockpane.xaml
        ///     via data binding.
        /// </summary>
        public InfoModalInfo ModalInfo {  get; set; }
        public bool AddInReady
        {
            get { return _addInReady; }
            set
            {
                _addInReady = value;
                OnPropertyChanged(nameof(AddInReady));
            }
        }
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                OnPropertyChanged(nameof(IsPaused));
            }
        }
        public double ZoomScale
        {
            get => _zoomScale;
            set
            {
                _zoomScale = value;
                OnPropertyChanged(nameof(ZoomScale));
            }
        }
        public string SearchRadius
        {
            get => _searchRadius;
            set
            {
                _searchRadius = value;
                this.MapPointVM.PointArgs.SearchRadius = value;
                this.MapLineVM.LineArgs.StartArgs.SearchRadius = value;
                this.MapLineVM.LineArgs.EndArgs.SearchRadius = value;
                OnPropertyChanged(nameof(SearchRadius));
            }
        }
        public string ResponseDate
        {
            get { return _responseDate; }
            set
            {
                _responseDate = value;
                OnPropertyChanged(nameof(ResponseDate));
            }
        }

        public string ReferenceDate
        {
            get { return _referenceDate; }
            set
            {
                _referenceDate = value;
                OnPropertyChanged(nameof(ReferenceDate));
            }
        }
        public ViewModelBase SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                _selectedViewModel = value;
                OnPropertyChanged(nameof(SelectedViewModel));
            }
        }
        public MapPointViewModel MapPointVM
        {
            get { return _mapPointVM; }
            set
            {
                _mapPointVM = value;
                OnPropertyChanged(nameof(MapPointVM));
            }
        }

        public MapLineViewModel MapLineVM
        {
            get { return _mapLineVM;}
            set
            {
                _mapLineVM = value;
                OnPropertyChanged(nameof(MapLineVM));
            }
        }
        public bool SettingsMenuVisible
        {
            get { return _settingsMenuVisible; }
            set
            {
                _settingsMenuVisible = value;
                OnPropertyChanged(nameof(SettingsMenuVisible));
            }
        }
        public bool LayerVisible { get; set; } = false;

        /// <summary>
        /// Command used to change the selected viewmodel.
        /// </summary>
        public RelayCommand<object> SelectPageCommand => new Commands.RelayCommand<object>(async (button) => {
            AddInReady = false;
            SetModalSettings("load");
            TabControl parentControl = (button as TabItem).Parent as TabControl;
            parentControl.SelectedIndex = -1;
            await GraphicsCommands.DeselectAllGraphics();//remove all graphic selections
            await MapToolUtils.DeactivateSession(this.SelectedViewModel);//deactivate any active map tool sessions
            TabCommands.SwitchTab(button, this);//switch the selected viewmodel
            if (this.SelectedViewModel != null)
            {
                if (this.SelectedViewModel == this.MapPointVM)
                {
                    this.SelectedViewModel.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this.MapPointVM);
                    parentControl.SelectedIndex = 0;
                }
                else
                {
                    this.SelectedViewModel.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this.MapLineVM);
                    this.SelectedViewModel.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this.MapLineVM);
                    parentControl.SelectedIndex = 1;
                }
            }
            AddInReady = true;
        });
        public RelayCommand<object> OnShowCommand => new Commands.RelayCommand<object>(async (dockPane) =>
        {//when a map opens without the add in, and add in is opened
            AddInReady = false;
            SetModalSettings("load");
            if (MapViewActive())
            {
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
                if (graphicsLayer != null)
                {
                    if (MapView.Active.DrawingPaused)
                    {
                        AddInReady = false;
                        SetModalSettings("paused");
                    }
                    else
                    {
                        if (graphicsLayer.IsVisible == false)
                        {
                            AddInReady = false;
                            SetModalSettings("layerOff");
                        }
                        else
                        {
                            if (MapPointVM.PointResponses.Count == 0 || MapLineVM.LineResponses.Count == 0)
                            {
                                SetModalSettings("load");
                                await ResetLayer();
                                await GraphicsCommands.SynchronizeGraphicsToAddIn(this, await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map));
                                AddInReady = true;
                            }
                        }; 
                    }
                }
                else
                {
                    AddInReady = false;
                    SetModalSettings("noLayer");
                }
            }
            else
            {
                AddInReady = false;
                SetModalSettings("noMapView");
            }
        });
        public RelayCommand<object> ToggleSettingsVisibleCommand => new Commands.RelayCommand<object>((dockPane) =>
        {
            SettingsMenuVisible = !SettingsMenuVisible;
        });

        public RelayCommand<object> CreateGraphicsLayerCommand => new Commands.RelayCommand<object>(async (dockPane) =>
        {
            if (MapView.Active.Map == null) 
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Please wait for map to finish loading.");
            }
            bool layerCreated = await MapViewUtils.CreateMilepostMappingLayer(MapView.Active.Map);
            if (layerCreated)
            {
                if (MapView.Active.DrawingPaused)
                {
                    AddInReady = false;
                    SetModalSettings("paused");
                }
                else
                {
                    await ResetLayer();
                    AddInReady = true;
                    await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                    await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                    await Utils.MapToolUtils.DeactivateSession(this.MapPointVM, "point");
                    await Utils.MapToolUtils.DeactivateSession(this.MapLineVM, "line");
                }
            }
        });

        
        private async void OnActivePaneChanging(PaneEventArgs e)
        {
            LayerVisible = false;
            AddInReady = false;
            SetModalSettings("load");
            Debug.WriteLine($"OnActivePaneChanging {DateTime.Now.ToString("H:mm:ss")}");
        }
        private async void OnPaneChanged(PaneEventArgs e)
        {
            if(e.IncomingPane.GetType().Name!= "MapPaneViewModel"&& e.IncomingPane.GetType().Name != "TablePaneViewModel")
            {
                AddInReady = false;
                SetModalSettings("noMapView");
            }
            Debug.WriteLine($"OnPaneChanged {DateTime.Now.ToString("H:mm:ss")}");
        }
        private async void OnMapChanged(ActiveMapViewChangedEventArgs e)
        {
           
            Debug.WriteLine($"OnActiveMapViewChanged {DateTime.Now.ToString("H:mm:ss")}");
        }

        private async void OnLayerRemoved(LayerEventsArgs obj) 
        {
            await QueuedTask.Run(async () =>
            {
                bool gLayerRemoved = true;
                if(MapView.Active.Map != null && MapView.Active != null) { 
                    foreach (var item in MapView.Active.Map.Layers)
                    {
                        CIMBaseLayer baseLayer = item.GetDefinition();
                        if (baseLayer.CustomProperties != null && baseLayer.CustomProperties.Length > 0)
                        {
                            foreach (var prop in baseLayer.CustomProperties)
                            {
                                if (prop.Key == "MilepostMappingLayer" && prop.Value == "true")
                                {
                                    gLayerRemoved = false;
                                }
                            };
                        }

                    };
                    if (gLayerRemoved)
                    {
                        AddInReady = false;
                        SetModalSettings("noLayer");
                    }
                }
            });
        }

        private async void OnViewStateChanged(MapViewEventArgs obj)
        {
            if (obj.MapView!=null && obj.MapView.Map!=null)
            {
                if (obj.MapView.DrawingPaused)
                {
                    AddInReady = false;
                    SetModalSettings("paused");
                }
                else
                {
                    GraphicsLayer gl = await Utils.MapViewUtils.GetMilepostMappingLayer(obj.MapView.Map);
                    if (gl!=null)
                    {
                        await ResetLayer();
                        AddInReady = false;
                        SetModalSettings("load");
                        await GraphicsCommands.SynchronizeGraphicsToAddIn(this, gl);
                        AddInReady = true;
                    }
                    else
                    {
                        SetModalSettings("noLayer");
                    }
                }
            }
            Debug.WriteLine($"OnViewStateChange {DateTime.Now.ToString("H:mm:ss")}");
        }
        private async void OnPauseDrawingChanged(PauseDrawingChangedEventArgs obj)
        {
            IsPaused = obj.IsPaused;
            if (IsPaused)
            {
                AddInReady = false;
                SetModalSettings("paused");
            }
            else
            {
                if (obj.MapView.IsReady)
                {
                    AddInReady = false;
                    SetModalSettings("load");
                    GraphicsLayer gl = await Utils.MapViewUtils.GetMilepostMappingLayer(obj.MapView.Map);
                    await ResetLayer();
                    await GraphicsCommands.SynchronizeGraphicsToAddIn(this, gl);
                    AddInReady = true;
                }
                else
                {
                    AddInReady = false;
                    SetModalSettings("load");
                }
            }
        }
        private async void OnDrawEventStarted(MapViewEventArgs e) //make sure layer is still on when a layer in the TOC is toggled on or off
        {
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
            if(graphicsLayer != null)
            {
                if (graphicsLayer.IsVisible == false)
                {
                    AddInReady = false;
                    SetModalSettings("layerOff");
                    LayerVisible = false;
                }
                else
                {
                    if(LayerVisible == false)
                    {
                        AddInReady = false;
                        SetModalSettings("load");
                        await ResetLayer();
                        await GraphicsCommands.SynchronizeGraphicsToAddIn(this, await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map));
                        AddInReady = true;
                        LayerVisible = true;//prevents loading from happening every time every layer draws
                    }
                }
            }
        }
       
        private async void OnElementSelectionChanged(ElementSelectionChangedEventArgs obj)
        {

            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
            if (graphicsLayer == obj.ElementContainer)
            {
                await QueuedTask.Run(() => { 
                foreach (GraphicElement graphicElement in graphicsLayer.GetSelectedElements())
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Sorry, but to keep things from getting too complicated, graphics can only be selected from the Milepost Tools add in, and deleted and recreated if they need to be moved. Please click the row of the record you wish to delete, and use the 'Delete Records' button.");
                        graphicsLayer.UnSelectElement(graphicElement);
                    }
                });
            }
        }
        private async Task<bool> GraphicsLayerExists()
        {
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
            if(graphicsLayer == null)
            {
                return false;
            }
            return true;
        }
        private static bool MapViewActive()
        {
            if (MapView.Active != null && MapView.Active.Map != null)
            {
                return true;
            }
            return false;
        }

        private void SetModalSettings(string type)
        {
            ModalInfo.ShowButton = false;
            ModalInfo.ShowLoader = false;
            switch (type){
                case "noLayer":
                    ModalInfo.ShowButton = true;
                    ModalInfo.Title = "No Graphics Layer Detected";
                    ModalInfo.Caption = "You must create a new milepost mapping layer before you can continue.";
                    break;
                case "load":
                    ModalInfo.ShowLoader = true;
                    ModalInfo.Title = "Loading";
                    ModalInfo.Caption = "Please wait for the tool to finish loading.";
                    break;
                case "noMapView":
                    ModalInfo.Title = "No Map View Selected";
                    ModalInfo.Caption = "Please select a map to use this tool.";
                    break;
                case "paused":
                    ModalInfo.Title = "Map Paused";
                    ModalInfo.Caption = "Please unpause the map to use this tool.";
                    break;
                case "layerOff":
                    ModalInfo.Title = "Layer Disabled";
                    ModalInfo.Caption = "Please enable milepost mapping layer.";
                    break;
            }
        }
        private async Task ResetLayer()
        {
            await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
            await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
            await Utils.MapToolUtils.DeactivateSession(this.MapPointVM, "point");
            await Utils.MapToolUtils.DeactivateSession(this.MapLineVM, "line");
            await GraphicsCommands.DeselectAllGraphics();
        }

        public MainViewModel()
        {
            ArcGIS.Desktop.Framework.FrameworkApplication.NotificationInterval = 0;//allow toast messages to appear immediately after another is displayed
            MapLineVM = new()
            {
                LineArgs = new()
                {
                    StartArgs = new()
                    {
                        SearchRadius = this.SearchRadius,
                        ReferenceDate = this.ReferenceDate,
                        ResponseDate = this.ResponseDate,
                        ZoomScale = this.ZoomScale,
                    },
                    EndArgs = new()
                    {
                        SearchRadius = this.SearchRadius,
                        ReferenceDate = this.ReferenceDate,
                        ResponseDate = this.ResponseDate,
                        ZoomScale = this.ZoomScale,
                    }
                }
            };
            MapPointVM = new()
            {
                PointArgs = new()
                {
                    SearchRadius = this.SearchRadius,
                    ReferenceDate = this.ReferenceDate,
                    ResponseDate = this.ResponseDate,
                    ZoomScale = this.ZoomScale,
                }
            };
            SelectedViewModel = MapPointVM;
            LayersRemovedEvent.Subscribe(OnLayerRemoved);
            DrawStartedEvent.Subscribe(OnDrawEventStarted);
            PauseDrawingChangedEvent.Subscribe(OnPauseDrawingChanged);
            ElementSelectionChangedEvent.Subscribe(OnElementSelectionChanged);
            ActivePaneChangingEvent.Subscribe(OnActivePaneChanging);
            ActivePaneChangedEvent.Subscribe(OnPaneChanged);
            ActiveMapViewChangedEvent.Subscribe(OnMapChanged);
            ActiveMapViewStateChangedEvent.Subscribe(OnViewStateChanged);
            ModalInfo = new();
        }
    }
}
