using MapAMilepost.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Models
{
    public class RRTInfo : ObservableObject
    {
        private string _title;
        private List<string> _relatedRouteQualifiers;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        public List<string> RelatedRouteQualifiers
        {
            get { return _relatedRouteQualifiers; }
            set
            {
                _relatedRouteQualifiers = value;
                OnPropertyChanged(nameof(RelatedRouteQualifiers));
            }
        }
    }
    public class RouteIDInfo : ObservableObject
    {
        private string _title;
        private List<RRTInfo> _relatedRouteTypes;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        public List<RRTInfo> RelatedRouteTypes
        {
            get { return _relatedRouteTypes; }
            set
            {
                _relatedRouteTypes = value;
                OnPropertyChanged(nameof(RelatedRouteTypes));
            }
        }
    }
}
