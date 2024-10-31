using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Events;
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
        private MapPointViewModel _mapPointVM;
        private MapLineViewModel _mapLineVM;
        /// <summary>
        /// -   The currently selected viewmodel, used when a tab is selected in the controlsGrid in MilepostDockpane.xaml
        ///     via data binding.
        /// </summary>
        public bool SyncComplete {  get; set; }
        public bool ShowLoader {  get; set; }
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

        /// <summary>
        /// Command used to change the selected viewmodel.
        /// </summary>
        public RelayCommand<object> SelectPageCommand => new Commands.RelayCommand<object>(async (button) => {
            if (MapViewUtils.CheckMapView())
            {
                await GraphicsCommands.DeselectAllGraphics();//remove all graphic selections
                await MapToolUtils.DeactivateSession(this.SelectedViewModel);//deactivate any active map tool sessions
                TabCommands.SwitchTab(button, this);//switch the selected viewmodel
            }   
        });
        public RelayCommand<object> OnShowCommand => new Commands.RelayCommand<object>(async (dockPane) => {
            if (MapView.Active != null)
            {
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
                await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                await GraphicsCommands.DeselectAllGraphics();
                if (graphicsLayer != null)
                {
                    await GraphicsCommands.SynchronizeGraphicsToAddIn(this, graphicsLayer);
                }
            }
        });

        private async void OnMapViewChanged(ActiveMapViewChangedEventArgs obj)
        {
            this.SyncComplete = false;
            this.MapPointVM.isEnabled = false;
            this.MapLineVM.isEnabled = false;
            if (obj.IncomingView != null && MapView.Active!=null)
            {
                ShowLoader = true;
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(obj.IncomingView.Map);
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
        private void OnDrawComplete( MapViewEventArgs obj)
        {
            if (SyncComplete)//if the add in is syncronized to the graphics layer contents
            {
                this.MapPointVM.isEnabled = true;
                this.MapLineVM.isEnabled = true;
                this.ShowLoader = false;
            }
            
        }
        private async void OnProjectOpened(ProjectEventArgs obj)
        {
            if(MapView.Active == null)
            {
                await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
            }
        }
        private async void OnLayerRemoved(LayerEventsArgs obj) 
        {
            if (MapView.Active!=null)
            {
                GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);
                if (graphicsLayer == null)
                {
                    await DataGridCommands.ClearDataGridItems(this.MapPointVM, true);
                    await DataGridCommands.ClearDataGridItems(this.MapLineVM, true);
                    await Utils.MapToolUtils.DeactivateSession(this.MapPointVM, "point");
                    await Utils.MapToolUtils.DeactivateSession(this.MapLineVM, "line");
                }
            }
        }
        public MainViewModel()
        {
            ArcGIS.Desktop.Framework.FrameworkApplication.NotificationInterval = 0;//allow toast messages to appear immediately after another is displayed
            MapPointVM = new MapPointViewModel();
            MapLineVM = new MapLineViewModel();
            SelectedViewModel = MapPointVM;
            ActiveMapViewChangedEvent.Subscribe(OnMapViewChanged);
            DrawCompleteEvent.Subscribe(OnDrawComplete);
            ProjectOpenedEvent.Subscribe(OnProjectOpened);
            LayersRemovedEvent.Subscribe(OnLayerRemoved);
        }
    }
}
