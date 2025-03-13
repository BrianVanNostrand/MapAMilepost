using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Layouts;
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

        /// <summary>
        /// Zoom the map to the provided coordinates and scale.
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="scale">scale to zoom to. Override this with the scale property in the main viewmodel</param>
        /// <returns></returns>
        public static async Task ZoomToCoords(double X, double Y, double scale = 0)
        {
            if (scale == 0)
            {
                scale = MapView.Active.Camera.Scale;
            }
            await QueuedTask.Run(() =>
            {
                Camera newCamera = MapView.Active.Camera;
                newCamera.X = X;
                newCamera.Y = Y;
                newCamera.Scale = scale;
                MapView.Active.ZoomToAsync(newCamera, TimeSpan.FromSeconds(.5));
            });
        }

        /// <summary>
        /// Zoom to the selected scale. Used to zoom to a line.
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public static async Task ZoomToEnvelope(List<List<Double>> coords)
        {
            double xMin=double.MaxValue;
            double yMin= double.MaxValue;
            double xMax= double.MinValue;
            double yMax= double.MinValue;

            foreach(List<Double> coord in coords) {
                if (coord[0] < xMin)
                {
                    xMin = coord[0];
                }
                if (coord[1] < yMin)
                {
                    yMin = coord[1];
                }
                if (coord[0] > xMax) 
                { 
                    xMax= coord[0];
                }
                if (coord[1] > yMax)
                {
                    yMax = coord[1];
                }
            }
            await QueuedTask.Run(async() =>
            {
                //create an envelope (extent) from the coordinates in the line
                List<MapPoint> list = new List<MapPoint>() {
                    MapPointBuilderEx.CreateMapPoint(xMin, yMin),
                    MapPointBuilderEx.CreateMapPoint(xMin, yMax),
                    MapPointBuilderEx.CreateMapPoint(xMax, yMax),
                    MapPointBuilderEx.CreateMapPoint(xMax, yMin)
                };
                ArcGIS.Core.Geometry.Multipoint multiPoint = MultipointBuilderEx.CreateMultipoint(list);
                var hull = GeometryEngine.Instance.ConvexHull(multiPoint);
                ArcGIS.Core.Geometry.Polygon hullPoly = hull as ArcGIS.Core.Geometry.Polygon;
                ArcGIS.Core.Geometry.Geometry buffer = GeometryEngine.Instance.Buffer(hullPoly.Extent, hullPoly.Extent.Width > hullPoly.Extent.Length ? hullPoly.Extent.Width * .2 : hullPoly.Extent.Length * .2);
                await MapView.Active.ZoomToAsync(buffer, TimeSpan.FromSeconds(.5));
            });
        }
    }
}
