using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Internal.Framework;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Windows;

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
            WarningMessage = "";
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
            WarningMessage = "";
            DataLoaded = false;
            if (File.Exists(SelectedFile))
            {
                ColumnTitles = [];
                FileSelected = true;
                DataTable file = Utils.TableUtils.readCSV(SelectedFile);
                Console.Write(file);
                foreach (DataColumn column in file.Columns)
                {
                    ColumnTitles.Add((column.ColumnName).ToString());
                }
            }
            else
            {
                FileSelected = false;
            }
        });
        public Commands.RelayCommand<object> LoadFileCommand => new((p) =>
        {
            WarningMessage = "";
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
            if(missingHeadersRequired.Count > 0)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"The following columns must be defined in order to load this file:\n{string.Join(", ", missingHeadersRequired)}");
                return;
            }
            if (FormInfo.BackColumn == null)
            {
                WarningMessage = "- Back column undefined. Milepost values will be assumed to be forward miles.";
            }
            if(FormInfo.DirectionColumn == null)
            {
                WarningMessage = WarningMessage + "\n- Direction column undefined. All milepost values will be placed on increasing lane.";
            }
            if(FormInfo.ReferenceDateColumn == null)
            {
                WarningMessage = WarningMessage + "\n- Reference Date column undefined. Today's date will be used.";
            }
            if (FormInfo.ResponseDateColumn == null)
            {
                WarningMessage = WarningMessage + "\n- Response Date column undefined. Today's date will be used.";
            }
            DataTable file = Utils.TableUtils.readCSV(SelectedFile);
            List<double> invalidMilepostRows = new();
            List<double> invalidBackRows = new();
            List<double> invalidDirectionRows = new();
            List<double> invalidRefDateRows = new();
            List<double> invalidResponseDateRows = new();
            foreach (DataRow row in file.Rows)
            {
                PointResponseModel pointInfo = new();
            #region Milepost value parsing
                if (SRMPIsSelected)
                {
                    int SRMPIndex = file.Columns[FormInfo.SRMPColumn].Ordinal;
                    if(double.TryParse(row.ItemArray[SRMPIndex].ToString(),out double result)) 
                    {
                        pointInfo.Srmp = result;
                    }
                    else 
                    { 
                        invalidMilepostRows.Add(file.Rows.IndexOf(row)); 
                    }
                }
                else
                {
                    int ARMIndex = file.Columns[FormInfo.ARMColumn].Ordinal;
                    if (double.TryParse(row.ItemArray[ARMIndex].ToString(), out double result))
                    {
                        pointInfo.Arm = result;
                    }
                    else
                    {
                        invalidMilepostRows.Add(file.Rows.IndexOf(row));
                    }
                }
            #endregion
            #region Route ID Parsing
                int RouteIndex = file.Columns[FormInfo.RouteColumn].Ordinal;
                pointInfo.Route = row.ItemArray[RouteIndex].ToString().Trim();
            #endregion
            #region Back parsing
                if (FormInfo.BackColumn != null)
                {
                    int BackIndex = file.Columns[FormInfo.BackColumn].Ordinal;
                    BooleanWithError val = TableUtils.getBack(row.ItemArray[BackIndex].ToString());
                    if (val.Error == false)
                    {
                        pointInfo.Back = val.BoolVal;
                    }
                    else
                    {
                        invalidBackRows.Add(file.Rows.IndexOf(row));
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
                    int DirectionIndex = file.Columns[FormInfo.DirectionColumn].Ordinal;
                    BooleanWithError val = TableUtils.getDirection(row.ItemArray[DirectionIndex].ToString());
                    if (val.Error == false)
                    {
                        pointInfo.Decrease = val.BoolVal;
                    }
                    else
                    {
                        invalidDirectionRows.Add(file.Rows.IndexOf(row));
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
                    int RefDateIndex = file.Columns[FormInfo.ReferenceDateColumn].Ordinal;
                    StringWithError val = TableUtils.getDate(row.ItemArray[RefDateIndex].ToString());
                    if(val.Error == false)
                    {
                        pointInfo.ReferenceDate = val.StringVal;
                    }
                    else
                    {
                        invalidRefDateRows.Add(file.Rows.IndexOf(row));
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
                    int ResponseDateIndex = file.Columns[FormInfo.ResponseDateColumn].Ordinal;
                    StringWithError val = TableUtils.getDate(row.ItemArray[ResponseDateIndex].ToString());
                    if (val.Error == false)
                    {
                        pointInfo.ResponseDate = val.StringVal;
                    }
                    else
                    {
                        invalidResponseDateRows.Add(file.Rows.IndexOf(row));
                    }
                }
                else
                {
                    pointInfo.ResponseDate = DateTime.Now.ToString("MM/dd/yyyy"); //default value
                }
                #endregion
                PointInfos.Add(pointInfo);
                DataLoaded = true;
            }
        });
        public MapTableViewModel()
        {
            ColumnTitles = [];
        }
    }
}
