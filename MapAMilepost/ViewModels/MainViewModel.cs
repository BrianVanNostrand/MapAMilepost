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
        public Commands.RelayCommand<object> SelectPageCommand => new Commands.RelayCommand<object>(async (button) => {
            if (MapViewUtils.CheckMapView())
            {
                await Commands.GraphicsCommands.DeselectAllGraphics();//remove all graphic selections
                MapToolUtils.DeactivateSession(this.SelectedViewModel);//deactivate any active map tool sessions
                Commands.TabCommands.SwitchTab(button, this);//switch the selected viewmodel
            }   
        });

        private async void OnMapChanged(ActiveMapViewChangedEventArgs obj)
        {
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(obj.IncomingView.Map);
            await Commands.DataGridCommands.ClearDataGridItems(this.MapPointVM, true);

        }


        public MainViewModel()
        {
            ArcGIS.Desktop.Framework.FrameworkApplication.NotificationInterval = 0;//allow toast messages to appear immediately after another is displayed
            MapPointVM = new MapPointViewModel();
            MapLineVM = new MapLineViewModel();
            SelectedViewModel = MapPointVM;
            ActiveMapViewChangedEvent.Subscribe(OnMapChanged);
        }
    }
}
