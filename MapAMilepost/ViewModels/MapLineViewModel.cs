using MapAMilepost.Models;
using MapAMilepost.Utils;
using System.Windows.Input;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Controls;

namespace MapAMilepost.ViewModels
{
    public class MapLineViewModel:ViewModelBase
    {
        private SoeResponseModel _SoeResponse;
        private SoeResponseModel _soeEndResponse;
        private SoeArgsModel _soeStartArgs;
        private SoeArgsModel _soeEndArgs;
        private ObservableCollection<SoeResponseModel> _soeLineResponses;
        private bool _showResultsTable = false;
        private MapToolInfo _mapToolInfos;
        private bool _showWarningLabel = false;
        private string _warningLabel;
        public MapLineViewModel()//constructor
        {
            _SoeResponse = new SoeResponseModel();
            _soeEndResponse = new SoeResponseModel();
            _soeStartArgs = new SoeArgsModel();
            _soeEndArgs = new SoeArgsModel();
            _soeLineResponses = new ObservableCollection<SoeResponseModel>();
            _mapToolInfos = new MapToolInfo
            {
                SessionActive = false,
                SessionEndActive = false,
                MapButtonLabel = "Start Mapping",
                MapButtonEndLabel = "Start Mapping",
                MapButtonToolTip = "Start 'start point' mapping session.",
                MapButtonEndToolTip = "Start 'end point' mapping session."
            };
            MappingTool = new MapAMilepostMaptool();
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
        public override bool ShowWarningLabel
        {
            get { return _showWarningLabel; }
            set
            {
                _showWarningLabel = value;
                OnPropertyChanged(nameof(ShowWarningLabel));
            }
        }
        public override string WarningLabel
        {
            get { return _warningLabel; }
            set
            {
                _warningLabel = value;
                OnPropertyChanged(nameof(WarningLabel));
            }
        }
        public override SoeResponseModel SoeResponse
        {
            get { return _SoeResponse; }
            set { _SoeResponse = value; OnPropertyChanged(nameof(SoeResponse)); }
        }

        public override SoeResponseModel SoeEndResponse
        {
            get { return _soeEndResponse; }
            set { _soeEndResponse = value; OnPropertyChanged(nameof(SoeEndResponse)); }
        }

        public override SoeArgsModel SoeArgs
        {
            get { return _soeStartArgs; }
            set { _soeStartArgs = value; }
        }

        public override SoeArgsModel SoeEndArgs
        {
            get { return _soeEndArgs; }
            set { _soeEndArgs = value; }
        }

        public override ObservableCollection<SoeResponseModel> SoeResponses
        {
            get { return _soeLineResponses; }
            set { _soeLineResponses = value; OnPropertyChanged(nameof(SoeResponses)); }
        }

        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public override List<SoeResponseModel> SelectedItems { get; set; } = new List<SoeResponseModel>();

        public Commands.RelayCommand<object> UpdateSelectionCommand => new Commands.RelayCommand<object>((grid) => Commands.DataGridCommands.UpdateSelection(grid as DataGrid, this));

        public Commands.RelayCommand<object> DeleteItemsCommand => new Commands.RelayCommand<object>((parms) => Commands.DataGridCommands.DeleteItems(this));

        public Commands.RelayCommand<object> ClearItemsCommand => new Commands.RelayCommand<object>((parms) => Commands.DataGridCommands.ClearItems(this));

        public Commands.RelayCommand<object> SavePointResultCommand => new Commands.RelayCommand<object>((grid) => Commands.GraphicsCommands.SavePointResult(grid as DataGrid, this));

        public Commands.RelayCommand<object> ToggleMapToolSessionCommand => new Commands.RelayCommand<object>((startEnd) =>
        {
            if(MapToolInfos.SessionActive&&(string)startEnd=="start"|| MapToolInfos.SessionEndActive && (string)startEnd == "end")
            {
                Utils.MapToolUtils.DeactivateSession(this, (string)startEnd);
            }
            else
            {
                Utils.MapToolUtils.DeactivateSession(this, (string)startEnd);
                Utils.MapToolUtils.InitializeSession(this, (string)startEnd);
            }
        });
    }
}
