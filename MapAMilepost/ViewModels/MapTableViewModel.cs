using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Framework;
using ArcGIS.Desktop.Internal.Mapping.Locate;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Windows;
using static ArcGIS.Desktop.Internal.Mapping.Symbology.SymbolUtil;

namespace MapAMilepost.ViewModels
{
    public class MapTableViewModel : ViewModelBase
    {
        private List<LineResponseModel> _lineInfos = new();
        private ObservableCollection<PointResponseModel> _pointInfos = new();
        private PointResponseModel _pointFormInfo;
        private LineResponseModel _lineFormInfo;
        private ObservableCollection<string> _columnTitles;
        private string _warningMessage;
        private string _selectedFile;
        private bool _fileSelected = false;
        private bool _srmpIsSelected = true;
        private bool _dataLoaded = false;
        private TableFormInfoModel _formInfo = new();
        private LoadFileRowError _fileLoadError = new();

        public LoadFileRowError FileLoadError
        {
            get { return _fileLoadError; }
            set
            {
                _fileLoadError = value;
                OnPropertyChanged(nameof(FileLoadError));
            }
        }
        public string WarningMessage
        {
            get { return _warningMessage; }
            set
            {
                _warningMessage = value;
                OnPropertyChanged(nameof(WarningMessage));
            }
        }
        public TableFormInfoModel FormInfo
        {
            get { return _formInfo; }
            set
            {
                _formInfo = value;
                OnPropertyChanged(nameof(FormInfo));
            }
        }
        public override bool SRMPIsSelected
        {
            get { return _srmpIsSelected; }
            set
            {
                _srmpIsSelected = value;
                OnPropertyChanged(nameof(SRMPIsSelected));
            }
        }
        public List<LineResponseModel> LineInfos
        {
            get { return _lineInfos; }
            set
            {
                _lineInfos = value;
                OnPropertyChanged(nameof(LineInfos));
            }
        }
        public ObservableCollection<PointResponseModel> PointInfos
        {
            get { return _pointInfos; }
            set
            {
                _pointInfos = value;
                OnPropertyChanged(nameof(PointInfos));
            }
        }
        public ObservableCollection<string> ColumnTitles
        {
            get { return _columnTitles; }
            set
            {
                _columnTitles = value;
                OnPropertyChanged(nameof(ColumnTitles));
            }
        }
        public string SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
            }
        }
        public bool DataLoaded
        {
            get { return _dataLoaded; }
            set
            {
                _dataLoaded = value;
                OnPropertyChanged(nameof(DataLoaded));
            }
        }
        public bool FileSelected
        {
            get { return _fileSelected; }
            set
            {
                _fileSelected = value;
                OnPropertyChanged(nameof(FileSelected));
            }
        }
        public Commands.RelayCommand<object> ChangeMPTypeCommand => new((val) =>
        {
            if ((string)val == "SRMP")
            {
                SRMPIsSelected = true;
                FormInfo = new()
                {
                    RouteColumn = FormInfo.RouteColumn,
                    BackColumn = FormInfo.BackColumn,
                    DirectionColumn = FormInfo.DirectionColumn,
                    ReferenceDateColumn = FormInfo.ReferenceDateColumn,
                    ResponseDateColumn = FormInfo.ResponseDateColumn
                };
            }
            else
            {
                SRMPIsSelected = false;
                FormInfo = new()
                {
                    RouteColumn = FormInfo.RouteColumn,
                    BackColumn = FormInfo.BackColumn,
                    DirectionColumn = FormInfo.DirectionColumn,
                    ReferenceDateColumn = FormInfo.ReferenceDateColumn,
                    ResponseDateColumn = FormInfo.ResponseDateColumn
                };
            }
        });
        public Commands.RelayCommand<object> ClearCommand => new((p) =>
        {
            WarningMessage = string.Empty;
            PointInfos.Clear();
            DataLoaded = false;
        });
        public Commands.RelayCommand<object> OpenFileCommand => new((p) =>
        {
            var openDlg = new OpenItemDialog
            {
                Title = "Select a Feature Class",
                InitialLocation = @"C:\Data",
                MultiSelect = false,
                BrowseFilter = BrowseProjectFilter.GetFilter(ArcGIS.Desktop.Catalog.ItemFilters.Tables_All)
            };
            bool? ok = openDlg.ShowDialog();
            if (!ok.HasValue || openDlg.Items.Count() == 0)
                return;   //nothing selected
            if (!(openDlg.Items.First().TypeID == "text_csv"))
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Only CSV files are currently supported.");
                return;
            }
            SelectedFile = openDlg.Items.First().Path;
        });
        public Commands.RelayCommand<object> FileNameChanged => new((p) =>
        {
            WarningMessage = String.Empty;
            DataLoaded = false;
            if (File.Exists(SelectedFile))
            {
                ColumnTitles = [];
                string[] headers = Utils.TableUtils.getCSVHeaders(SelectedFile);
                if(headers!=null&& headers.Length > 0)
                {
                    foreach (string header in headers)
                    {
                        ColumnTitles.Add(header);
                    }
                    FileSelected = true;
                }
            }
            else
            {
                FileSelected = false;
            }
        });
        public Commands.RelayCommand<object> LoadFileCommand => new((p) =>
        {
            WarningMessage = String.Empty;
            DataLoaded = false;
            PointInfos.Clear();
            List<string> missingHeadersRequired = new List<string>();
            List<string> missingHeadersOptional = new List<string>();

            if (FormInfo.RouteColumn == null)
            {
                missingHeadersRequired.Add("Route");
            }
            if (FormInfo.ARMColumn == null && FormInfo.SRMPColumn == null)
            {
                missingHeadersRequired.Add("Milepost (SRMP or ARM)");
            }
            if (missingHeadersRequired.Count > 0)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"The following columns must be defined in order to load this file:\n{string.Join(", ", missingHeadersRequired)}");
                return;
            }
            if (FormInfo.BackColumn == null)
            {
                WarningMessage = "- Back column undefined. Milepost values will be assumed to be forward miles.";
            }
            if (FormInfo.DirectionColumn == null)
            {
                WarningMessage = WarningMessage + "\n- Direction column undefined. All milepost values will be placed on increasing lane.";
            }
            if (FormInfo.ReferenceDateColumn == null)
            {
                WarningMessage = WarningMessage + "\n- Reference Date column undefined. Today's date will be used.";
            }
            if (FormInfo.ResponseDateColumn == null)
            {
                WarningMessage = WarningMessage + "\n- Response Date column undefined. Today's date will be used.";
            }
            List<TableFormInfoModel> records = Utils.TableUtils.loadCSV(SelectedFile,FormInfo);
            FileLoadError = new();
            int rowIndex = 0;
            foreach (var record in records)
            {
                PointResponseModel pointInfo = new();
                if (record != null)
                {
                    if (SRMPIsSelected)
                    {
                        if (double.TryParse(record.SRMPColumn, out double result))
                        {
                            pointInfo.Srmp = result;
                        }
                        else
                        {
                            FileLoadError.SRMPRows.Add(rowIndex);
                        }
                    }
                    else
                    {
                        if (double.TryParse(record.ARMColumn, out double result))
                        {
                            pointInfo.Arm = result;
                        }
                        else
                        {
                            FileLoadError.ARMRows.Add(rowIndex);
                        }
                    }
                    string routeString = record.RouteColumn;
                    if (routeString.Length < 3)
                    {
                        while (routeString.Length < 3)
                        {
                            routeString = "0" + routeString;
                        }
                    }
                    pointInfo.Route = routeString;
                    #region back parsing
                    if (FormInfo.BackColumn != null)
                    {
                        BooleanWithError val = TableUtils.getBack(record.BackColumn);
                        if (val.Error == false)
                        {
                            pointInfo.Back = val.BoolVal;
                        }
                        else
                        {
                            FileLoadError.BackRows.Add(rowIndex);
                        }
                    }
                    else
                    {
                        pointInfo.Back = false; //default value
                    }
                    #endregion
                    #region Direction Parsing
                    if (FormInfo.DirectionColumn != null)
                    {
                        BooleanWithError val = TableUtils.getDirection(record.DirectionColumn);
                        if (val.Error == false)
                        {
                            pointInfo.Decrease = val.BoolVal;
                        }
                        else
                        {
                            FileLoadError.DirectionRows.Add(rowIndex);
                        }
                    }
                    else
                    {
                        pointInfo.Decrease = false; //default value
                    }
                    #endregion
                    #region Reference Date Parsing
                    if (FormInfo.ReferenceDateColumn != null)
                    {
                        StringWithError val = TableUtils.getDate(record.ReferenceDateColumn);
                        if (val.Error == false)
                        {
                            pointInfo.ReferenceDate = val.StringVal;
                        }
                        else
                        {
                            FileLoadError.RefDateRows.Add(rowIndex);
                        }
                    }
                    else
                    {
                        pointInfo.ReferenceDate = DateTime.Now.ToString("MM/dd/yyyy"); //default value
                    }
                    #endregion
                    #region Response Date Parsing
                    if (FormInfo.ResponseDateColumn != null)
                    {
                        StringWithError val = TableUtils.getDate(record.ResponseDateColumn);
                        if (val.Error == false)
                        {
                            pointInfo.ResponseDate = val.StringVal;
                        }
                        else
                        {
                            FileLoadError.ResDateRows.Add(rowIndex);
                        }
                    }
                    else
                    {
                        pointInfo.ResponseDate = DateTime.Now.ToString("MM/dd/yyyy"); //default value
                    }
                    #endregion
                }
                PointInfos.Add(pointInfo);
            }
            DataLoaded = true;
        });
        private static void getTableValue(dynamic record, string columnName)
        {
            var test = JObject.Parse(record);
            string value1 = record[columnName];
            var propertyInfo = record.GetType().GetProperty(columnName);
            var value = propertyInfo.GetValue(record, null);
        }
        public Commands.RelayCommand<object> RowEditEnding => new(async (p) => {
            Console.Write(FileLoadError);
            Console.Write(p);
        });

        public Commands.RelayCommand<object> CreatePointsCommand => new(async(p) => {
            GraphicsLayer graphicsLayer = await Utils.MapViewUtils.GetMilepostMappingLayer(MapView.Active.Map);//look for layer
            List<CIMGraphic> SavedPointGraphics = new();
            List<ElementInfo> ElementInfos = new();
            for (int i = PointInfos.Count - 1; i >= 0; i--)
            {
                LocationInfo Location = new(PointInfos[i]);
                PointResponseModel newPointResponse = await Utils.HTTPRequest.FindRouteLocation(Location, PointArgs);
                if (newPointResponse != null && newPointResponse.RouteGeometry != null)
                {
                    GraphicInfoModel graphicInfo = await Commands.GraphicsCommands.CreateTablePointGraphic(PointArgs, newPointResponse,"point");
                    SavedPointGraphics.Add(graphicInfo.CGraphic);
                    ElementInfos.Add(graphicInfo.EInfo);
                    PointInfos.RemoveAt(i);
                }
            }
            if(SavedPointGraphics.Count > 0)
            {
                await QueuedTask.Run(() =>
                    {
                        graphicsLayer.AddElements(graphics:SavedPointGraphics, elementInfos:ElementInfos,select:false);
                    }
                );
            }
            if (PointInfos.Count == 0)
            {
                DataLoaded = false;
                WarningMessage = string.Empty;
            }
        });
        public Commands.RelayCommand<object> CreateLinesCommand => new((p) => {
            Console.Write(LineInfos);
        });
        public MapTableViewModel()
        {
            ColumnTitles = [];
        }
    }
}
