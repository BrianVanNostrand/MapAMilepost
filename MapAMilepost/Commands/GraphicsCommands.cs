using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using MapAMilepost.Models;
using MapAMilepost.Utils;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using ArcGIS.Desktop.Internal.Catalog.DistributedGeodatabase.ManageReplicas;
namespace MapAMilepost.Commands
{
    class GraphicsCommands
    {

        /// <summary>
        /// -   Update the graphics on the map, converting a newly mapped route graphic instance to a persisting
        ///     saved graphic (a green point).
        /// -   Delete the click point.
        /// </summary>
        public static async void UpdateSaveGraphicInfos(CustomGraphics CustomPointSymbols)
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            CIMPointSymbol savedRoutePointSymbol = CustomPointSymbols.SymbolsLibrary["DeselectedSymbols"]["SavedRoutePoint"] as CIMPointSymbol;
            //var graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            IEnumerable<GraphicElement> graphicItems = graphicsLayer.GetElementsAsFlattenedList();
            await QueuedTask.Run(() =>
            {
                foreach (GraphicElement item in graphicItems)
                {
                    var cimPointGraphic = item.GetGraphic() as CIMPointGraphic;
                    //set route graphic saved property to true
                    if(item.GetCustomProperty("eventType")== "route")
                    {
                        item.SetCustomProperty("saved", "true");
                    }
                    if (item.GetCustomProperty("eventType") == "route" && item.GetCustomProperty("sessionType") == "point")
                    {
                        cimPointGraphic.Symbol = savedRoutePointSymbol.MakeSymbolReference();
                        item.SetGraphic(cimPointGraphic);
                    }
                }
            });
        }

        /// <summary>
        /// -   Create a reference to the graphics in the graphics layer
        /// -   Delete any previous unsaved graphics from the graphics layer that were created in a point editing session
        ///     (any existing click points or unsaved route points)
        /// -   Generate new click point and route points to reflect the new clicked map point, using custom properties to set the 
        ///     session type, saved status, and event type. These values are used to determine behavior in the UpdateSaveGraphicInfos method.
        /// -   Add the new route and click point to the map, then clear the selection on the graphics layer, since points are added
        ///     to the graphics layer in a "selected" state, displaying editing guide marks.
        /// </summary>
        /// <param name="SoeArgs"></param>
        /// <param name="SoeResponse"></param>
        public static void CreateClickRoutePointGraphics(SoeArgsModel SoeArgs, SoeResponseModel SoeResponse, CustomGraphics CustomPointSymbols, string startEnd)
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            #region create and add click point graphic
            
            var clickedPtGraphic = new CIMPointGraphic() { 
                Symbol = (CustomPointSymbols.SymbolsLibrary["DeselectedSymbols"]["ClickPoint"] as CIMPointSymbol).MakeSymbolReference(),
                Location = (MapPointBuilderEx.CreateMapPoint(SoeArgs.X, SoeArgs.Y))
            };
            //create custom click point props
            var clickPtElemInfo = new ArcGIS.Desktop.Layouts.ElementInfo()
            {
                CustomProperties = new List<CIMStringMap>()
                {
                    new() { Key = "saved", Value = "false" },
                    new() { Key = "eventType", Value = "click" },
                    new() { Key = "sessionType", Value = startEnd!=null?"line":"point" },
                    new() { Key = "startEnd", Value = startEnd }
                }
            };
            graphicsLayer.AddElement(cimGraphic: clickedPtGraphic, elementInfo: clickPtElemInfo, select: false);
            #endregion

            #region create and add route point graphic
            CIMSymbolReference routePointSymbol = (
                startEnd == "start" ? (CustomPointSymbols.SymbolsLibrary["DeselectedSymbols"]["StartRoutePoint"] as CIMPointSymbol).MakeSymbolReference() :
                startEnd == "end" ? (CustomPointSymbols.SymbolsLibrary["DeselectedSymbols"]["EndRoutePoint"] as CIMPointSymbol).MakeSymbolReference() :
                (CustomPointSymbols.SymbolsLibrary["DeselectedSymbols"]["RoutePoint"] as CIMPointSymbol).MakeSymbolReference()
            );
            var soePtGraphic = new CIMPointGraphic() { 
                Symbol = routePointSymbol,
                Location = MapPointBuilderEx.CreateMapPoint(SoeResponse.RouteGeometry.x, SoeResponse.RouteGeometry.y)
            };

