using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using MapAMilepost.Utils;

namespace MapAMilepost.Models
{
    /// <summary>
    /// -   SOE Response properties that are deserialized from the HTTP response, and 
    ///     used by the viewmodels.
    /// </summary>
    public class PointResponseModel : ObservableObject
    {
        private string _pointFeatureID;
        private double? _arm;
        private bool? _back;
        private bool? _decrease;
        private string _route;
        private double? _srmp;
        private string _referenceDate;
        private string _responseDate;
        private string _realignmentDate;
        private coordinatePair _routeGeometry;
        public  string PointFeatureID
        {
            get { return _pointFeatureID; }
            set
            {
                _pointFeatureID = value;
                OnPropertyChanged(nameof(PointFeatureID));
            }
        }
        public  double? Arm
        {
            get { return _arm; }
            set
            {
                _arm = value;
                OnPropertyChanged(nameof(Arm));
            }
        }
        public  bool? Back
        {
            get { return _back; }
            set
            {
                _back = value;
                OnPropertyChanged(nameof(Back));
            }
        }
        public  bool? Decrease
        {
            get { return _decrease; }
            set
            {
                _decrease = value;
                OnPropertyChanged(nameof(Decrease));
            }
        }
        public  string Route
        {
            get { return _route; }
            set
            {
                _route = value;
                OnPropertyChanged(nameof(Route));
            }
        }
        public double? Srmp
        {
            get { return _srmp; }
            set
            {
                _srmp = value;
                OnPropertyChanged(nameof(Srmp));
            }
        }
        public  string ReferenceDate
        {
            get { return _referenceDate; }
            set
            {
                _referenceDate = value;
                OnPropertyChanged(nameof(ReferenceDate));
            }
        }

        public  string ResponseDate
        {
            get { return _responseDate; }
            set
            {
                _responseDate = value;
                OnPropertyChanged(nameof(ResponseDate));
            }
        }
        public  string RealignmentDate
        {
            get { return _realignmentDate; }
            set
            {
                _realignmentDate = value;
                OnPropertyChanged(nameof(RealignmentDate));
            }
        }
        public  coordinatePair RouteGeometry
        {
            get { return _routeGeometry; }
            set
            {
                _routeGeometry = value;
                OnPropertyChanged(nameof(RouteGeometry));
            }
        }
        public class coordinatePair
        {
            public double x { get; set; }
            public double y { get; set; }
        }
    }

    public class LineResponseModel : ObservableObject
    {
        private PointResponseModel _startResponse;
        private PointResponseModel _endResponse;
        private string _FeatureID;
        public  PointResponseModel StartResponse
        {
            get { return _startResponse; }
            set
            {
                _startResponse = value;
                OnPropertyChanged(nameof(StartResponse));
            }
        }
        public  PointResponseModel EndResponse {
            get { return _endResponse; }
            set
            {
                _endResponse = value;
                OnPropertyChanged(nameof(EndResponse));
            }
        }
        public  string LineFeatureID
        {
            get { return _FeatureID; }
            set
            {
                _FeatureID = value;
                OnPropertyChanged(nameof(LineFeatureID));
            }
        }
        public LineResponseModel()
        {
            StartResponse = new PointResponseModel();
            EndResponse = new PointResponseModel();
        }
    }

    /// <summary>
    /// The subset of properties returned by the "Find Nearest Route Location" API endpoint,
    /// that are used in the "Find Route Locations" API Endpoint. 
    /// </summary>
    public class LocationInfo
    {
        public double? Id { get; set; }
        public string? Route { get; set; }
        public bool? Decrease { get; set; }
        public double? Arm { get; set; }
        public double? Srmp { get; set; }
        public bool? Back { get; set; }
        public string? ReferenceDate { get; set; }
        public string? ResponseDate { get; set; }
       // public bool? EndBack { get; set; }
        //public coordinatePair? EventPoint { get; set; }
        //public class coordinatePair
        //{
        //    public double? x { get; set; }
        //    public double? y { get; set; }
        //}

        /// <summary>
        /// -   Constructor to automatically assign the properties of the response object returned by the
        ///     "Find Nearest Route Location" request, to the new serialized object used to define the
        ///     URL parameters of the "Find Route Locations" request.
        /// </summary>
        /// <param name="FNRL"></param>
        public LocationInfo(PointResponseModel FNRL)
        {
            Route = FNRL.Route;
            Decrease = FNRL.Decrease;
            Arm = FNRL.Arm;
            Srmp = FNRL.Srmp;
            Back = FNRL.Back;
            ReferenceDate = FNRL.ReferenceDate;
            ResponseDate = FNRL.ResponseDate;
        }
    }
    public class ArmCalcInfo
    {
        public double? ArmCalcReturnCode { get; set; }
        public string ArmCalcReturnMessage { get; set; }
    }

    public class FRLLineGeometryModel : ObservableObject
    {
        private PathsArray? _routeGeometry;
        public PathsArray? RouteGeometry
        {
            get { return _routeGeometry; }
            set
            {
                _routeGeometry = value;
                OnPropertyChanged(nameof(RouteGeometry));
            }
        }
        public class PathsArray
        {
            public List<List<List<double>>> paths { get; set; }
        }
        public FRLLineGeometryModel()
        {
            
        }
    }
}

