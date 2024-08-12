using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using MapAMilepost.Models;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
    class MapToolUtils
    {
        /// <summary>
        /// -   Initialize a mapping session (using the setsession method in MapAMilepostMaptool viewmodel)
        /// -   Update the public MapButtonLabel property to reflect the behavior of the MapPointExecuteButton.
        ///     This value is bound to the content of the button as a label.
        /// -   Update the private _sessionActive property to change the behavior of the method.
        /// </summary>
        /// <param name="VM">the target viewmodel</param>
        public static void InitializeSession(Utils.ViewModelBase VM, string startEnd = null)
        {
            VM.MappingTool.StartSession(VM, startEnd);
            if (startEnd == null||startEnd=="start")//if point mapping
            {
                VM.MapToolInfos.SessionActive = true;
                VM.MapToolInfos.MapButtonLabel = "Stop Mapping";
                VM.MapToolInfos.MapButtonToolTip = "End mapping session.";
            }
            else
            {
                VM.MapToolInfos.SessionEndActive = true;
                VM.MapToolInfos.MapButtonEndLabel = "Stop Mapping";
                VM.MapToolInfos.MapButtonEndToolTip = "End mapping session.";
            }
            
        }

        /// <summary>
        /// -   Initialize a mapping session (using the EndSession method in MapAMilepostMaptool viewmodel)
        /// -   Update the public MapButtonLabel property to reflect the behavior of the MapPointExecuteButton.
        ///     This value is bound to the content of the button as a label.
        /// -   Update the private _setSession property to change the behavior of the method.
        /// </summary>
        /// <param name="VM">the target viewmodel</param>
        public static void DeactivateSession(Utils.ViewModelBase VM, string startEnd = null)
        {
            {
                VM.MapToolInfos.SessionActive = false;
                VM.MapToolInfos.MapButtonLabel = "Start Mapping";
                VM.MapToolInfos.MapButtonToolTip = "Start mapping session.";
                VM.MapToolInfos.SessionEndActive = false;
                VM.MapToolInfos.MapButtonEndLabel = "Start Mapping";
                VM.MapToolInfos.MapButtonEndToolTip = "Start mapping session.";

                //  Calls the EndSession method from the MapAMilepostMapTool viewmodel, setting the active tool
                //  to whatever was selected before the mapping session was initialized.
                VM.MappingTool.EndSession();
                if(startEnd == null)
                {
                    Commands.GraphicsCommands.DeleteUnsavedGraphics(startEnd);
                }
            }
        }

        /// <summary>
        /// -   Update the SoeArgs that will be passed
        ///     to the HTTP query based on the clicked point on the map.
        /// -   Query the SOE and if the query executes successfully, update the IsSaved public property to determine whether
        ///     or not a dialog box will be displayed, confirming that the user wants to save a duplicate point.
        ///     
        /// ##TODO## 
        /// look at using overlays to display these graphics rather than a graphics layer.
        /// </summary>
        /// <param name="mapPoint"></param>
        public static async void MapPoint2RoutePoint(object mapPoint, Utils.ViewModelBase VM, string startEnd)
        {
            //VM.SoeResponse = new SoeResponseModel();
            SoeResponseModel response = new SoeResponseModel();
            if ((MapPoint)mapPoint != null)
            {
                if (startEnd == "end")
                {
                    VM.SoeEndArgs.X = ((MapPoint)mapPoint).X;
                    VM.SoeEndArgs.Y = ((MapPoint)mapPoint).Y;
                    VM.SoeEndArgs.SR = ((MapPoint)mapPoint).SpatialReference.Wkid;
                    response = (await Utils.HTTPRequest.QuerySOE(VM.SoeEndArgs) as SoeResponseModel);
                }
                else
                {
                    VM.SoeArgs.X = ((MapPoint)mapPoint).X;
                    VM.SoeArgs.Y = ((MapPoint)mapPoint).Y;
                    VM.SoeArgs.SR = ((MapPoint)mapPoint).SpatialReference.Wkid;
                    response = (await Utils.HTTPRequest.QuerySOE(VM.SoeArgs) as SoeResponseModel);
                }
            }
            if (response != null)
            {
                CustomGraphics customPointSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
                await QueuedTask.Run(() =>
                {
                    Commands.GraphicsCommands.DeleteUnsavedGraphics(startEnd);//delete all unsaved graphics
                    if (startEnd == "end")
                    {
                        VM.SoeEndResponse = response;
                        //SoeResponseUtils.CopyProperties(response, VM.SoeEndResponse);
                        Commands.GraphicsCommands.CreateClickRoutePointGraphics(VM.SoeEndArgs, VM.SoeEndResponse, customPointSymbols, startEnd);
                    }
                    else
                    {
                        VM.SoeResponse = response;
                        //SoeResponseUtils.CopyProperties(response, VM.SoeResponse);
                        Commands.GraphicsCommands.CreateClickRoutePointGraphics(VM.SoeArgs, VM.SoeResponse, customPointSymbols, startEnd);
                    }
                });
            }
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
                _sessionActive = value;
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
