using ArcGIS.Core.Internal.Data.LinearReferencing;
using MapAMilepost.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
    class UIUtils
    {
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
    }
}
