using MapAMilepost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using System.Net.Http;
using System.Security.Policy;
using System.Text.Json;
using System.Windows;
using ArcGIS.Desktop.Core;
using System.Text.Json.Nodes;
using Flurl.Util;
using System.Diagnostics;
using MapAMilepost.ViewModels;
using ArcGIS.Core.Geometry;

namespace MapAMilepost.Utils
{
    class HTTPRequest
    {
        /// <summary>
        /// -   Uses Flurl to execute an HTTP Get request, with URL parameters generated using the SOE arguments passed from the MapPointViewModel and MapLineViewModel.
        /// -   Deserializes the HTTP response to an array of PointResponseModels and parses that array to return the appropriate value, depending on if this method
        ///     was invoked by Map A Point or Map A Line.
        /// -   The initial deserialization to an array is performed because the SOE Always returns an array, even if only one response is found. This array is ordered 
        ///     based on proximity to the geometry of the argument passed in via URL params, so the first response in the array is used.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        
        public class SOEResponse
        {
            public PointResponseModel Response;
            public ArmCalcInfo Info;
            public SOEResponse()
            {

            }
        }
        public static async Task<object> QuerySOE(object mapPoint, PointArgsModel args, string routeFilter=null)
        {
            args.X = ((MapPoint)mapPoint).X;
            args.Y = ((MapPoint)mapPoint).Y;
            args.SR = ((MapPoint)mapPoint).SpatialReference.Wkid;
            object routeLocation = new();// assume find nearest route location fails
            SOEResponse nearestResponse = await FindNearestRouteLocation(args, routeFilter);
            if (nearestResponse.Response != null)
            {
                var locationParam = new LocationInfo(nearestResponse.Response);
                routeLocation = await FindRouteLocation(locationParam, args, nearestResponse.Info.ArmCalcReturnMessage);
            }
            else
            {
                routeLocation = null;
            }
            return routeLocation;
        }

