using MapAMilepost.Models;
using MapAMilepost;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace MapAMilepost.Utils
{


    public class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase() {
            MappingTool = new MapAMilepostMaptool();
        }
        public MapAMilepostMaptool MappingTool { get; set; }
        public virtual MapToolInfo MapToolInfos { get; set; }
        public virtual bool ShowResultsTable { get; set; }
        public virtual SoeResponseModel SoeResponse { get; set; } //used for both points and lines
        public virtual SoeResponseModel SoeEndResponse { get; set; } //used for lines
        public virtual SoeArgsModel SoeArgs { get; set; } //used for both points and lines
        public virtual SoeArgsModel SoeEndArgs { get; set; } //used for lines
        public virtual ObservableCollection<SoeResponseModel> SoeResponses { get; set; }
        public virtual List<SoeResponseModel> SelectedItems { get; set; }
        
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
