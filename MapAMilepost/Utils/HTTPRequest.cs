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

namespace MapAMilepost.Utils
{
    class HTTPRequest
    {
        /// <summary>
        /// -   Uses Flurl to execute an HTTP Get request, with URL parameters generated using the SOE arguments passed from the MapPointViewModel and MapLineViewModel.
        /// -   Deserializes the HTTP response to an array of SOEResponseModels and parses that array to return the appropriate value, depending on if this method
        ///     was invoked by Map A Point or Map A Line.
        /// -   The initial deserialization to an array is performed because the SOE Always returns an array, even if only one response is found. This array is ordered 
        ///     based on proximity to the geometry of the argument passed in via URL params, so the first response in the array is used.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        
        
        public static async Task<object> QuerySOE(SOEArgsModel args)
        {
            object response = new object();// assume find nearest route location fails
            var FNRLResponse = await findNearestRouteLocation(args);
            if (FNRLResponse != null)
            {
                var FRLParams = new FRLRequestObject(FNRLResponse as SOEResponseModel);
                object FRLResponse = await findRouteLocation(FRLParams, args);
                response = FRLResponse;
            }
            return response;
        }
        private static async Task<object> findNearestRouteLocation(SOEArgsModel args)
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
                    var soeResponses = JsonSerializer.Deserialize<List<SOEResponseModel?>>(responseString);
                    if (soeResponses.Count > 0)
                    {
                        responseObject = soeResponses.First();
                    }
                    else
                    {
                        MessageBox.Show($"No results found within {args.SearchRadius} feet of clicked point.");
                    }
                }
                else
                {
                    MessageBox.Show(FNRLresponse.ResponseMessage.ToString());
                }
            }
            catch
            {
                MessageBox.Show("HTTP Request  1 timed out. Please check internet connection and try again.");
            }
            return responseObject;
        }
        private static async Task<object> findRouteLocation(object FNRLResponse, SOEArgsModel args)
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
                    var soeResponses = JsonSerializer.Deserialize<List<SOEResponseModel?>>(responseString);
                    if (soeResponses.Count > 0)
                    {
                        responseObject = soeResponses.First();
                    }
                    else
                    {
                        MessageBox.Show($"HTTP Request 2 failed. Please try again.");
                    }
                }
                else
                {
                    MessageBox.Show(FRLresponse.ResponseMessage.ToString());
                }
            }
            catch
            {
                MessageBox.Show("HTTP Request  2 timed out. Please check internet connection and try again.");
            }
            return responseObject;
        }
    }
    
}