            //create custom route point props
            var routePtElemInfo = new ElementInfo()
            {
                CustomProperties = new List<CIMStringMap>()
                {
                    new() { Key = "Route",Value = SoeResponse.Route},
                    new() { Key = "Decrease",Value = SoeResponse.Decrease.ToString()},
                    new() { Key = "Arm",Value = SoeResponse.Arm.ToString()},
                    new() { Key = "Srmp",Value = SoeResponse.Srmp.ToString()},
                    new() { Key = "Back",Value = SoeResponse.Back.ToString()},
                    new() { Key = "ResponseDate",Value = SoeResponse.ResponseDate.ToString()},
                    new() { Key = "EndBack",Value = SoeResponse.EndBack.ToString()},
                    new() { Key = "x",Value = SoeResponse.RouteGeometry.x.ToString()},
                    new() { Key = "y",Value = SoeResponse.RouteGeometry.y.ToString()},
                    new() { Key = "RealignmentDate",Value = SoeResponse.RealignmentDate},
                    new() { Key = "ResponseDate",Value = SoeResponse.ResponseDate},
                    new() { Key = "saved", Value="false"},
                    new() { Key = "eventType", Value="route"},
                    new() { Key = "sessionType", Value = startEnd!=null?"line":"point"},
                    new() { Key = "startEnd", Value = startEnd }
                }
            };
            graphicsLayer.AddElement(cimGraphic: soePtGraphic, elementInfo: routePtElemInfo, select: false);
            #endregion
        }

        public static void CreateRouteLineGraphics(SoeResponseModel startPoint, SoeResponseModel endPoint, CustomGraphics CustomSymbols, List<Coordinate2D> points)
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            var lineGraphic = new CIMLineGraphic()
            {
                Symbol = (CustomSymbols.SymbolsLibrary["DeselectedSymbols"]["RouteLine"] as CIMLineSymbol).MakeSymbolReference(),
                Line = (PolylineBuilderEx.CreatePolyline(points))
            };
            var routePtElemInfo = new ElementInfo()
            {
                CustomProperties = new List<CIMStringMap>()
                {
                    new() { Key = "Route",Value = startPoint.Route},
                    new() { Key = "Decrease",Value = startPoint.Decrease.ToString()},
                    new() { Key = "Arm",Value = startPoint.Arm.ToString()},
                    new() { Key = "EndArm",Value = endPoint.Arm.ToString()},
                    new() { Key = "Srmp",Value = startPoint.Srmp.ToString()},
                    new() { Key = "EndSRMP",Value = endPoint.Srmp.ToString()},
                    new() { Key = "Back",Value = startPoint.Back.ToString()},
                    new() { Key = "ResponseDate",Value = startPoint.ResponseDate.ToString()},
                    new() { Key = "EndBack",Value = endPoint.EndBack.ToString()},
                    new() { Key = "saved", Value="false"},
                    new() { Key = "sessionType", Value = "line"}
                }
            };
            graphicsLayer.AddElement(cimGraphic: lineGraphic, elementInfo: routePtElemInfo, select: false);
            OrganizeGraphics(graphicsLayer);
        }   

        /// <summary>
        /// -   Use the geometry of the route point from the SOE response to generate a label that is displayed
        ///     in an overlay on the map.
        /// </summary>
        /// <param name="SoeResponse"></param>
        public static async void CreateLabel(SoeResponseModel SoeResponse, SoeArgsModel SoeArgs)
        {
            var textSymbol = new CIMTextSymbol();
            //define the text graphic
            var textGraphic = new CIMTextGraphic();
            await QueuedTask.Run(() =>
            {
                var labelGeometry = (MapPointBuilderEx.CreateMapPoint(SoeResponse.RouteGeometry.x, SoeResponse.RouteGeometry.y, SoeArgs.SR));
                //Create a simple text symbol
                textSymbol = SymbolFactory.Instance.ConstructTextSymbol(ColorFactory.Instance.BlackRGB, 10, "Arial", "Bold");
                //Sets the geometry of the text graphic
                textGraphic.Shape = labelGeometry;
                //Sets the text string to use in the text graphic
                textGraphic.Text = $"    {SoeResponse.Srmp}";
                //Sets symbol to use to draw the text graphic
                textGraphic.Symbol = textSymbol.MakeSymbolReference();
                //Draw the overlay text graphic
                MapView.Active.AddOverlay(textGraphic);
            });
        }

        /// <summary>
        /// -   Create reference to graphics layer and the 
        /// </summary>
        /// <param name="deleteIndices"></param>
        public static async void DeleteGraphics(List<int> deleteIndices, string sessionType)
        {
            await QueuedTask.Run(() => {
                GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                var graphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == sessionType);
                for (int i = graphics.Count() - 1; i >= 0; i--)
                {
                    if (deleteIndices.Contains(i))
                    {
                        graphicsLayer.RemoveElement(graphics.ElementAt(i));
                    }
                }
            });
        }

        /// <summary>
        /// -   Delete unsaved graphics, such as click point graphics and unsaved route graphics
        /// </summary>
        public static async void DeleteUnsavedGraphics(string startEnd = null)
        {
            await QueuedTask.Run(() =>
            {
                GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                if (graphicsLayer != null)
                {
                    var graphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic);
                    #region remove previous element if it isn't saved
                    if (graphics != null)
                    {
                        foreach (GraphicElement item in graphicsLayer.GetElementsAsFlattenedList())
                        {
                            //if this graphic item was generated in a point mapping session and is unsaved (if it is a click point or unsaved route point)
                            if (item.GetCustomProperty("saved") == "false")
                            {
                                if (startEnd == null)
                                {
                                    graphicsLayer.RemoveElement(item);
                                }
                                else if (item.GetCustomProperty("startEnd") == startEnd)
                                {
                                    graphicsLayer.RemoveElement(item);
                                }
                                if (item.Name.Contains("Line"))
                                {
                                    graphicsLayer.RemoveElement(item);
                                }
                            }
                        }
                    }
                }
                #endregion
            });
        }
        public static async Task DeselectAllGraphics()
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            if (graphicsLayer != null)
            {
                await QueuedTask.Run(() =>
                {
                    var graphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic);
                    for (int i = graphics.Count() - 1; i >= 0; i--)
                    {
                        setGraphicDelesected(graphics.ElementAt(i));
                    }
                });
            }
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SelectedItems">List of selected rows in data grid</param>
        /// <param name="SoeResponses">List of saved route points</param>
        /// <param name="sessionType">Type of session (point or line) </param>
        public static async void SetPointGraphicsSelected(List<SoeResponseModel> SelectedItems, ObservableCollection<SoeResponseModel> SoeResponses, string sessionType)
        {
            List<int> SelectedIndices = new List<int>();
            for (int i = SoeResponses.Count - 1; i >= 0; i--)
            {
                if (SelectedItems.Contains(SoeResponses[i]))
                {
                    SelectedIndices.Add(i);
                }
            }
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            await QueuedTask.Run(() =>
            {
                var graphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == sessionType);
                if(SelectedItems.Count > 0) //if items are selected
                {
                   for (int i = graphics.Count() - 1; i >= 0; i--)
                    {
                        if (SelectedIndices.Contains(i))
                        {
                            setGraphicSelected(graphics.ElementAt(i));
                        }
                    }
                }
                else //if no items are selected
                {
                    if (sessionType == "point")
                    {
                        for (int i = graphics.Count() - 1; i >= 0; i--)
                        {
                            setGraphicDelesected(graphics.ElementAt(i));
                        }
                    }

                }
            });
        }

        private static void setGraphicSelected(GraphicElement pointGraphic)
        {
            var newCimDefiition = pointGraphic.GetGraphic();
            var polyFill = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(0, 0, 255, 50));
            var polyStroke = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.BlackRGB, 0);
            var haloPoly = SymbolFactory.Instance.ConstructPolygonSymbol(polyFill, polyStroke);
            (newCimDefiition.Symbol.Symbol as CIMPointSymbol).HaloSize = 3;
            (newCimDefiition.Symbol.Symbol as CIMPointSymbol).HaloSymbol = haloPoly;
            pointGraphic.SetGraphic(newCimDefiition);
        }
        private static void setGraphicDelesected(GraphicElement pointGraphic)
        {
            var newCimDefiition = pointGraphic.GetGraphic();
            (newCimDefiition.Symbol.Symbol as CIMPointSymbol).HaloSize = 1;
            (newCimDefiition.Symbol.Symbol as CIMPointSymbol).HaloSymbol = null;
            pointGraphic.SetGraphic(newCimDefiition);
        }

        /// <summary>
        /// -   Check if the point has already been saved to the saved responses array, and if so,
        ///     present a dialog box to confirm the decision to save a duplicate
        /// -   If it has not already been saved
        ///         -   Clear all datagrid selections
        ///         -   create new instance of the SoeResponseModel data object,
        ///             duplicating the properties of the target response model, and add the new instance to the 
        ///             saved response model array.
        /// </summary>
        public static async void SavePointResult(DataGrid myGrid, Utils.ViewModelBase VM)
        {
            //if a point has been mapped
            if (Utils.SoeResponseUtils.HasBeenUpdated(VM.SoeResponse))
            {
                if (VM.SoeResponses.Contains(VM.SoeResponse))
                {
                    System.Windows.MessageBox.Show("This route location has already been saved.");
                }
                else
                //create a duplicate responsemodel object and add it to the array of response models that will persist
                { 
                    //clear selected items
                    VM.SelectedItems.Clear();
                    //clear selected rows
                    myGrid.SelectedItems.Clear();
                    VM.SoeResponses.Add(new SoeResponseModel()
                    {
                        Angle = VM.SoeResponse.Angle,
                        Arm = VM.SoeResponse.Arm,
                        Back = VM.SoeResponse.Back,
                        Decrease = VM.SoeResponse.Decrease,
                        Distance = VM.SoeResponse.Distance,
                        Route = VM.SoeResponse.Route,
                        RouteGeometry = VM.SoeResponse.RouteGeometry,
                        Srmp = VM.SoeResponse.Srmp,
                        RealignmentDate = VM.SoeResponse.RealignmentDate,
                        ResponseDate = VM.SoeResponse.ResponseDate
                    });
                    CustomGraphics customPointSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
                    UpdateSaveGraphicInfos(customPointSymbols);
                    DeleteUnsavedGraphics();
                    VM.SoeArgs.X = 0;
                    VM.SoeArgs.Y = 0;
                    VM.SoeResponse = new SoeResponseModel();//clear the SOE response info panel
                    if (VM.SoeResponses.Count > 0)
                    {
                        VM.ShowResultsTable = true;
                    };
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Create a point to save it to the results tab.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async static Task<ObservableCollection<SoeResponseModel>> TrySyncAddIn(MainViewModel VM)
        {
            ObservableCollection<SoeResponseModel> responses = new ObservableCollection<SoeResponseModel>();
            await QueuedTask.Run(() =>
            {
                GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                if(graphicsLayer != null)
                {
                    var pointGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == "point");
                    var lineGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == "line");
                    ObservableCollection<SoeResponseModel> pointSoeResponses = parseResponses(pointGraphics);
                    VM.MapPointVM.SoeResponses = pointSoeResponses;
                    if(pointSoeResponses.Count > 0)
                    {
                        VM.MapPointVM.ShowResultsTable = true;
                    }
                }
            });
            return responses;
        }

        private static ObservableCollection<SoeResponseModel> parseResponses(IEnumerable<GraphicElement> Graphics)
        {
            ObservableCollection<SoeResponseModel> Responses = new ObservableCollection<SoeResponseModel>();
            foreach (GraphicElement item in Graphics)
            {//if saved TODO
                SoeResponseModel response = new()
                {
                    Route = item.GetCustomProperty("Route"),
                    Decrease = Convert.ToBoolean(item.GetCustomProperty("Decrease")),
                    Arm = Convert.ToDouble(item.GetCustomProperty("Arm")),
                    Srmp = Convert.ToDouble(item.GetCustomProperty("Srmp")),
                    Back = Convert.ToBoolean(item.GetCustomProperty("Back")),
                    EndBack = Convert.ToBoolean(item.GetCustomProperty("EndBack")),
                    Angle = Convert.ToDouble(item.GetCustomProperty("Angle")),
                    ResponseDate =item.GetCustomProperty("ResponseDate"),
                    RealignmentDate = item.GetCustomProperty("RealignmentDate"),
                    RouteGeometry = new SoeResponseModel.coordinatePair { 
                        x= Convert.ToDouble(item.GetCustomProperty("x")), y= Convert.ToDouble(item.GetCustomProperty("y"))
                    }
                };
                Responses.Add(response);
            }
            return Responses;
        }

        private static async void OrganizeGraphics(GraphicsLayer graphicsLayer)
        {
            await QueuedTask.Run(() =>
            {
                var elements = graphicsLayer.GetElementsAsFlattenedList();
                foreach (GraphicElement item in elements)
                { 
                    if (item.Name.Contains("Line")) {
                        graphicsLayer.SendToBack(item);
                    }
                }
            });
        }
    }   

}
