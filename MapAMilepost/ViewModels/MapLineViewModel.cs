﻿using MapAMilepost.Models;
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
        private SoeResponseModel _SoeStartResponse;
        private SoeResponseModel _soeEndResponse;
        private SoeArgsModel _soeStartArgs;
        private SoeArgsModel _soeEndArgs;
        private ObservableCollection<SoeResponseModel> _soeLineResponses;
        private bool _showResultsTable = false;
        private MapToolInfo _mapToolInfos;
        public MapLineViewModel()//constructor
        {
            _SoeStartResponse = new SoeResponseModel();
            _soeEndResponse = new SoeResponseModel();
            _soeStartArgs = new SoeArgsModel();
            _soeEndArgs = new SoeArgsModel();
            _soeLineResponses = new ObservableCollection<SoeResponseModel>();
            _mapToolInfos = new MapToolInfo
            {
                SessionActive = false,
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
        public SoeResponseModel SoeStartResponse
        {
            get { return _SoeStartResponse; }
            set { _SoeStartResponse = value; OnPropertyChanged(nameof(SoeStartResponse)); }
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

        public Commands.RelayCommand<object> ToggleMapToolSessionCommand => new Commands.RelayCommand<object>((direction) =>
        {
            if (!this.MapToolInfos.SessionActive)
            {
                Utils.MapToolUtils.InitializeSession(this, (string)direction);
            }
            else
            {
                Utils.MapToolUtils.DeactivateSession(this);
            }
        });
    }
}
