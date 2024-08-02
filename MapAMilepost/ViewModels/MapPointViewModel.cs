using MapAMilepost.Models;
using MapAMilepost.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace MapAMilepost.ViewModels
{
    public class MapPointViewModel:ViewModelBase
    {
        /// <summary>
        /// Private variables with associated public variables, granting access to the INotifyPropertyChanged command via ViewModelBase.
        /// </summary>
        private SoeResponseModel _SoeResponse;
        private SoeArgsModel _SoeArgs;
        private ObservableCollection<SoeResponseModel> _SoeResponses;
        private bool _showResultsTable = false;
        private MapToolInfo _mapToolInfos;
        public MapPointViewModel()//constructor
        {
            _SoeResponse = new SoeResponseModel();
            _SoeArgs = new SoeArgsModel();
            _SoeResponses = new ObservableCollection<SoeResponseModel>();
            _mapToolInfos = new MapToolInfo {
                SessionActive = false,
                MapButtonLabel = "Start Mapping",
                MapButtonToolTip = "Start mapping session."
            };
        }
        public override MapToolInfo MapToolInfos
        {
            get { return _mapToolInfos; }
            set
            {
                _mapToolInfos = value;
                OnPropertyChanged(nameof(MapToolInfos));
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
        public override SoeResponseModel SoeResponse
        {
            get { return _SoeResponse; }
            set { _SoeResponse = value; OnPropertyChanged(nameof(SoeResponse)); }
        }

        /// <summary>
        /// -   Arguments passed to the SOE HTTP query.
        /// </summary>
        public override SoeArgsModel SoeArgs
        {
            get { return _SoeArgs; }
            set { _SoeArgs = value; OnPropertyChanged(nameof(SoeArgs)); }
        }

        /// <summary>
        /// -   Array of saved SoeResponse data objects.
        /// </summary>
        public override ObservableCollection<SoeResponseModel> SoeResponses
        {
            get { return _SoeResponses; }
            set { _SoeResponses = value; OnPropertyChanged(nameof(SoeResponses)); }
        }

        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public override List<SoeResponseModel> SelectedItems { get; set; } = new List<SoeResponseModel>();
   
        public Commands.RelayCommand<object> UpdateSelectionCommand => new Commands.RelayCommand<object>((grid) => Commands.DataGridCommands.UpdateSelection(grid as DataGrid, this));

        public Commands.RelayCommand<object> DeleteItemsCommand => new Commands.RelayCommand<object>((p) => Commands.DataGridCommands.DeleteItems(this));
        
        public Commands.RelayCommand<object> ClearItemsCommand => new Commands.RelayCommand<object>((p) => Commands.DataGridCommands.ClearItems(this));

        public Commands.RelayCommand<object> SavePointResultCommand => new Commands.RelayCommand<object>((grid) => Commands.GraphicsCommands.SavePointResult(grid as DataGrid, this));       

        public Commands.RelayCommand<object> ToggleMapToolSessionCommand => new Commands.RelayCommand<object>((p) => { 
            if (!this.MapToolInfos.SessionActive) {
                Utils.MapToolUtils.InitializeSession(this);
            }
            else{
                Utils.MapToolUtils.DeactivateSession(this);
            }
        });
    }
}
