using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Models
{
    public class BooleanWithError
    {
        public bool BoolVal { get; set; }
        public bool Error { get; set; }
    }
    public class StringWithError
    {
        public string StringVal { get; set; }
        public bool Error { get; set; }
    }
    public class TableFormInfoModel : ObservableObject
    {
        private string _routeColumn;
        private string _sRMPColumn;
        private string _aRMColumn;
        private string _backColumn;
        private string _directionColumn;
        private string _referenceDateColumn;
        private string _responseDateColumn;
        public string RouteColumn
        {
            get { return _routeColumn; }
            set
            {
                _routeColumn = value;
                OnPropertyChanged(nameof(RouteColumn));
            }
        }
        public string SRMPColumn
        {
            get { return _sRMPColumn; }
            set
            {
                _sRMPColumn = value;
                OnPropertyChanged(nameof(SRMPColumn));
            }
        }
        public string ARMColumn
        {
            get { return _aRMColumn; }
            set
            {
                _aRMColumn = value;
                OnPropertyChanged(nameof(ARMColumn));
            }
        }
        public string BackColumn
        {
            get { return _backColumn; }
            set
            {
                _backColumn = value;
                OnPropertyChanged(nameof(BackColumn));
            }
        }
        public string DirectionColumn
        {
            get { return _directionColumn; }
            set
            {
                _directionColumn = value;
                OnPropertyChanged(nameof(DirectionColumn));
            }
        }
        public string ReferenceDateColumn
        {
            get { return _referenceDateColumn; }
            set { 
                _referenceDateColumn = value;
                OnPropertyChanged(nameof(ReferenceDateColumn));
            }
        }
        public string ResponseDateColumn
        {
            get { return _responseDateColumn; }
            set
            {
                _responseDateColumn = value;
                OnPropertyChanged(nameof(ResponseDateColumn));
            }
        }
    }
}
