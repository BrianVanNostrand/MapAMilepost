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
    public class LoadFileRowError : ObservableObject
    {
        private List<int> _routeRows = new();
        private List<int> _srmpRows = new();
        private List<int> _armRows = new();
        private List<int> _backRows = new();
        private List<int> _directionRows = new();
        private List<int> _refDateRows = new();
        private List<int> _resDateRows = new();

        public List<int> RouteRows
        {
            get { return _routeRows; }
            set 
            { 
                _routeRows = value;
                OnPropertyChanged(nameof(RouteRows));
            }
        }
        public List<int> SRMPRows
        {
            get { return _srmpRows; }
            set
            {
                _srmpRows = value;
                OnPropertyChanged(nameof(SRMPRows));
            }
        }
        public List<int> ARMRows
        {
            get { return _armRows; }
            set
            {
                _armRows = value;
                OnPropertyChanged(nameof(ARMRows));
            }
        }
        public List<int> BackRows
        {
            get { return _backRows; }
            set
            {
                _backRows = value;
                OnPropertyChanged(nameof(BackRows));
            }
        }
        public List<int> DirectionRows
        {
            get { return _directionRows; }
            set
            {
                _directionRows = value;
                OnPropertyChanged(nameof(DirectionRows));
            }
        }
        public List<int> RefDateRows
        {
            get { return _refDateRows; }
            set
            {
                _refDateRows = value;
                OnPropertyChanged(nameof(RefDateRows));
            }
        }
        public List<int> ResDateRows
        {
            get { return _resDateRows; }
            set
            {
                _resDateRows = value;
                OnPropertyChanged(nameof(ResDateRows));
            }
        }
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
