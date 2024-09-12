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
using System.Windows.Media;
using ArcGIS.Core.Internal.CIM;
namespace MapAMilepost.Commands
{
    class GraphicsCommands
    {

        /// <summary>
        /// -   Update the graphics on the map, converting a newly mapped route graphic instance to a persisting
        ///     saved graphic (a green point).
        /// -   Delete the click point.
        /// </summary>
        public static async void UpdateSaveGraphicInfos(CustomGraphics CustomSymbols, string featureID = null)
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            CIMPointSymbol savedRoutePointSymbol = CustomSymbols.SymbolsLibrary["SavedRoutePoint"] as CIMPointSymbol;
            CIMPointSymbol savedRouteStartSymbol = CustomSymbols.SymbolsLibrary["SavedStartRoutePoint"] as CIMPointSymbol;
            CIMPointSymbol savedRouteEndSymbol = CustomSymbols.SymbolsLibrary["SavedEndRoutePoint"] as CIMPointSymbol;
            CIMLineSymbol savedRouteLineSymbol = CustomSymbols.SymbolsLibrary["SavedRouteLine"] as CIMLineSymbol;
            //var graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            IEnumerable<GraphicElement> graphicItems = graphicsLayer.GetElementsAsFlattenedList();
            await QueuedTask.Run(() =>
            {
                foreach (GraphicElement item in graphicItems)
                {
                    //set route graphic saved property to true
                    if(item.GetCustomProperty("eventType")== "route"&& item.GetCustomProperty("saved") != "true")
                    {
                        item.SetCustomProperty("saved", "true");
                        if (item.GetCustomProperty("sessionType") == "point")
                        {
                            var cimGraphic = item.GetGraphic() as CIMPointGraphic;
                            cimGraphic.Symbol = savedRoutePointSymbol.MakeSymbolReference();
                            item.SetGraphic(cimGraphic);
                        }
                        else if (item.GetCustomProperty("sessionType") == "line")
                        {
                            item.SetCustomProperty("featureID", featureID);
                            if (item.GetCustomProperty("startEnd") == "end")//the end point graphic
                            {
                                var cimGraphic = item.GetGraphic() as CIMPointGraphic;
                                cimGraphic.Symbol = savedRouteEndSymbol.MakeSymbolReference();
                                item.SetGraphic(cimGraphic);
                            }
                            else if (item.GetCustomProperty("startEnd") == "start")//the start point graphic
                            {
                                var cimGraphic = item.GetGraphic() as CIMPointGraphic;
                                cimGraphic.Symbol = savedRouteStartSymbol.MakeSymbolReference();
                                item.SetGraphic(cimGraphic);
                            }
                            else if (item.Name.Contains("Line"))//the line graphic
                            {
                                var cimGraphic = item.GetGraphic() as CIMLineGraphic;
                                cimGraphic.Symbol = savedRouteLineSymbol.MakeSymbolReference();
                                item.SetGraphic(cimGraphic);
                            }
                        }
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
        public static async Task CreatePointGraphics(PointArgsModel SoeArgs, PointResponseModel SoeResponse, string sessionType)
        {
            if (SoeResponse != null)
            {
                CustomGraphics CustomSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
                await QueuedTask.Run(() =>
                {
                    GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                    #region create and add click point graphic
            
                    var clickedPtGraphic = new CIMPointGraphic() { 
                        Symbol = (CustomSymbols.SymbolsLibrary["ClickPoint"] as CIMPointSymbol).MakeSymbolReference(),
                        Location = (MapPointBuilderEx.CreateMapPoint(SoeArgs.X, SoeArgs.Y))
                    };
                    //create custom click point props
                    var clickPtElemInfo = new ArcGIS.Desktop.Layouts.ElementInfo()
                    {
                        CustomProperties = new List<CIMStringMap>()
                        {
                            new() { Key = "saved", Value = "false" },
                            new() { Key = "eventType", Value = "click" },
                            new() { Key = "sessionType", Value = sessionType== "point" ? "point" : "line"},
                            new() { Key = "startEnd", Value = sessionType }
                        }
                    };
                    graphicsLayer.AddElement(cimGraphic: clickedPtGraphic, elementInfo: clickPtElemInfo, select: false);
                    #endregion

                    #region create and add route point graphic
                    CIMSymbolReference routePointSymbol = (
                        sessionType == "start" ? (CustomSymbols.SymbolsLibrary["StartRoutePoint"] as CIMPointSymbol).MakeSymbolReference() :
                        sessionType == "end" ? (CustomSymbols.SymbolsLibrary["EndRoutePoint"] as CIMPointSymbol).MakeSymbolReference() :
                        (CustomSymbols.SymbolsLibrary["RoutePoint"] as CIMPointSymbol).MakeSymbolReference()
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
                            new() { Key = "sessionType", Value = sessionType == "point" ? "point" : "line"},
                            new() { Key = "startEnd", Value = sessionType }
                        }
                    };
                    graphicsLayer.AddElement(cimGraphic: soePtGraphic, elementInfo: routePtElemInfo, select: false);
                    #endregion
                });
            }
        }

        /// <summary>
        /// Create graphics for lines.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="LineGeometry"></param>
        /// <returns></returns>
        public static async Task CreateLineGraphics(PointResponseModel startPoint, PointResponseModel endPoint, List<List<double>> LineGeometry)
        {
            CustomGraphics CustomSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            await QueuedTask.Run(async () =>
            {
                List<Coordinate2D> points = new List<Coordinate2D>();
                foreach (var item in LineGeometry)
                {
                    points.Add(new Coordinate2D(item[0], item[1]));
                }
                var lineGraphic = new CIMLineGraphic()
                {
                    Symbol = (CustomSymbols.SymbolsLibrary["RouteLine"] as CIMLineSymbol).MakeSymbolReference(),
                    Line = (PolylineBuilderEx.CreatePolyline(points))
                };
                var routePtElemInfo = new ArcGIS.Desktop.Layouts.ElementInfo()
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
                        new() { Key = "eventType", Value="route"},
                        new() { Key = "sessionType", Value = "line"}
                    }
                };
                graphicsLayer.AddElement(cimGraphic: lineGraphic, elementInfo: routePtElemInfo, select: false);
                await OrganizeGraphics(graphicsLayer);
            });
        }   

        /// <summary>
        /// -   Use the geometry of the route point from the SOE response to generate a label that is displayed
        ///     in an overlay on the map.
        /// </summary>
        /// <param name="SoeResponse"></param>
        public static async void CreateLabel(PointResponseModel SoeResponse, PointArgsModel SoeArgs)
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
        /// -   Create reference to graphics layer and the 
        /// </summary>
        /// <param name="deleteIndices"></param>
        public static async Task DeleteLineGraphics(string[] FeatureIDs)
        {
            await QueuedTask.Run(() => {
                GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                var graphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetCustomProperty("sessionType") == "line");
                foreach (GraphicElement graphic in graphics.ToList())
                {
                    if (FeatureIDs.Contains(graphic.GetCustomProperty("featureID")))
                    {
                        graphicsLayer.RemoveElement(graphic);
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
                                else if (item.Name.Contains("Line"))
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

        /// <summary>
        /// Remove all halos from graphics in the map.
        /// </summary>
        /// <returns></returns>
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
                        SetGraphicDelesected(graphics.ElementAt(i));
                    }
                });
            }
           
        }

