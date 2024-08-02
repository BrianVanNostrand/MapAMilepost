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
    public class SoeResponseModel : ObservableObject
    {

        private double? _angle;
        public double? Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                OnPropertyChanged(nameof(Angle));
            }
        }

        private double? _arm;
        public double? Arm
        {
            get { return _arm; }
            set
            {
                _arm = value;
                OnPropertyChanged(nameof(Arm));
            }
        }

        private bool? _back;
        public bool? Back
        {
            get { return _back; }
            set
            {
                _back = value;
                OnPropertyChanged(nameof(Back));
            }
        }

        private bool? _endback;
        public bool? EndBack
        {
            get { return _endback; }
            set
            {
                _endback = value;
                OnPropertyChanged(nameof(EndBack));
            }
        }

        private bool? _decrease;

        public bool? Decrease
        {
            get { return _decrease; }
            set
            {
                _decrease = value;
                OnPropertyChanged(nameof(Decrease));
            }
        }

        private double? _distance;

        public double? Distance
        {
            get { return _distance; }
            set
            {
                _distance = value;
                OnPropertyChanged(nameof(Distance));
            }
        }

        private string? _route;

        public string? Route
        {
            get { return _route; }
            set
            {
                _route = value;
                OnPropertyChanged(nameof(Route));
            }
        }

        private double? _srmp;

        public double? Srmp
        {
            get { return _srmp; }
            set
            {
                _srmp = value;
                OnPropertyChanged(nameof(Srmp));
            }
        }

        private string? _referenceDate;
        public string? ReferenceDate
        {
            get { return _referenceDate; }
            set
            {
                _referenceDate = value;
                OnPropertyChanged(nameof(ReferenceDate));
            }
        }

        private string? _responseDate;
        public string? ResponseDate
        {
            get { return _responseDate; }
            set
            {
                _responseDate = value;
                OnPropertyChanged(nameof(ResponseDate));
            }
        }

        private string? _realignmentDate;
        public string? RealignmentDate
        {
            get { return _realignmentDate; }
            set
            {
                _realignmentDate = value;
                OnPropertyChanged(nameof(RealignmentDate));
            }
        }
        private coordinatePair? _routeGeometry;
        public coordinatePair? RouteGeometry
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
    public class FRLRequestObject
    {
        public double? Id { get; set; }
        public string? Route { get; set; }
        public bool? Decrease { get; set; }
        public double? Arm { get; set; }
        public double? Srmp { get; set; }
        public bool? Back { get; set; }
        public string? ReferenceDate { get; set; }
        public string? ResponseDate { get; set; }
        public bool? EndBack { get; set; }
        public coordinatePair? EventPoint { get; set; }
        public class coordinatePair
        {
            public double? x { get; set; }
            public double? y { get; set; }
        }
        public double? Distance { get; set; }
        public FRLRequestObject(SoeResponseModel FNRL)
        {
            Route = FNRL.Route;
            Decrease = FNRL.Decrease;
            Arm = FNRL.Arm;
            Srmp = FNRL.Srmp;
            Back = FNRL.Back;
            ReferenceDate = FNRL.ReferenceDate;
            ResponseDate = FNRL.ResponseDate;
            EndBack = FNRL.EndBack;
            EventPoint = new coordinatePair();
            EventPoint.x = FNRL.RouteGeometry.x;
            EventPoint.y = FNRL.RouteGeometry.y;
        }
    }
}
