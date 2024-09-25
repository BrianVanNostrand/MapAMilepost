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
        private LineResponseModel _lineResponse;
        private LineArgsModel _lineArgs;
        private ObservableCollection<LineResponseModel> _lineResponses;
        private bool _showResultsTable = true;
        private MapToolInfo _mapToolInfos;
        public MapLineViewModel()//constructor
        {
            _lineArgs = new LineArgsModel();
            _lineResponses = new ObservableCollection<LineResponseModel>();
            _lineResponse = new LineResponseModel();
            _mapToolInfos = new MapToolInfo
            {
                SessionActive = false,
                SessionEndActive = false,
                MapButtonLabel = "Map Start",
                MapButtonEndLabel = "Map End",
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

        public override LineResponseModel LineResponse
        {
            get { return _lineResponse; }
            set { 
                _lineResponse = value; 
                OnPropertyChanged(nameof(LineResponse)); 
            }
        }

        public override LineArgsModel LineArgs 
        {
            get { return _lineArgs; }
            set { _lineArgs = value; OnPropertyChanged(nameof(LineArgs)); }
        }

        public override ObservableCollection<LineResponseModel> LineResponses
        {
            get { return _lineResponses; }
            set { _lineResponses = value; OnPropertyChanged(nameof(LineResponses)); }
        }

        /// <summary>
        /// -   Array of selected saved SOE response data objects in the DataGrid in ResultsView.xaml. Updated when a row is clicked in he DataGrid
        ///     via data binding.
        /// </summary>
        public override List<LineResponseModel> SelectedLines { get; set; } = new List<LineResponseModel>();

        public Commands.RelayCommand<object> UpdateSelectionCommand => new (async(grid) => await Commands.DataGridCommands.UpdateLineSelection(grid as DataGrid, this));

        public Commands.RelayCommand<object> DeleteItemsCommand => new (async(parms) => await Commands.DataGridCommands.DeleteLineItems(this));

        public Commands.RelayCommand<object> ClearItemsCommand => new (async(parms) => await Commands.DataGridCommands.ClearDataGridItems(this));

        public Commands.RelayCommand<object> SaveLineResultCommand => new ((grid) => Commands.GraphicsCommands.SaveLineResult(grid as DataGrid, this));

        public Commands.RelayCommand<object> ToggleMapToolSessionCommand => new ((startEnd) =>
        {
            if((string)startEnd=="start")//if start button is clicked
            {
                if (this.MapToolInfos.SessionEndActive == true || this.MapToolInfos.SessionActive == false)
                {
                    Utils.MapToolUtils.DeactivateSession(this, "end");
                    Utils.MapToolUtils.InitializeSession(this, "start");
                }
                else if(this.MapToolInfos.SessionActive == true)
                {
                    Utils.MapToolUtils.DeactivateSession(this, "start");
                }
            }
            if ((string)startEnd == "end")
            {
                if (this.MapToolInfos.SessionActive == true || this.MapToolInfos.SessionEndActive == false)
                {
                    Utils.MapToolUtils.DeactivateSession(this, "start");
                    Utils.MapToolUtils.InitializeSession(this, "end");
                }
                else if (this.MapToolInfos.SessionEndActive == true)
                {
                    Utils.MapToolUtils.DeactivateSession(this, "end");
                }
            }
           
        });
    }
}