        /// <summary>
        /// Add halos to selected items if items are selected in the data grid,
        /// otherwise remove halos.
        /// </summary>
        /// <param name="SelectedItems">List of selected rows in data grid</param>
        /// <param name="SoeResponses">List of saved route points</param>
        /// <param name="sessionType">Type of session (point or line) </param>
        public static async void SetPointGraphicsSelected(List<PointResponseModel> SelectedItems, ObservableCollection<PointResponseModel> SoeResponses, string sessionType)
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
                            SetGraphicSelected(graphics.ElementAt(i));
                        }
                    }
                }
                else //if no items are selected
                {
                    for (int i = graphics.Count() - 1; i >= 0; i--)
                    {
                        SetGraphicDelesected(graphics.ElementAt(i));
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SelectedItems">List of selected rows in data grid</param>
        /// <param name="SoeResponses">List of saved route points</param>
        /// <param name="sessionType">Type of session (point or line) </param>
        public static async void SetLineGraphicsSelected(List<LineResponseModel> SelectedItems)
        {
            CustomGraphics CustomSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            var lineGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMLineGraphic && elem.GetCustomProperty("sessionType") == "line");
            var pointGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == "line");
            await QueuedTask.Run(() =>
            {
                if (SelectedItems.Count > 0) //if items are selected
                {
                    var ItemIDs = SelectedItems.Select(Item => Item.featureID).ToArray();//Selected item IDs
                    foreach (GraphicElement graphic in lineGraphics)
                    {
                        var newCimDefinition = graphic.GetGraphic();
                        newCimDefinition.Symbol.Symbol = (CustomSymbols.SymbolsLibrary["SelectedRouteLine"] as CIMLineSymbol);
                        if (ItemIDs.Contains(graphic.GetCustomProperty("featureID")))
                        {
                            graphic.SetGraphic(newCimDefinition);
                        }
                    }
                    foreach (GraphicElement graphic in pointGraphics)
                    {
                       if (ItemIDs.Contains(graphic.GetCustomProperty("featureID")))
                        {
                            CIMGraphic newCimGraphic = Utils.CustomGraphics.GetSelectedPointGraphicSymbol(graphic);
                            graphic.SetGraphic(newCimGraphic);
                        }
                    }
                }
                else //if no items are selected
                {
                    foreach (GraphicElement graphic in lineGraphics)
                    {
                        var newCimDefiition = graphic.GetGraphic();
                        newCimDefiition.Symbol.Symbol = (CustomSymbols.SymbolsLibrary["SavedRouteLine"] as CIMLineSymbol);
                        graphic.SetGraphic(newCimDefiition);
                    }
                    foreach (GraphicElement graphic in pointGraphics)
                    {
                        if (graphic.GetCustomProperty("startEnd") == "start")
                        {
                            var graphicSymbol = graphic.GetGraphic();
                            graphicSymbol.Symbol.Symbol = (CustomSymbols.SymbolsLibrary["SavedStartRoutePoint"] as CIMPointSymbol);   
                            graphic.SetGraphic(graphicSymbol);
                        }
                        else
                        {
                            var graphicSymbol = graphic.GetGraphic();
                            graphicSymbol.Symbol.Symbol = (CustomSymbols.SymbolsLibrary["SavedEndRoutePoint"] as CIMPointSymbol);
                            graphic.SetGraphic(graphicSymbol);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Add halo to graphic symbols.
        /// </summary>
        /// <param name="targetGraphic"></param>
        private static void SetGraphicSelected(GraphicElement targetGraphic)
        {
            CIMGraphic newCimGraphic = Utils.CustomGraphics.GetSelectedPointGraphicSymbol(targetGraphic);
            targetGraphic.SetGraphic(newCimGraphic);
        }

        /// <summary>
        /// Remove halo from graphic symbols.
        /// </summary>
        /// <param name="targetGraphic"></param>
        private static void SetGraphicDelesected(GraphicElement targetGraphic)
        {
            var newCimDefinition = targetGraphic.GetGraphic();
            if (targetGraphic.Name.Contains("Point"))
            {
                (newCimDefinition.Symbol.Symbol as CIMPointSymbol).HaloSize = 1;
                (newCimDefinition.Symbol.Symbol as CIMPointSymbol).HaloSymbol = null;
            }
            targetGraphic.SetGraphic(newCimDefinition);
        }

        /// <summary>
        /// -   Check if the point has already been saved to the saved responses array, and if so,
        ///     present a dialog box to alert user that point cannot be saved.
        /// -   If it has not already been saved
        ///         -   Clear all datagrid selections
        ///         -   create new instance of the PointResponseModel data object,
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
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("This route location has already been saved.");
                }
                else
                //create a duplicate responsemodel object and add it to the array of response models that will persist
                { 
                    //clear selected items
                    VM.SelectedPoints.Clear();
                    //clear selected rows
                    myGrid.SelectedItems.Clear();
                    VM.SoeResponses.Add(new PointResponseModel()
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
                    VM.SoeResponse = new PointResponseModel();//clear the SOE response info panel
                    if (VM.SoeResponses.Count > 0)
                    {
                        VM.ShowResultsTable = true;
                    };
                }
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Create a point to save it to the results tab.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// -   Check if the line has already been saved to the saved responses array, and if so,
        ///     present a dialog box to alert user that line cannot be saved.
        /// -   If it has not already been saved
        ///         -   Clear all datagrid selections
        ///         -   create new instance of the PointResponseModel data object,
        ///             duplicating the properties of the target response model, and add the new instance to the 
        ///             saved response model array.
        /// </summary>
        public static async void SaveLineResult(DataGrid myGrid, Utils.ViewModelBase VM)
        {
            if (!Utils.SoeResponseUtils.HasBeenUpdated(VM.LineResponse.StartResponse))
            {
                if (Utils.SoeResponseUtils.HasBeenUpdated(VM.LineResponse.EndResponse))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Create a start point to generate a line before saving.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Create a start point and an end point to generate a line before saving.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            else if (!Utils.SoeResponseUtils.HasBeenUpdated(VM.LineResponse.EndResponse))
            {
                if (Utils.SoeResponseUtils.HasBeenUpdated(VM.LineResponse.StartResponse))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Create an end point to generate a line before saving.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Create a start point and an end point to generate a line before saving.", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                if (!(VM.LineResponse.StartResponse.Route == VM.LineResponse.EndResponse.Route))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Start and points must be on the same route.");
                }
                else if (!(VM.LineResponse.StartResponse.Decrease == VM.LineResponse.EndResponse.Decrease))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Start and points must be on the same lane direction.");
                }
                else
                {
                    if (VM.LineResponses.Contains(VM.LineResponse))
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("This route location has already been saved.");
                    }
                    else
                    //create a duplicate responsemodel object and add it to the array of response models that will persist
                    {
                        //clear selected items
                        VM.SelectedLines.Clear();
                        //clear selected rows
                        myGrid.SelectedItems.Clear();
                        string featureID = $"{VM.LineResponse.StartResponse.Route}s{VM.LineResponse.StartResponse.Srmp}e{VM.LineResponse.EndResponse.Srmp}";
                        VM.LineResponses.Add(new LineResponseModel()
                        {
                            StartResponse = VM.LineResponse.StartResponse,
                            EndResponse = VM.LineResponse.EndResponse,
                            featureID = featureID
                        });
                        CustomGraphics customPointSymbols = await Utils.CustomGraphics.CreateCustomGraphicSymbolsAsync();
                        UpdateSaveGraphicInfos(customPointSymbols, featureID);
                        DeleteUnsavedGraphics();
                        VM.LineArgs = new LineArgsModel(VM.LineArgs.StartArgs.SearchRadius, VM.LineArgs.EndArgs.SearchRadius);
                        VM.LineResponse = new LineResponseModel();//clear the SOE response info panel
                        if (VM.LineResponses.Count > 0)
                        {
                            VM.ShowResultsTable = true;
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the content of the add in's graphic layer and adds it to the "saved features" table in the add in.
        /// </summary>
        /// <param name="VM">The main viewmodel</param>
        /// <returns></returns>
        public async static Task<ObservableCollection<PointResponseModel>> TrySyncAddIn(MainViewModel VM)
        {
            ObservableCollection<PointResponseModel> responses = new ObservableCollection<PointResponseModel>();
            await QueuedTask.Run(() =>
            {
                GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
                if(graphicsLayer != null)
                {
                    var pointGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == "point");
                    var lineGraphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic && elem.GetCustomProperty("sessionType") == "line");
                    ObservableCollection<PointResponseModel> pointSoeResponses = ParseResponses(pointGraphics);
                    VM.MapPointVM.SoeResponses = pointSoeResponses;
                    if(pointSoeResponses.Count > 0)
                    {
                        VM.MapPointVM.ShowResultsTable = true;
                    }
                }
            });
            return responses;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Graphics"></param>
        /// <returns></returns>
        private static ObservableCollection<PointResponseModel> ParseResponses(IEnumerable<GraphicElement> Graphics)
        {
            ObservableCollection<PointResponseModel> Responses = new ObservableCollection<PointResponseModel>();
            foreach (GraphicElement item in Graphics)
            {//if saved TODO
                PointResponseModel response = new()
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
                    RouteGeometry = new PointResponseModel.coordinatePair { 
                        x= Convert.ToDouble(item.GetCustomProperty("x")), y= Convert.ToDouble(item.GetCustomProperty("y"))
                    }
                };
                Responses.Add(response);
            }
            return Responses;
        }

        /// <summary>
        /// Push the line features in the graphics layer to the bottom,
        /// ensuring the points are displayed on top of the lines.
        /// </summary>
        /// <param name="graphicsLayer"></param>
        /// <returns></returns>
        private static async Task OrganizeGraphics(GraphicsLayer graphicsLayer)
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
