using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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
        private MapPointViewModel _mapPointVM = new();
        private MapLineViewModel _mapLineVM = new();
        private bool _isEnabled = true;
        private bool _settingsMenuVisible = false;
        /// <summary>
        /// -   The currently selected viewmodel, used when a tab is selected in the controlsGrid in MilepostDockpane.xaml
        ///     via data binding.
        /// </summary>
        public ErrorModalModel ErrorModalInfo {  get; set; }
        public bool SyncComplete {  get; set; }
        public AppStateModel AppState { get; set; }
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
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

        /// <summary>
        /// Command used to change the selected viewmodel.
        /// </summary>
        public RelayCommand<object> SelectPageCommand => new Commands.RelayCommand<object>(async (button) => {
            if (MapViewUtils.CheckMapView())
            {
                TabControl parentControl = (button as TabItem).Parent as TabControl;
                parentControl.SelectedIndex = -1;
                this.IsEnabled = false;
                await GraphicsCommands.DeselectAllGraphics();//remove all graphic selections
                await MapToolUtils.DeactivateSession(this.SelectedViewModel);//deactivate any active map tool sessions
                TabCommands.SwitchTab(button, this);//switch the selected viewmodel
                this.IsEnabled = true;
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
            }
        });
        public RelayCommand<object> OnShowCommand => new Commands.RelayCommand<object>(async (dockPane) => {//when a map opens without the add in, and add in is opened
            await CheckAppState();
            if (AppState.MapViewPaneSelected)
            {
                if (MapPointVM.PointResponses.Count==0 || MapLineVM.LineResponses.Count == 0)
                {
                    await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                    await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                    await GraphicsCommands.DeselectAllGraphics();
                    if (AppState.Layer != null)
                    {
                        await GraphicsCommands.SynchronizeGraphicsToAddIn(this, AppState.Layer);
                    }
                    
                }
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
                await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                await Utils.MapToolUtils.DeactivateSession(this.MapPointVM, "point");
                await Utils.MapToolUtils.DeactivateSession(this.MapLineVM, "line");
                await CheckAppState();
            }
        });

        private async void OnMapViewChanged(ActiveMapViewChangedEventArgs obj)//when the mapview changes
        {
            await CheckAppState();
            if (obj.IncomingView != null)
            {
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(obj.IncomingView.Map);
                this.SyncComplete = false;
                this.MapPointVM.isEnabled = false;
                this.MapLineVM.isEnabled = false;
                if (obj.IncomingView != null && MapView.Active != null)
                {
                    await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                    await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                    await GraphicsCommands.DeselectAllGraphics();
                    if (graphicsLayer != null)
                    {
                        await GraphicsCommands.SynchronizeGraphicsToAddIn(this, graphicsLayer);
                    }
                    else
                    {
                        this.SyncComplete = true;
                    }
                }
            }
        }
        private async void OnDrawComplete( MapViewEventArgs obj)//when map is opened with add in opened, and draw completes.
        {
            await CheckAppState();
            if (AppState.Layer != null)
            {
                if (SyncComplete)//if the add in is syncronized to the graphics layer contents
                {
                    this.MapPointVM.isEnabled = true;
                    this.MapLineVM.isEnabled = true;
                }
                else
                {
                    await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                    await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                    await GraphicsCommands.DeselectAllGraphics();
                    await GraphicsCommands.SynchronizeGraphicsToAddIn(this, AppState.Layer);
                }
            }
            else
            {
                this.SyncComplete = true;
            }

        }
        private async void OnLayerRemoved(LayerEventsArgs obj) 
        {
            await CheckAppState();
            if (AppState.Layer == null)
            {
                await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                await Utils.MapToolUtils.DeactivateSession(this.MapPointVM, "point");
                await Utils.MapToolUtils.DeactivateSession(this.MapLineVM, "line");
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
        private async Task CheckAppState()
        {
            if (MapView.Active != null && MapView.Active.Map != null)
            {
                AppState.MapViewPaneSelected = true;
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
                if(graphicsLayer != null)
                {
                    AppState.Layer = graphicsLayer;
                    AppState.AppReady = true;
                }
                else
                {
                    AppState.AppReady = false;
                    AppState.Layer = null;
                    ErrorModalInfo.Title = "No Graphics Layer Detected";
                    ErrorModalInfo.Caption = "You must create a new milepost mapping layer before you can continue.";
                    ErrorModalInfo.ShowButton = true;
                }
            }
            else
            {
                AppState.AppReady = false;
                ErrorModalInfo.Title = "No Map View Detected";
                ErrorModalInfo.Caption = "A map view must be active to use this tool.";
                ErrorModalInfo.ShowButton = false;
            }
        }
        public MainViewModel()
        {
            ArcGIS.Desktop.Framework.FrameworkApplication.NotificationInterval = 0;//allow toast messages to appear immediately after another is displayed
            SelectedViewModel = MapPointVM;
            ActiveMapViewChangedEvent.Subscribe(OnMapViewChanged);
            DrawCompleteEvent.Subscribe(OnDrawComplete);
            LayersRemovedEvent.Subscribe(OnLayerRemoved);
            //ActivePaneChangedEvent.Subscribe(OnPaneChanged);//if pane is opened for the first time after the map is loaded
            ElementSelectionChangedEvent.Subscribe(OnElementSelectionChanged);
            AppState = new();
            ErrorModalInfo = new();
        }
    }
}
