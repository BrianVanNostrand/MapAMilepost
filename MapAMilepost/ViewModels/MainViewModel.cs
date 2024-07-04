using MapAMilepost.Commands;
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
        private string _testString = "TEST";

        public string TestString
        {
            get { return _testString; }
            set
            {
                _testString = value;
                OnPropertyChanged(nameof(TestString));
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
        public ICommand SelectPageCommand { get; set; }


        public MainViewModel()
        {
            MapPointVM = new MapPointViewModel();
            MapLineVM = new MapLineViewModel();
            SelectPageCommand = new SelectPage(viewModel:this);
            SelectedViewModel = MapPointVM;
            TestString = "TEST";
        }
    }
}
