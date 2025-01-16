using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Models
{
    /// <summary>
    /// -   SOE Arguments passed to the SOE HTTP get request
    /// </summary>
    public class PointArgsModel : ObservableObject
    {
        private string? _referenceDate;

        public string? ReferenceDate //reassigned by main viewmodel
        {
            get { return _referenceDate; }
            set
            {
                _referenceDate = value;
                OnPropertyChanged(nameof(ReferenceDate));
            }
        }

        private string? _responseDate;
        public string? ResponseDate //reassigned by main viewmodel
        {
            get { return _responseDate; }
            set
            {
                _referenceDate = value;
                OnPropertyChanged(nameof(ResponseDate));
            }
        }

        private string? _searchRadius;

        public string? SearchRadius //reassigned by main viewmodel
        {
            get { return _searchRadius; }
            set
            {
                _searchRadius = value;
                OnPropertyChanged(nameof(SearchRadius));
            }
        }

        private long _sR;

        public long SR//spatial reference - reassigned by main viewmodel
        {
            get { return _sR; }
            set
            {
                _sR = value;
                OnPropertyChanged(nameof(SR));
            }
        }

        private string? _searchRadiusUnits = "feet";

        public string? SearchRadiusUnits//reassigned by main viewmodel
        {
            get { return _searchRadiusUnits; }
            set
            {
                _searchRadiusUnits = value;
                OnPropertyChanged(nameof(SearchRadiusUnits));
            }
        }

        private double _x = 0;

        public double X//assigned when a point is clicked (mapped) on a map
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        private double _y = 0;

        public double Y//assigned when a point is clicked (mapped) on a map
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }
        private double _zoomScale = 0;

        public double ZoomScale //reassigned by main viewmodel
        {
            get { return _zoomScale; }
            set
            {
                _zoomScale = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public PointArgsModel(string searchRadius = null)//set default values in constructor
        {
            this._referenceDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
            this._responseDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
            this._searchRadius = "2000";
            if (searchRadius!= null)
            {
                this._searchRadius = searchRadius;
            }
        }
    }

    public class LineArgsModel
    {
        public PointArgsModel StartArgs { get; set; }
        public PointArgsModel EndArgs { get; set; }
        public LineArgsModel(string startSearchRadius = null, string endSearchRadius = null)
        {
            StartArgs = new PointArgsModel(startSearchRadius);
            EndArgs = new PointArgsModel(endSearchRadius);
        }
    }

    /// <summary>
    /// The subset of properties returned by the "Find Route Locations" API endpoint,
    /// that are used in a subsequent "Find Route Locations" API call, to retrieve line
    /// geometry. 
    /// </summary>
    public class LineURLParamsModel
    {
        public string Route { get; set; }
        public bool? Decrease { get; set; }
        public double? Srmp { get; set; }
        public bool? Back { get; set; }
        public string ReferenceDate { get; set; }
        public double? EndSrmp { get; set; }
        public bool? EndBack { get; set; }

    }
}
//https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations?f=json&referenceDate=5%2F8%2F2024&coordinates=%5B-13644740.56427878%2C5960013.904550078%5D&searchRadius=200&inSR=102100&outSR=102100&lrsYear=&routeFilter=

//https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations?f=json&referenceDate=5%2F8%2F2024&coordinates=%5B-13589075.36974272%2C6032781.928509494%5D&searchRadius=200&inSR=102100&outSR=102100&lrsYear=&routeFilter=

//https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations?f=json&referenceDate=5%2F8%2F2024&coordinates=%5B-13617068.174226709%2C6061177.441105765%5D&searchRadius=200&inSR=102100&outSR=102100&lrsYear=&routeFilter=