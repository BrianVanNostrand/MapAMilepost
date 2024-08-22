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

namespace MapAMilepost.Utils
{
    class HTTPRequest
    {
        /// <summary>
        /// -   Uses Flurl to execute an HTTP Get request, with URL parameters generated using the SOE arguments passed from the MapPointViewModel and MapLineViewModel.
        /// -   Deserializes the HTTP response to an array of SoeResponseModels and parses that array to return the appropriate value, depending on if this method
        ///     was invoked by Map A Point or Map A Line.
        /// -   The initial deserialization to an array is performed because the SOE Always returns an array, even if only one response is found. This array is ordered 
        ///     based on proximity to the geometry of the argument passed in via URL params, so the first response in the array is used.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        
        
        public static async Task<object> QuerySOE(SoeArgsModel args)
        {
            object response = new();// assume find nearest route location fails
            object FNRLResponse = await FindNearestRouteLocation(args);
            if (FNRLResponse != null)
            {
                var FRLParams = new FRLRequestObject(FNRLResponse as SoeResponseModel);
                object FRLResponse = await FindRouteLocation(FRLParams, args);
                response = FRLResponse;
            }
            else
            {
                response = null;
            }
            return response;
        }

        private static async Task<object> FindNearestRouteLocation(SoeArgsModel args)
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
            FNRLurl.SetQueryParams(FNRLQueryParams);
            var responseObject = new object();
            try
            {
                var FNRLresponse = await FNRLurl.GetAsync();
                if (FNRLresponse.StatusCode == 200)
                {
                    string responseString = await FNRLresponse.ResponseMessage.Content.ReadAsStringAsync();
                    var SoeResponses = JsonSerializer.Deserialize<List<SoeResponseModel?>>(responseString);
                    if (SoeResponses.Count > 0)
                    {
                        responseObject = SoeResponses.First();
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"No results found within {args.SearchRadius} feet of clicked point.");
                        responseObject = null;
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
            return responseObject;
        }
        private static async Task<object> FindRouteLocation(object FNRLResponse, SoeArgsModel args)
        {
            var FNRLurl = new Flurl.Url("https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Route%20Locations");
            Dictionary<string,object> FNRLQueryParams = new Dictionary<string, object> {
                {"f", "json"},
                {"locations", $"[{JsonSerializer.Serialize(FNRLResponse)}]"},
                {"outSR",args.SR}
            };
            FNRLurl.SetQueryParams(FNRLQueryParams);
            var responseObject = new object();
            try
            {
                var FRLresponse = await FNRLurl.GetAsync();
                if (FRLresponse.StatusCode == 200)
                {
                    string responseString = await FRLresponse.ResponseMessage.Content.ReadAsStringAsync();
                    var SoeResponses = JsonSerializer.Deserialize<List<SoeResponseModel?>>(responseString);
                    if (SoeResponses.Count > 0)
                    {
                        if(SoeResponses.First().RouteGeometry.x != 0 && SoeResponses.First().RouteGeometry.y != 0) { 
                            responseObject = SoeResponses.First();
                        }
                        else
                        {
                            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"The nearest route, {SoeResponses.First().Route}, did not return a route location.");
                        }
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show($"Location couldn't be found. Please try again.");
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
            return responseObject;
        }
        public static async Task<List<List<double>>> FindLineLocation(SoeResponseModel startResponse, SoeResponseModel endResponse, SoeArgsModel args)
        {
            var lineRequestURL = new Flurl.Url("https://data.wsdot.wa.gov/arcgis/rest/services/Shared/ElcRestSOE/MapServer/exts/ElcRestSoe/Find%20Route%20Locations");
            SOELineArgsModel lineLocations = new SOELineArgsModel
            {
                Route = startResponse.Route,
                Decrease = startResponse.Decrease,
                Srmp = startResponse.Srmp,
                Back = startResponse.Back,
                ReferenceDate = args.ReferenceDate,
                EndSrmp = endResponse.Srmp,
                EndBack = endResponse.Back,
            };
            Dictionary<string, object> lineRequestParams = new Dictionary<string, object> {
                {"f", "json"},
                {"locations", $"[{JsonSerializer.Serialize(lineLocations)}]"},
                {"outSR",args.SR}
            };
            lineRequestURL.SetQueryParams(lineRequestParams);
            List<List<double>> responseObject = new List<List<double>>();
            try
            {
                var FRLresponse = await lineRequestURL.GetAsync();
                if (FRLresponse.StatusCode == 200)
                {
                    string responseString = await FRLresponse.ResponseMessage.Content.ReadAsStringAsync();
                    var SoeResponses = JsonSerializer.Deserialize<List<FRLLineGeometryModel>>(responseString);
                    if (SoeResponses.Count > 0)
                    {
                         responseObject = (SoeResponses.First().RouteGeometry.paths).First();
                    }
                    else
                    {
                        MessageBox.Show($"No lines found. Please try again.");
                    }
                }
                else
                {
                    MessageBox.Show(FRLresponse.ResponseMessage.ToString());
                }
            }
            catch
            {
                MessageBox.Show("Line request timed out. Please check internet connection and try again.");
            }
            return responseObject;
        }
    }
    
}
