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
        private MapTableViewModel _mapTableVM;
        bool _addInReady = false;
        private bool _settingsMenuVisible = false;
        private bool _isPaused = false;
        private double _zoomScale = 0;
        private string _searchRadius = "3000";
        private string _responseDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
        private string _referenceDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
       
        /// <summary>
        /// Whether or not the add in is ready.
        /// If false, the error modal is displayed.
        /// </summary>
        public bool AddInReady
        {
            get { return _addInReady; }
            set
            {
                _addInReady = value;
                OnPropertyChanged(nameof(AddInReady));
            }
        }
        /// <summary>
        /// Whether or not the active map is paused. 
        /// Used to update the error modal text.
        /// </summary>
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                OnPropertyChanged(nameof(IsPaused));
            }
        }

        //object that describes the state of the error modal
        public InfoModalInfo ModalInfo { get; set; }

        /// <summary>
        /// Whether or not the graphics layer is visible. 
        /// Used in the draw event handler to prevent code 
        /// from executing over and over.
        /// </summary>
        public bool? LayerVisible { get; set; } = null;
       
        /// <summary>
        /// The map table Viewmodel. Used in datatemplate of ESRIDockpane to display the map line view.
        /// </summary>
        public MapLineViewModel MapLineVM
        {
            get { return _mapLineVM;}
            set
            {
                _mapLineVM = value;
                OnPropertyChanged(nameof(MapLineVM));
            }
        }
        /// <summary>
        /// The map table Viewmodel. Used in datatemplate of ESRIDockpane to display the map point view.
        /// </summary>
        public MapPointViewModel MapPointVM
        {
            get { return _mapPointVM; }
            set
            {
                _mapPointVM = value;
                OnPropertyChanged(nameof(MapPointVM));
            }
        }
        /// <summary>
        /// The map table Viewmodel. Used in datatemplate of ESRIDockpane to display the map table view.
        /// </summary>
        public MapTableViewModel MapTableVM
        {
            get { return _mapTableVM; }
            set
            {
                _mapTableVM = value;
                OnPropertyChanged(nameof(MapTableVM));
            }
        }
       
        /// <summary>
        /// Today's date. This is the date that the SOE request was initiated. 
        /// When set, this is fed down 
        /// to SOE request arguments for both the point and line viewmodels.
        /// </summary>
        public string ResponseDate
        {
            get { return _responseDate; }
            set
            {
                _responseDate = value;
                this.MapLineVM.LineArgs.StartArgs.ResponseDate = value;
                this.MapLineVM.LineArgs.EndArgs.ResponseDate = value;
                this.MapPointVM.PointArgs.ResponseDate = value;
                OnPropertyChanged(nameof(ResponseDate));
            }
        }
        /// <summary>
        /// Reference date to be used in SOE calls. When set, this is fed down 
        /// to SOE request arguments for both the point and line viewmodels.
        /// Is set by the date selector in the settings menu.
        /// </summary>
        public string ReferenceDate
        {
            get { return _referenceDate; }
            set
            {
                _referenceDate = DateTime.Parse(value).Date.ToShortDateString();
                this.MapLineVM.LineArgs.StartArgs.ReferenceDate = _referenceDate;
                this.MapLineVM.LineArgs.EndArgs.ReferenceDate = _referenceDate;
                this.MapPointVM.PointArgs.ReferenceDate = _referenceDate;
                OnPropertyChanged(nameof(ReferenceDate));
            }
        }
        /// <summary>
        /// The search radius used in the map tool for "Find Nearest Route Location" 
        /// requests. This value is bound two ways to the text box in the settings
        /// menu and is initialized as 3000 feet. When set, this is fed down 
        /// to SOE request arguments for both the point and line viewmodels.
        /// </summary>
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
        /// <summary>
        /// -   The currently selected viewmodel, used when a tab is selected in the controlsGrid in MilepostDockpane.xaml
        ///     via data binding.
        /// </summary>
        public ViewModelBase SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                _selectedViewModel = value;
                OnPropertyChanged(nameof(SelectedViewModel));
            }
        }
        //Whether or not the settings menu should be displayed. 
        //Toggled by settings menu button.
        public bool SettingsMenuVisible
        {
            get { return _settingsMenuVisible; }
            set
            {
                _settingsMenuVisible = value;
                OnPropertyChanged(nameof(SettingsMenuVisible));
            }
        }
        

        /// <summary>
        /// Unused implementation of a selectable zoom scale for the settings panel. 
        /// </summary>
        public double ZoomScale
        {
            get => _zoomScale;
            set
            {
                _zoomScale = value;
                OnPropertyChanged(nameof(ZoomScale));
            }
        }

        /// <summary>
        /// Command used to create the graphics layer when the "Create milepost mapping layer" experience is used in the add in.
        /// </summary>
        public RelayCommand<object> CreateGraphicsLayerCommand => new Commands.RelayCommand<object>(async (dockPane) =>
        {
            if (Utils.UIUtils.MapViewActive())
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
                    await ResetVMUI();
                    AddInReady = true;
                }
            }
        });

        /// <summary>
        /// Command triggered by the ESRIDockpane being shown, from a click of the "MapAMilepost_ESRIDockpane_ShowButton"
        /// in config.daml. This is the button in the ArcGIS Pro add in ribbon.
        /// </summary>
        public RelayCommand<object> OnShowCommand => new Commands.RelayCommand<object>(async (dockPane) =>
        {//when a map opens without the add in, and add in is opened
            AddInReady = false;
            SetModalSettings("load");
            if (Utils.UIUtils.MapViewActive())
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
                            SetModalSettings("load");
                            await ResetVMUI();
                            await GraphicsCommands.SynchronizeGraphicsToAddIn(this, await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map));
                            AddInReady = true;
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

        /// <summary>
        /// Command used to change the selected viewmodel.
        /// </summary>
        public RelayCommand<object> SelectPageCommand => new Commands.RelayCommand<object>(async (button) => {
            AddInReady = false;
            SetModalSettings("load");
            TabControl parentControl = (button as TabItem).Parent as TabControl;
            parentControl.SelectedIndex = -1;
            await Utils.UIUtils.ResetUI(MapLineVM);
            await Utils.UIUtils.ResetUI(MapPointVM);
            await GraphicsCommands.DeselectAllGraphics();//remove all graphic selections
            await MapToolUtils.DeactivateSession(this.SelectedViewModel);//deactivate any active map tool sessions
            await GraphicsCommands.SynchronizeGraphicsToAddIn(this, await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map));
            TabCommands.SwitchTab(button, this);//switch the selected viewmodel
            if (this.SelectedViewModel != null)
            {
                if (this.SelectedViewModel == this.MapPointVM)
                {
                    this.SelectedViewModel.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this.MapPointVM);
                    parentControl.SelectedIndex = 0;
                }
                if (this.SelectedViewModel == this.MapLineVM)
                {
                    this.SelectedViewModel.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this.MapLineVM);
                    this.SelectedViewModel.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(this.MapLineVM);
                    parentControl.SelectedIndex = 1;
                }
                if (this.SelectedViewModel == this.MapTableVM)
                {
                    this.MapTableVM.WarningMessage = String.Empty;
                    SettingsMenuVisible = false;
                    parentControl.SelectedIndex = 2;
                }
            }
            AddInReady = true;
        });
        
        /// <summary>
        /// Command executed when the "+/-" button in the settings panel is clicked
        /// to show the settings panel.
        /// </summary>
        public RelayCommand<object> ToggleSettingsVisibleCommand => new Commands.RelayCommand<object>((dockPane) =>
        {
            SettingsMenuVisible = !SettingsMenuVisible;
        });
        
        /// <summary>
        /// Method triggered by event hook when the active ArcGIS Pro pane begins changing.
        /// </summary>
        /// <param name="e"></param>
        private void OnActivePaneChanging(PaneEventArgs e)
        {
            LayerVisible = false;
            AddInReady = false;
            SetModalSettings("load");
            Debug.WriteLine($"OnActivePaneChanging {DateTime.Now.ToString("H:mm:ss")}");
        }

        /// <summary>
        /// Method triggered by event hook when the active pane has changed.
        /// </summary>
        /// <param name="e"></param>
        private void OnPaneChanged(PaneEventArgs e)
        {
            if(e.IncomingPane.GetType().Name!= "MapPaneViewModel"&& e.IncomingPane.GetType().Name != "TablePaneViewModel")
            {
                AddInReady = false;
                SetModalSettings("noMapView");
            }
            Debug.WriteLine($"OnPaneChanged {DateTime.Now.ToString("H:mm:ss")}");
        }

        /// <summary>
        /// Method triggered by event hook when the active map has changed. 
        /// </summary>
        /// <param name="e"></param>
        private void OnMapChanged(ActiveMapViewChangedEventArgs e)
        {
            Debug.WriteLine($"OnActiveMapViewChanged {DateTime.Now.ToString("H:mm:ss")}");
        }

        /// <summary>
        /// Method triggered by event hook when a layer in the active map has been removed.  
        /// </summary>
        /// <param name="obj"></param>
        private async void OnLayerRemoved(LayerEventsArgs obj) 
        {
            await QueuedTask.Run(() =>
            {
                bool gLayerRemoved = true;
                if(Utils.UIUtils.MapViewActive()) { 
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
                        //turn on error modal
                        AddInReady = false;
                        //set error modal text to "no layer" state
                        SetModalSettings("noLayer");
                        //clear all point and line response data from child viewmodels.
                        MapPointVM.PointResponses.Clear();
                        MapPointVM.PointResponse = new();
                        MapLineVM.LineResponses.Clear();
                        MapLineVM.LineResponse = new();
                    }
                }
            });
        }

        /// <summary>
        /// Method triggered by event hook when the active map view state changed (ready etc.). 
        /// </summary>
        /// <param name="obj"></param>
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
                        await ResetVMUI();
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

        /// <summary>
        /// Method triggered by event hook when the active map is paused or unpaused. 
        /// </summary>
        /// <param name="obj"></param>
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
                    await ResetVMUI();
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

        /// <summary>
        /// Method triggered by event hook when any feature in the active map has begun to draw.  
        /// </summary>
        /// <param name="e"></param>
        private async void OnDrawEventStarted(MapViewEventArgs e) //make sure layer is still on when a layer in the TOC is toggled on or off
        {
            if (Utils.UIUtils.MapViewActive())
            {
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
                if(graphicsLayer != null)
                {
                    if (graphicsLayer.IsVisible == false)
                    {
                        AddInReady = false;
                        SetModalSettings("layerOff");
                        await ResetVMUI();
                        this.MapLineVM.LineResponse = new();
                        this.MapPointVM.PointResponse = new();
                        LayerVisible = false;
                    }
                    else
                    {
                        if(LayerVisible == false)//if layer is being turned on (if this part is left out, this will execute constantly as different layers draw different features asyncronously)
                        {
                            AddInReady = false;
                            SetModalSettings("load");
                            await GraphicsCommands.SynchronizeGraphicsToAddIn(this, await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map));
                            AddInReady = true;
                            LayerVisible = true;//prevents loading from happening every time every layer draws
                        }
                    }
                }
                //set spatial reference to new map view
                if (
                    MapPointVM.PointArgs.SR!= MapView.Active.Map.SpatialReference.Wkid
                )
                {
                    MapPointVM.PointArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                }
                if (
                    MapLineVM.LineArgs.StartArgs.SR != MapView.Active.Map.SpatialReference.Wkid||
                    MapLineVM.LineArgs.EndArgs.SR != MapView.Active.Map.SpatialReference.Wkid
                )
                {
                    MapLineVM.LineArgs.StartArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                    MapLineVM.LineArgs.EndArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                }
                if(
                    MapTableVM.PointArgs.SR != MapView.Active.Map.SpatialReference.Wkid ||
                    MapTableVM.LineArgs.StartArgs.SR != MapView.Active.Map.SpatialReference.Wkid ||
                    MapTableVM.LineArgs.EndArgs.SR != MapView.Active.Map.SpatialReference.Wkid
                )
                {
                    MapTableVM.PointArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                    MapTableVM.LineArgs.StartArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                    MapTableVM.LineArgs.EndArgs.SR = MapView.Active.Map.SpatialReference.Wkid;
                }
            }
        }

        /// <summary>
        /// Method triggered by event hook when an element in the active map has been selected.  
        /// Prevents users from selecting a graphic in the milepost mapping graphics layer.
        /// </summary>
        /// <param name="obj"></param>
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

        /// <summary>
        /// Update the modal info's title, caption, and element content 
        /// </summary>
        /// <param name="type"></param>
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

        /// <summary>
        /// Reset combobox indices, point and line response values, 
        /// delete unsaved graphics, deselect all graphics, and deactivate map tool session
        /// </summary>
        /// <returns></returns>
        private async Task ResetVMUI()
        {
            await Utils.UIUtils.ResetUI(MapPointVM);
            await Utils.UIUtils.ResetUI(MapLineVM);
            await Utils.MapToolUtils.DeactivateSession(MapPointVM, "point");
            await Utils.MapToolUtils.DeactivateSession(MapLineVM, "line");
            await GraphicsCommands.DeselectAllGraphics();
        }

        /// <summary>
        /// constructor
        /// </summary>
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
            MapTableVM = new()
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
                },
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
