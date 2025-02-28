using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Models;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MapAMilepost.Utils
{
    class MapToolUtils
    {
        /// <summary>
        /// -   Initialize a mapping session (using the setsession method in MapAMilepostMaptool viewmodel)
        /// -   Update the private _sessionActive property to change the behavior of the method.
        /// </summary>
        /// <param name="VM">the target viewmodel</param>
        public static async Task InitializeSession(Utils.ViewModelBase VM, string sessionType)
        {
            if ((MapView.Active != null && MapView.Active.Map != null))
            {
                if (sessionType == "point"|| sessionType == "start")//if point mapping
                {
                    VM.SessionActive = true;
                }
                else
                {
                    VM.SessionEndActive = true;
                }
                await VM.MappingTool.StartSession(VM, sessionType);
            }
        }

        /// <summary>
        /// -   Initialize a mapping session (using the EndSession method in MapAMilepostMaptool viewmodel)
        /// -   Update the public MapButtonLabel property to reflect the behavior of the MapPointExecuteButton.
        ///     This value is bound to the content of the button as a label.
        /// -   Update the private _setSession property to change the behavior of the method.
        /// </summary>
        /// <param name="VM">the target viewmodel</param>
        public static async Task DeactivateSession(Utils.ViewModelBase VM, string sessionType = null)
        {
           // VM.SRMPIsSelected = true;
            if (sessionType == "point")//if start session or point session
            {
                VM.SessionActive = false;
            }
            else if (sessionType == "start")
            {
                VM.SessionActive = false;
            }
            else if (sessionType == "end")//if end session
            {
                VM.SessionEndActive = false;
            }
            //  Calls the EndSession method from the MapAMilepostMapTool viewmodel, setting the active tool
            //  to whatever was selected before the mapping session was initialized.
            VM.MappingTool.EndSession();
            //await Commands.GraphicsCommands.DeleteUnsavedGraphics(sessionType);
            if (sessionType == null)//tab switched
            {
                await Commands.GraphicsCommands.DeleteUnsavedGraphics(sessionType);
                if (VM.GetType() == typeof(MapPointViewModel))
                {
                    VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);//clear the SOE response info panel or set the default parameters for form
                    await DeactivateSession(VM, "point");
                }
                if (VM.GetType() == typeof(MapLineViewModel))
                {
                    if (VM.IsMapMode)
                    {
                        VM.LineResponse.StartResponse = new PointResponseModel();
                        VM.LineResponse.EndResponse = new PointResponseModel();
                    }
                    else
                    {
                        VM.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);//clear the SOE response info panel or set the default parameters for form
                        VM.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);//clear the SOE response info panel or set the default parameters for form
                    }
                    await DeactivateSession(VM, "start");
                    await DeactivateSession(VM, "end");
                }
            }
        }

        public static async Task<List<List<double>>> GetLine(PointResponseModel startPoint, PointResponseModel endPoint, long SR, string ReferenceDate)
        {
            List<List<double>> lineGeometryResponse = new();
            if(endPoint==null||startPoint==null) { return lineGeometryResponse; }
            if(endPoint.Srmp != null && startPoint.Srmp != null)//ensure that start and endpoints have more than just route and direction, which are assigned immediately after http request resolves.
            {
                if (endPoint.Route == startPoint.Route)
                {
                    if (endPoint.Decrease == startPoint.Decrease)
                    {
                        lineGeometryResponse = (await Utils.HTTPRequest.FindLineLocation(startPoint, endPoint, SR, ReferenceDate));
                    }
                    else
                    {
                        Notification notification = new Notification()
                        {
                            Title = "Different Lanes",
                            Severity = (Notification.SeverityLevel)2,
                            Message = "Start and End points must be located on the same lane direction.",
                            ImageSource = Utils.ImageUtils.ByteToImage(Properties.Resources.LaneDirectionError)
                        };
                        FrameworkApplication.AddNotification(notification);
                    }
                }
                else
                {
                    if (endPoint.Route != null && startPoint.Route != null)
                    {
                        Notification notification = new Notification()
                        {
                            Title = "Different Routes",
                            Message = "Start and End points must be located on the same route.",
                            Severity = (Notification.SeverityLevel)2,
                            ImageSource = Utils.ImageUtils.ByteToImage(Properties.Resources.DifferentRoutesError)
                        };
                        FrameworkApplication.AddNotification(notification);
                    }

                }
            }
            return lineGeometryResponse;
        }
    }

    public class MapToolInfo : ObservableObject
    {
        private bool _sessionActive { get; set; }
        public bool SessionActive {
            get { return _sessionActive; }
            set
            {
                _sessionActive = value;
                OnPropertyChanged(nameof(SessionActive));
            }
        }
        private bool _sessionEndActive { get; set; }
        public bool SessionEndActive
        {
            get { return _sessionEndActive; }
            set
            {
                _sessionEndActive = value;
                OnPropertyChanged(nameof(SessionEndActive));
            }
        }
        private string _mapButtonToolTip {  get; set; }
        public string MapButtonToolTip
        {
            get { return _mapButtonEndToolTip; }
            set
            {
                _mapButtonEndToolTip = value;
                OnPropertyChanged(nameof(MapButtonToolTip));
            }
        }
        private string _mapButtonEndToolTip { get; set; }
        public string MapButtonEndToolTip
        {
            get { return _mapButtonEndToolTip; }
            set
            {
                _mapButtonEndToolTip = value;
                OnPropertyChanged(nameof(MapButtonEndToolTip));
            }
        }

        private string? _mapButtonLabel;
        public string? MapButtonLabel
        {
            get { return _mapButtonLabel; }
            set
            {
                _mapButtonLabel = value;
                OnPropertyChanged(nameof(MapButtonLabel));
            }
        }
        private string? _mapButtonEndLabel;
        public string? MapButtonEndLabel
        {
            get { return _mapButtonEndLabel; }
            set
            {
                _mapButtonEndLabel = value;
                OnPropertyChanged(nameof(MapButtonEndLabel));
            }
        }
    }
}
