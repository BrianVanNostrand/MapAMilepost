﻿using MapAMilepost.Utils;
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
    public class SoeArgsModel : ObservableObject
    {
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

        private string? _searchRadius;

        public string? SearchRadius
        {
            get { return _searchRadius; }
            set
            {
                _searchRadius = value;
                OnPropertyChanged(nameof(SearchRadius));
            }
        }

        private long _sR;

        public long SR//spatial reference
        {
            get { return _sR; }
            set
            {
                _sR = value;
                OnPropertyChanged(nameof(SR));
            }
        }

        private string? _searchRadiusUnits = "feet";

        public string? SearchRadiusUnits
        {
            get { return _searchRadiusUnits; }
            set
            {
                _searchRadiusUnits = value;
                OnPropertyChanged(nameof(SearchRadiusUnits));
            }
        }

        private double _x = 0;

        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        private double _y = 0;

        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public SoeArgsModel()//set default values in constructor
        {
            this._referenceDate = $"{DateTime.Now.ToString("M/d/yyyy")}";
            this._searchRadius = "200000";
        }
    }
}
//https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations?f=json&referenceDate=5%2F8%2F2024&coordinates=%5B-13644740.56427878%2C5960013.904550078%5D&searchRadius=200&inSR=102100&outSR=102100&lrsYear=&routeFilter=

//https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations?f=json&referenceDate=5%2F8%2F2024&coordinates=%5B-13589075.36974272%2C6032781.928509494%5D&searchRadius=200&inSR=102100&outSR=102100&lrsYear=&routeFilter=

//https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations?f=json&referenceDate=5%2F8%2F2024&coordinates=%5B-13617068.174226709%2C6061177.441105765%5D&searchRadius=200&inSR=102100&outSR=102100&lrsYear=&routeFilter=