        private static async Task<SOEResponse> FindNearestRouteLocation(PointArgsModel args, string routeFilter = null)
        {
            var FNRLurl = new Flurl.Url("https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Nearest%20Route%20Locations");
            Dictionary<string, string> FNRLQueryParams = new()
            {
                {"referenceDate", args.ReferenceDate},
                {"coordinates", $"[{args.X},{args.Y}]"},
                {"searchRadius", args.SearchRadius},
                {"inSR", args.SR.ToString()},
                {"outSR", args.SR.ToString()},
                {"f", "json"},
            };
            if (routeFilter!=null)
            {
                FNRLQueryParams.Add("routeFilter", $"='{routeFilter}'");
            }
            FNRLurl.SetQueryParams(FNRLQueryParams);
            SOEResponse ResponseInfo = new SOEResponse();
            try
            {
                var FNRLresponse = await FNRLurl.GetAsync();
                if (FNRLresponse.StatusCode == 200)
                {
                    string responseString = await FNRLresponse.ResponseMessage.Content.ReadAsStringAsync();
                    var PointResponses = JsonSerializer.Deserialize<List<PointResponseModel>>(responseString);
                    var ArmCalcReturnInfo = JsonSerializer.Deserialize<List<ArmCalcInfo>>(responseString);
                    if(ArmCalcReturnInfo.Count > 0)
                    {
                        ResponseInfo.Info = ArmCalcReturnInfo.First();
                    }
                    if (PointResponses.Count > 0)
                    {
                        ResponseInfo.Response = PointResponses.First();
                    }
                    else
                    {
                        if(routeFilter != null)
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"No results found within {args.SearchRadius} feet of clicked point on route {routeFilter}.");
                        }
                        else
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"No results found within {args.SearchRadius} feet of clicked point.");
                        }
                        ResponseInfo.Response = null;
                    }
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(FNRLresponse.ResponseMessage.ToString());
                }
            }
            catch
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("'Find Nearest Route Location' timed out. Please check internet connection and try again.");
            }
            return ResponseInfo;
        }
        public static async Task<object> FindRouteLocation(object location, PointArgsModel args, string FNRLInfo = null)
        {
            var FNRLurl = new Flurl.Url("https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Route%20Locations");
            Dictionary<string,object> FNRLQueryParams = new Dictionary<string, object> {
                {"f", "json"},
                {"locations", $"[{JsonSerializer.Serialize(location)}]"},
                {"outSR",args.SR}
            };
            FNRLurl.SetQueryParams(FNRLQueryParams);
            object routeLocation = new();
            try
            {
                var FRLresponse = await FNRLurl.GetAsync();
                if (FRLresponse.StatusCode == 200)
                {
                    string responseString = await FRLresponse.ResponseMessage.Content.ReadAsStringAsync();
                    var PointResponses = JsonSerializer.Deserialize<List<PointResponseModel>>(responseString);
                    var ArmCalcReturnInfo = JsonSerializer.Deserialize<List<ArmCalcInfo>>(responseString);
                    if (ArmCalcReturnInfo.First().ArmCalcReturnCode == 0)
                    {
                        if (PointResponses.Count > 0)
                        {
                            if (PointResponses.First().RouteGeometry.x != 0 && PointResponses.First().RouteGeometry.y != 0)
                            {
                                routeLocation = PointResponses.First();
                            }
                            else
                            {
                                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                                    messageText: $"The nearest route, {PointResponses.First().Route}, did not return a route location."
                                );
                            }
                        }
                        else
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Location couldn't be found. Please try again.");
                        }
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"{ArmCalcReturnInfo.First().ArmCalcReturnMessage + (FNRLInfo!=null?$"\n{FNRLInfo}":"")}");
                    }
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(FRLresponse.ResponseMessage.ToString());
                }
            }
            catch
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("'Find Route Location' timed out. Please check internet connection and try again.");
            }
            return routeLocation;
        }

        public static bool CheckFormData(PointResponseModel FormInfo, string type = null)
        {
            bool formDataValid = true;
            List<string> missingParams = new List<string> { };
            if (FormInfo != null) {
                if (FormInfo.Arm == null && FormInfo.Srmp == null)
                {
                    formDataValid = false;
                    missingParams.Add("SRMP");
                }
                if (FormInfo.Back == null)
                {
                    formDataValid = false;
                    missingParams.Add("Back");
                }
                if (FormInfo.Decrease == null)
                {
                    formDataValid = false;
                    missingParams.Add("Direction");
                }
                if ( FormInfo.Route == null || FormInfo.Route == "")
                {
                    formDataValid = false;
                    missingParams.Add("Route ID");
                }
            }
            else
            {
                formDataValid = false;
            }
            if (missingParams.Count>0)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"The following parameters must be provided in the input form:{(type!=null?" "+type:null)} {string.Join(",",missingParams)}");
            }
            return formDataValid;
        }

        public static async Task<List<List<double>>> FindLineLocation(PointResponseModel startResponse, PointResponseModel endResponse, long SR, string ReferenceDate)
        {
            var lineRequestURL = new Flurl.Url("https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Route%20Locations");
            LineURLParamsModel lineLocations = new LineURLParamsModel
            {
                Route = startResponse.Route,
                Decrease = startResponse.Decrease,
                Srmp = startResponse.Srmp,
                Back = startResponse.Back,
                ReferenceDate = ReferenceDate,
                EndSrmp = endResponse.Srmp,
                EndBack = endResponse.Back,
            };
            Dictionary<string, object> lineRequestParams = new Dictionary<string, object> {
                {"f", "json"},
                {"locations", $"[{JsonSerializer.Serialize(lineLocations)}]"},
                {"outSR",SR}
            };
            lineRequestURL.SetQueryParams(lineRequestParams);
            List<List<double>> responseObject = new List<List<double>>();
            try
            {
                var FRLresponse = await lineRequestURL.GetAsync();
                if (FRLresponse.StatusCode == 200)
                {
                    string responseString = await FRLresponse.ResponseMessage.Content.ReadAsStringAsync();
                    var PointResponses = JsonSerializer.Deserialize<List<FRLLineGeometryModel>>(responseString);
                    if (PointResponses.Count > 0)
                    {
                         responseObject = (PointResponses.First().RouteGeometry.paths).First();
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"No lines found. Please try again.");
                    }
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(FRLresponse.ResponseMessage.ToString());
                }
            }
            catch
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Line request timed out. Please check internet connection and try again.");
            }
            return responseObject;
        }
    }
    
}
