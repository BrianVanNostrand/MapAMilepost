using MapAMilepost.Models;
using MapAMilepost.Utils;
using System.Windows.Input;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Controls;
using ArcGIS.Desktop.Mapping;
using System;
using System.Threading.Tasks;
using ArcGIS.Core.Data;

namespace MapAMilepost.ViewModels
{
    public class MapLineViewModel:ViewModelBase
    {
        private LineResponseModel _lineResponse = new();
        private LineArgsModel _lineArgs = new();
        private bool _isEnabled = false;
        private ObservableCollection<LineResponseModel> _lineResponses = new();
        private bool _showResultsTable = true;
        private bool _sessionActive = false;
        private bool _sessionEndActive = false;
        private bool _isMapMode = false;
        public MapLineViewModel()//constructor
        {
            MappingTool = new MapAMilepostMaptool();
        }
        public bool isEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(isEnabled));
            }
        }
        public override bool SessionActive 
        { 
            get { return _sessionActive; }
            set
            {
                _sessionActive = value;
                OnPropertyChanged(nameof(SessionActive));
            }
        }
        public override bool SessionEndActive
        {
            get { return _sessionEndActive; }
            set
            {
                _sessionEndActive = value;
                OnPropertyChanged(nameof(SessionEndActive));
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

        /// <summary>
        /// -   Indicates whether selected mode is "map click" or not.
        /// </summary>
        public override bool IsMapMode
        {
            get { return _isMapMode; }
            set { _isMapMode = value; OnPropertyChanged(nameof(IsMapMode)); }
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

        public Commands.RelayCommand<object> ChangeModeCommand => new((param) =>
        {
            IsMapMode = !IsMapMode;
        });

        public Commands.RelayCommand<object> ClearItemsCommand => new(async (parms) => {
            if (MapView.Active != null && MapView.Active.Map != null)
            {
                await Commands.DataGridCommands.ClearDataGridItems(this);
            }
            else
            {
                MessageBox.Show("Please switch to a map view before attempting to clear mileposts.");
            }
        });

        public Commands.RelayCommand<object> SaveLineResultCommand => new ((grid) => Commands.GraphicsCommands.SaveLineResult(grid as DataGrid, this));

        public Commands.RelayCommand<object> InteractionCommand => new (async(startEnd) =>
        {
            if (IsMapMode)
            {
                await ToggleSession((string)startEnd);
            }
            else
            {
                Console.WriteLine(LineResponse);
                //bool formDataValid = HTTPRequest.CheckFormData(LineResponse);
                //LocationInfo formLocation = new LocationInfo(LineResponse);
                //CurrentViewModel.LineResponse.StartResponse = (await Utils.HTTPRequest.QuerySOE(mapPoint, CurrentViewModel.LineArgs.StartArgs) as PointResponseModel);
                //lineGeometryResponse = await Utils.MapToolUtils.GetLine(CurrentViewModel.LineResponse.StartResponse, CurrentViewModel.LineResponse.EndResponse, CurrentViewModel.LineArgs.StartArgs.SR, CurrentViewModel.LineArgs.StartArgs.ReferenceDate);
                //await Commands.GraphicsCommands.CreatePointGraphics(CurrentViewModel.LineArgs.StartArgs, CurrentViewModel.LineResponse.StartResponse, MapToolSessionType);
            }

        });

        private async Task ToggleSession(string startEnd)
        {
            if (startEnd == "start")//if start button is clicked
            {
                if (this.SessionEndActive == true || this.SessionActive == false)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "end");
                    await Utils.MapToolUtils.InitializeSession(this, "start");
                }
                else if (this.SessionActive == true)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "start");
                }
            }
            if (startEnd == "end")
            {
                if (this.SessionActive == true || this.SessionEndActive == false)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "start");
                    await Utils.MapToolUtils.InitializeSession(this, "end");
                }
                else if (this.SessionEndActive == true)
                {
                    await Utils.MapToolUtils.DeactivateSession(this, "end");
                }
            }
        }
    }
}
