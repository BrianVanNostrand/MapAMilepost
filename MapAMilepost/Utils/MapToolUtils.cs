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
            if (Utils.UIUtils.MapViewActive())
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
        /// When either the "Stop mapping" button is clicked, the "Start Mapping" button of the currently inactive mapping session is 
        /// clicked (a start point mapping session is active and the end point mapping session is initialized), or the feature type changes 
        /// by clicking a different tab, or the mapping session mode changes from map mode to form mode:
        /// -   Set the session active property of the start and/or end point mapping session to false. This updates the label of the button via value converter in the xaml.
        /// -   Deactivate the mapping session by disabling the MapTool component (using the EndSession method in MapAMilepostMaptool viewmodel)
        /// -   If a tab change intiated the method, no sessionType is passed in. When this happens:
        ///     - Delete the unsaved graphics in the milepost graphics layer
        ///     - If the viewmodel passed in is the MapPointViewmodel:
        ///         - Reset the point response object
        ///         - Re-call this method with the appropriate sessiontype, re-labeling the buttons and disabling the maptool.
        ///     - If the viewmodel passed in is the MapLineViewmodel:
        ///         -Reset the start and endpoints of the line response object
        ///         - Re-call this method with the appropriate sessiontype, re-labeling the buttons and disabling the maptool.
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
                    VM.LineResponse.StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);//clear the SOE response info panel or set the default parameters for form
                    VM.LineResponse.EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);//clear the SOE response info panel or set the default parameters for form
                    await DeactivateSession(VM, "start");
                    await DeactivateSession(VM, "end");
                }
            }
        }

        /// <summary>
        /// Invoked by the map tool when a start or end point is mapped by clicking the map, in a line mapping session.
        /// - Create an empty list of line geometries
        /// - If either the start or end point doesn't exist, don't attempt to create a line.
        /// - If the start and end point have SRMP values
        /// - If both the start and end points are on the same route
        ///     - If both the start and end points are on the same lane direction:
        ///         - Use the FindLineLocation method to query the SOE and compose the line geometry
        ///     - If not:
        ///         - Create a Arcgis.Desktop.Framework notification alert warning that the points need to be on the same lane
        ///         direction and display it
        /// - If not, Create a Arcgis.Desktop.Framework notification alert warning that the points need to be on the same route
        ///   and display it
        /// </summary>
        /// <param name="startPoint">The start point response model</param>
        /// <param name="endPoint">The end point response model</param>
        /// <param name="SR">The spatial reference WKID</param>
        /// <param name="ReferenceDate">The referenceDate to send to the ELC.</param>
        /// <returns></returns>
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
        //whether or not the start point map tool session is active
        private bool _sessionActive { get; set; }
        public bool SessionActive {
            get { return _sessionActive; }
            set
            {
                _sessionActive = value;
                OnPropertyChanged(nameof(SessionActive));
            }
        }
        //whether or not the end point map tool session is active
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
        //Tool tip for the start mapping button
        private string _mapButtonToolTip {  get; set; }
        public string MapButtonToolTip
        {
            get { return _mapButtonToolTip; }
            set
            {
                _mapButtonToolTip = value;
                OnPropertyChanged(nameof(MapButtonToolTip));
            }
        }
        //Tool tip for the end mapping button
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
        //Label for the start mapping button
        private string _mapButtonLabel;
        public string MapButtonLabel
        {
            get { return _mapButtonLabel; }
            set
            {
                _mapButtonLabel = value;
                OnPropertyChanged(nameof(MapButtonLabel));
            }
        }
        //Label for the end mapping button
        private string _mapButtonEndLabel;
        public string MapButtonEndLabel
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
