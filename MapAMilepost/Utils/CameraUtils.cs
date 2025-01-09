using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
    class CameraUtils
    {
        public static async Task ZoomToCoords(double X, double Y)
        {
            await QueuedTask.Run(() =>
            {
                Camera newCamera = MapView.Active.Camera;
                newCamera.X = X;
                newCamera.Y = Y;
                MapView.Active.ZoomToAsync(newCamera, TimeSpan.FromSeconds(.5));
            });
        }
        public static async Task ZoomToEnvelope()
        {

        }
    }
}
