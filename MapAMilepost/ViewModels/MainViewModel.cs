using MapAMilepost.Commands;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapAMilepost.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Private variables with associated public variables, granting access to the INotifyPropertyChanged command via ViewModelBase.
        /// </summary>
        private ViewModelBase _selectedViewModel;
        private MapPointViewModel _mapPointVM;
        private MapLineViewModel _mapLineVM;
        private string _testString = "MainVMTestString";

        public string TestString{
            get
            {
                return _testString;
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
        public Commands.RelayCommand<object> SelectPageCommand => new Commands.RelayCommand<object>((button) => {
            //MapToolUtils.DeactivateSession(this.SelectedViewModel);
            Console.WriteLine(MapPointVM);
            Commands.TabCommands.SwitchTab(button, this);
        });
        public void MapPointViewModel_onParameterChange(SoeResponseModel parameter)
        {
            // Do something with the new parameter data here
            MapPointVM.SoeResponse = parameter;
            Console.WriteLine(parameter.ToString());
        }
        public MainViewModel()
        {
            MapPointVM = new MapPointViewModel();
            MapPointVM.OnParameterChange += MapPointViewModel_onParameterChange;
            MapLineVM = new MapLineViewModel();
            SelectedViewModel = MapPointVM;
        }
    }
}
