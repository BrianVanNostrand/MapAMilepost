using MapAMilepost.Models;
using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace MapAMilepost.ViewModels
{
    public class MapLineViewModel:ViewModelBase
    {
        private SOEResponseModel _soeStartResponse;
        private SOEResponseModel _soeEndResponse;
        private SOEArgsModel _soeStartArgs;
        private SOEArgsModel _soeEndArgs;
        private ICommand _updateSOEStartEndResponse;
        private ICommand _updateSOEStartEndArgs;
        private ICommand _saveLineResultCommand;
        private MapAMilepostMaptool _lineMapTool;
        private List<List<SOEResponseModel>> _soeLineResponses;
        public MapLineViewModel()//constructor
        {
            _soeStartResponse = new SOEResponseModel();
            _soeEndResponse = new SOEResponseModel();
            _soeStartArgs = new SOEArgsModel();
            _soeEndArgs = new SOEArgsModel();
            _soeLineResponses = new List<List<SOEResponseModel>>();
        }
        public List<List<SOEResponseModel>> SoeLineResponses
        {
            get { return _soeLineResponses; }
            set { _soeLineResponses = value; OnPropertyChanged("SoeLineResponses"); }
        }
        public SOEResponseModel SOEStartResponse
        {
            get { return _soeStartResponse; }
            set { _soeStartResponse = value; OnPropertyChanged("SOEStartResponse"); }
        }
        public SOEResponseModel SOEEndResponse
        {
            get { return _soeEndResponse; }
            set { _soeEndResponse = value; }
        }
        public SOEArgsModel SOEStartArgs
        {
            get { return _soeStartArgs; }
            set { _soeStartArgs = value; }
        }
        public SOEArgsModel SOEEndArgs
        {
            get { return _soeEndArgs; }
            set { _soeEndArgs = value; }
        }
        public MapAMilepostMaptool LineMapTool
        {
            get { return _lineMapTool; }
            set { _lineMapTool = value; }
        }
        public ICommand UpdateSOEStartEndResponseCommand
        {
            get
            {
                return null;
                //if (_updateSOEStartEndResponse == null)
                //    _updateSOEStartEndResponse = new Commands.RelayCommand(param => this.SubmitStartEnd(param),
                //        null);
                //return _updateSOEStartEndResponse;
            }
            set
            {
                _updateSOEStartEndResponse = value;
            }
        }
        public ICommand SaveLineResultCommand
        {
            get
            {
                return null;
                //if (_saveLineResultCommand == null)
                //    _saveLineResultCommand = new Commands.RelayCommand(SaveLineResult,
                //        null);
                //return _saveLineResultCommand;
            }
            set
            {
                _saveLineResultCommand = value;
            }
    }
        public async void SubmitStartEnd(object param)
        {
            if (param.ToString() == "start")
            {
                object response = await Utils.HTTPRequest.QuerySOE(SOEStartArgs);
                if (response != null)
                {
                    if (Utils.SOEResponseUtils.HasBeenUpdated(SOEEndResponse))
                    {
                        if (SOEStartResponse.Route == SOEEndResponse.Route)
                        {
                            SOEResponseUtils.CopyProperties(response, SOEStartResponse);
                            //createLine
                        }
                        else
                        {
                            MessageBox.Show("Start and end points must be on the same route.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                    else
                    {
                        SOEResponseUtils.CopyProperties(response, SOEStartResponse);
                    }
                }
                else
                {
                    MessageBox.Show("Could not find nearest route.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
            else if (param.ToString() == "end")
            {
                object response = await Utils.HTTPRequest.QuerySOE(SOEEndArgs);
                if (response != null)
                {
                    if (Utils.SOEResponseUtils.HasBeenUpdated(SOEStartResponse))
                    {
                        if (SOEStartResponse.Route == SOEEndResponse.Route)
                        {
                            SOEResponseUtils.CopyProperties(response, SOEEndResponse);
                            //create line
                        }
                        else
                        {
                            MessageBox.Show("Start and end points must be on the same route.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                    else
                    {
                        SOEResponseUtils.CopyProperties(response, SOEEndResponse);
                    }
                }
                else
                {
                    MessageBox.Show("Could not find nearest route.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }
        public void SaveLineResult(object state)
        {
            if (Utils.SOEResponseUtils.HasBeenUpdated(SOEStartResponse) && Utils.SOEResponseUtils.HasBeenUpdated(SOEEndResponse))
            {
                // SoeLineResponses.Add({ SOEStartResponse, SOEEndResponse});
            }
        }
    }
}
