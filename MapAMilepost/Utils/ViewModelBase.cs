﻿using MapAMilepost.Models;
using MapAMilepost;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Security.Policy;

namespace MapAMilepost.Utils
{


    public class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase() {
            MappingTool = new MapAMilepostMaptool();
            Directions = ["Increasing","Decreasing"];
            AheadBack = [true, false];
        }
        public ObservableCollection<string> Directions { get; set; }
        public ObservableCollection<bool> AheadBack { get; set; }
        public virtual bool IsMapMode { get; set; } 
        public MapAMilepostMaptool MappingTool { get; set; }
        public virtual bool SRMPIsSelected { get; set; }
        public virtual bool SessionActive { get; set; }
        public virtual bool SessionEndActive { get; set; }
        public virtual bool ShowResultsTable { get; set; }
        public virtual PointResponseModel PointResponse { get; set; } //used for point sessions
        public virtual LineResponseModel LineResponse { get; set; } // used for lines
        public virtual PointArgsModel PointArgs { get; set; } //used for both points and lines
        public virtual LineArgsModel LineArgs { get; set; }
        public virtual ObservableCollection<PointResponseModel> PointResponses { get; set; }
        public virtual ObservableCollection<LineResponseModel> LineResponses { get; set; }
        public virtual List<PointResponseModel> SelectedPoints { get; set; }
        public virtual List<LineResponseModel> SelectedLines { get; set; }
        public virtual ObservableCollection<RouteIDInfo> RouteIDInfos { get; set; }
        public virtual ObservableCollection<string> RouteQualifiers { get; set; }
        public virtual int RouteComboIndex {  get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
