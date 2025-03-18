using ArcGIS.Core.Internal.Data.LinearReferencing;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Models;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapAMilepost.Utils
{
    class UIUtils
    {
        /// <summary>
        /// Check to make sure the map is available
        /// </summary>
        /// <returns></returns>
        public static bool MapViewActive()
        {
            if (MapView.Active!=null&&MapView.Active.Map!=null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the RRQ values of a given Related Route Type
        /// </summary>
        /// <param name="val">The first three digits of the route ID (005 etc.)</param>
        /// <returns></returns>
        public static ObservableCollection<string> GetRouteQualifiers(RouteIDInfo val)
        {
            ObservableCollection<string> result = new ObservableCollection<string>() { "Mainline" };
            List<List<string>> RRTRRQInt = new List<List<string>>();
            List<List<string>> RRTRRQString = new List<List<string>>();
            if (val.RelatedRouteTypes != null && val.RelatedRouteTypes.Count > 0)
            {
                foreach (RRTInfo p in val.RelatedRouteTypes)
                {
                    if (p.RelatedRouteQualifiers != null && p.RelatedRouteQualifiers.Count > 0)
                    {
                        foreach (string rrq in p.RelatedRouteQualifiers)
                        {
                            bool isInt = int.TryParse(rrq.Substring(0, 5), out int number);
                            if (isInt)
                            {
                                RRTRRQInt.Add([p.Title, rrq]);
                            }
                            else
                            {
                                RRTRRQString.Add([p.Title, rrq]);
                            }
                        }
                    }
                }
            }
            RRTRRQInt.Sort((a, b) => (a[1].Substring(0, 5)).CompareTo(b[1].Substring(0, 5)));
            RRTRRQString.Sort((a, b) => a[1].CompareTo(b[1]));
            foreach (var item in RRTRRQString)
            {
                result.Add(item[0] + item[1]);
            }
            foreach (var item in RRTRRQInt)
            {
                result.Add(item[0] + item[1]);
            }
            return result;
        }

        /// <summary>
        /// Parse the route ID data into the collection of RouteID Infos in the map point and map line viewmodels.
        /// These values are used in the RouteID and RRTRRQ comboboxes.
        /// </summary>
        /// <param name="RouteResponses"></param>
        /// <param name="VM"></param>
        public static void SetRouteInfos(Dictionary<string,int> RouteResponses, ViewModelBase VM)
        {
            VM.RouteIDInfos = new();
            foreach (var routeResponse in RouteResponses)
            {
                string RouteNumberTitle = routeResponse.Key.Substring(0, 3);
                string RRTTitle = routeResponse.Key.Length > 3 ? routeResponse.Key.Substring(3, 2) : null;
                string RRQTitle = routeResponse.Key.Length > 5 ? routeResponse.Key.Substring(5) : null;
                List<string> allRouteIDs = VM.RouteIDInfos.Select(x => x.Title).ToList();
                if (allRouteIDs.Contains(RouteNumberTitle))
                {
                    RouteIDInfo routeIDInfo = VM.RouteIDInfos.Where(x => x.Title == RouteNumberTitle).FirstOrDefault();//get the object for this route
                    List<string> allRRTs = routeIDInfo.RelatedRouteTypes.Select(x => x.Title).ToList();
                    if (allRRTs.Contains(RRTTitle))
                    {
                        RRTInfo rrtInfo = routeIDInfo.RelatedRouteTypes.Where(x => x.Title == RRTTitle).FirstOrDefault();
                        if (RRQTitle != null)
                        {
                            rrtInfo.RelatedRouteQualifiers.Add(RRQTitle);
                        }
                    }
                    else//create new RRT
                    {
                        if (RRTTitle != null)
                        {
                            if (RRQTitle != null)
                            {
                                routeIDInfo.RelatedRouteTypes.Add(new()
                                {
                                    Title = RRTTitle,
                                    RelatedRouteQualifiers = new() { RRQTitle }
                                });
                            }
                            else
                            {
                                routeIDInfo.RelatedRouteTypes.Add(new()
                                {
                                    Title = RRTTitle,
                                    RelatedRouteQualifiers = new() { }
                                });
                            }
                        }
                    }
                }
                else//create new route
                {
                    List<RRTInfo> RRTs = new();
                    if (RRTTitle != null)
                    {
                        if (RRQTitle != null)
                        {
                            RRTs.Add(new RRTInfo()
                            {
                                Title = RRTTitle,
                                RelatedRouteQualifiers = new()
                            {
                                RRQTitle
                            }
                            });
                        }
                        else
                        {
                            RRTs.Add(new RRTInfo()
                            {
                                Title = RRTTitle,
                                RelatedRouteQualifiers = new() { }
                            });
                        }
                    }
                    VM.RouteIDInfos.Add(new()
                    {
                        Title = RouteNumberTitle,
                        RelatedRouteTypes = RRTs
                    });
                }
            }
        }

        /// <summary>
        /// Resets the UI by performing the following operations:
        /// - Reset the combobox index for the route selector combobox
        /// - New up the route qualifiers list, clearing the bound RRT/RRQ item set that populates the RRT/RRQ combobox.
        /// - Reset the X/Y click point arguments for the viewmodel.
        /// - Conditionally reset the endpoint values or point value of the active response in the add in. 
        /// </summary>
        /// <param name="VM"> The target viewmodel whose view should be reset</param>
        /// <returns></returns>
        public static async Task ResetUI(Utils.ViewModelBase VM)
        {
            VM.RouteComboIndex = -1;
            VM.RouteQualifiers = new();
            if (VM.GetType() == typeof(MapPointViewModel))
            {
                ResetClickPointArgs("point", VM);
                if (VM.IsMapMode)
                {
                    VM.PointResponse = new PointResponseModel();//clear the SOE response info panel
                }
                else
                {
                    VM.PointResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM);
                }
                if (VM.PointResponses.Count == 0)
                {
                    VM.ShowResultsTable = false;
                };
            }
            else
            {
                ResetClickPointArgs("line", VM);
                if (VM.IsMapMode)
                {
                    VM.LineResponse = new LineResponseModel();//clear the SOE response info panel
                }
                else
                {
                    VM.LineResponse = new LineResponseModel()
                    {
                        StartResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM),
                        EndResponse = Utils.SOEResponseUtils.CreateInputConditionalPointModel(VM)
                    };
                }
                if (VM.LineResponses.Count == 0)
                {
                    VM.ShowResultsTable = false;
                };
            }
            if (Utils.UIUtils.MapViewActive())
            {
                await Commands.GraphicsCommands.DeleteUnsavedGraphics();
            }
        }

        /// <summary>
        /// Reset the click point arguments for the point and line viewmodels. 
        /// This method could be removed and the viewmodels refactored, but I
        /// planned to show the click point coordinates in a text box at one time.
        /// </summary>
        /// <param name="sessionType"></param>
        /// <param name="VM"></param>
        private static void ResetClickPointArgs(string sessionType, Utils.ViewModelBase VM)
        {
            if (!string.IsNullOrEmpty(sessionType))
            {
                if (sessionType == "line")
                {
                    VM.LineArgs.StartArgs.X = 0;
                    VM.LineArgs.StartArgs.Y = 0;
                    VM.LineArgs.EndArgs.X = 0;
                    VM.LineArgs.EndArgs.Y = 0;
                }
                else
                {
                    VM.PointArgs.X = 0;
                    VM.PointArgs.Y = 0;
                }
            }
        }
    }
}
