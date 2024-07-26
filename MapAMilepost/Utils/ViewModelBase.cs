using MapAMilepost.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
   

    public class ViewModelBase:INotifyPropertyChanged
    {
        public virtual MapToolInfo MapToolInfos { get; set; }
        public virtual string MapButtonLabel { get; set; }
        public virtual bool ShowResultsTable {  get; set; }
        public virtual SOEResponseModel SOEResponse {  get; set; }
        public virtual SOEArgsModel SOEArgs { get; set; }
        public virtual ObservableCollection<SOEResponseModel> SoePointResponses { get; set; }
        public virtual List<SOEResponseModel> SelectedItems { get; set; }
        public virtual MapAMilepostMaptool PointMapTool { get; set; }
        public virtual async void MapPoint2RoutePoint(object mapPoint) { }

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
