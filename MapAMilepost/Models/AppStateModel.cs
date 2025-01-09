using ArcGIS.Desktop.Mapping;
using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Models
{
    public class AppStateModel : ObservableObject
    {
        private bool _appReady;
        private bool _mapViewPaneSelected;
        private GraphicsLayer _layer;
        public bool MapViewPaneSelected
        {
            get { return _mapViewPaneSelected; }
            set
            {
                _mapViewPaneSelected = value;
                OnPropertyChanged(nameof(MapViewPaneSelected));
            }
        }
        public GraphicsLayer Layer
        {
            get { return _layer; }
            set
            {
                _layer = value;
                OnPropertyChanged(nameof(Layer));
            }
        }
        public bool AppReady
        {
            get { return _appReady; }
            set
            {
                _appReady = value;
                OnPropertyChanged(nameof(AppReady));
            }
        }
        public AppStateModel()
        {

            _appReady = false;
            _layer = null;
        }
    }
    public class ErrorModalModel : ObservableObject
    {
        private string _title;
        private string _caption;
        private bool _showButton;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        public string Caption
        {
            get { return _caption; }
            set
            {
                _caption = value;
                OnPropertyChanged(nameof(Caption));
            }
        }
        public bool ShowButton
        {
            get { return _showButton; }
            set
            {
                _showButton = value;
                OnPropertyChanged(nameof(ShowButton));
            }
        }
        public ErrorModalModel()
        {
            Title = "Error";
            Caption = "Error";
            ShowButton = false;
        }
    }
}
