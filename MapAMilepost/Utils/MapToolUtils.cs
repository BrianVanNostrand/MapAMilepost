using ArcGIS.Desktop.Framework;
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
        public static void InitializeSession(Utils.ViewModelBase VM)
        {
            if (!VM.MapToolInfos.SessionActive)
            {
                VM.MapToolInfos.SessionActive = true;
                VM.MapToolInfos.MapButtonLabel = "Stop Mapping";
                VM.MapToolInfos.MapButtonToolTip = "End mapping session.";
                VM.MapToolInfos.MappingTool.StartSession();
            }
        }

        /// <summary>
        /// -   Initialize a mapping session (using the EndSession method in MapAMilepostMaptool viewmodel)
        /// -   Update the public MapButtonLabel property to reflect the behavior of the MapPointExecuteButton.
        ///     This value is bound to the content of the button as a label.
        /// -   Update the private _setSession property to change the behavior of the method.
        /// </summary>
        /// <param name="VM">the target viewmodel</param>
        public static void DeactivateSession(Utils.ViewModelBase VM)
        {
            {
                VM.MapToolInfos.SessionActive = false;
                VM.MapToolInfos.MapButtonLabel = "Start Mapping";
                VM.MapToolInfos.MapButtonToolTip = "Start mapping session.";
                //  Calls the EndSession method from the MapAMilepostMapTool viewmodel, setting the active tool
                //  to whatever was selected before the mapping session was initialized.
                VM.MapToolInfos.MappingTool.EndSession();
                Commands.GraphicsCommands.DeleteUnsavedGraphics();
            }
        }
    }
    public class MapToolInfo : ObservableObject
    {
        public virtual Utils.ViewModelBase VM { get; set; }
        private bool _sessionActive { get; set; }
        public bool SessionActive {
            get { return _sessionActive; }
            set
            {
                _sessionActive = value;
                OnPropertyChanged(nameof(SessionActive));
            }
        }
        public MapAMilepostMaptool MappingTool { get; set; }

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
