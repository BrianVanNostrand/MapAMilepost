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
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MapAMilepost.Commands
{
    class GraphicsCommands
    {

        /// <summary>
        /// -   Create a point symbol, used when a click or route point is created or a route point is updated to its saved state.
        /// </summary>
        /// <param name="fillColor"></param>
        /// <returns></returns>
        public static Task<CIMPointSymbol> CreatePointSymbolAsync(string fillColor)
        {
            return QueuedTask.Run(() =>
            {
                CIMPointSymbol circlePtSymbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreenRGB, 8, SimpleMarkerStyle.Circle);
                var marker = circlePtSymbol.SymbolLayers[0] as CIMVectorMarker;
                var polySymbol = marker.MarkerGraphics[0].Symbol as CIMPolygonSymbol;
                switch (fillColor)
                {
                    case "blue":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(62, 108, 214)); //This is the fill
                        break;
                    case "yellow":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(224, 227, 66)); //This is the fill
                        break;
                    case "green":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(2, 222, 28));
                        break;
                    case "red":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(227, 13, 9));
                        break;
                    case "purple":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(185, 12, 247));
                        break;
                }
                polySymbol.SymbolLayers[0] = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.WhiteRGB, 1, SimpleLineStyle.Solid); //This is the outline
                return circlePtSymbol;
            });
        }

        /// <summary>
        /// -   Update the graphics on the map, converting a newly mapped route graphic instance to a persisting
        ///     saved graphic (a green point).
        /// -   Delete the click point.
        /// </summary>
        public static async void UpdateSaveGraphicInfos()
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            CIMPointSymbol greenPointSymbol = await Commands.GraphicsCommands.CreatePointSymbolAsync("green");
            //var graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            IEnumerable<GraphicElement> graphicItems = graphicsLayer.GetElementsAsFlattenedList();
            await QueuedTask.Run(() =>
            {
                foreach (GraphicElement item in graphicItems)
                {
                    var cimPointGraphic = item.GetGraphic() as CIMPointGraphic;
                    //set graphic saved property to true
                    item.SetCustomProperty("saved", "true");
                    //if point was generated in a point mapping session and it is a click point, remove it.
                    if (item.GetCustomProperty("sessionType") == "point" && item.GetCustomProperty("eventType") == "click")
                    {
                        graphicsLayer.RemoveElement(item);
                    }
                    //if point was generated in a point mapping session and it is a route point, turn it green because it is now saved.
                    if (item.GetCustomProperty("eventType") == "route" && item.GetCustomProperty("sessionType") == "point")
                    {
                        cimPointGraphic.Symbol = greenPointSymbol.MakeSymbolReference();
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
        /// <param name="SOEArgs"></param>
        /// <param name="SOEResponse"></param>
        public static async void CreateClickRoutePointGraphics(SOEArgsModel SOEArgs, SOEResponseModel SOEResponse)
        {
            GraphicsLayer graphicsLayer = MapView.Active.Map.FindLayer("CIMPATH=map/milepostmappinglayer.json") as GraphicsLayer;//look for layer
            var graphics = graphicsLayer.GetElementsAsFlattenedList().Where(elem => elem.GetGraphic() is CIMPointGraphic);
            #region remove previous element if it isn't saved
            if (graphics != null)
            {
                foreach (GraphicElement item in graphicsLayer.GetElementsAsFlattenedList())
                {
                    //if this graphic item was generated in a point mapping session and is unsaved (if it is a click point or unsaved route point)
                    if (item.GetCustomProperty("sessionType") == "point" && item.GetCustomProperty("saved") == "false")
                    {
                        graphicsLayer.RemoveElement(item);
                    }
                }
            }
            #endregion

            #region create and add point graphics
            var clickedPtGraphic = new CIMPointGraphic();
            clickedPtGraphic.Attributes = new Dictionary<string, object>();
            var clickedPtSymbol = await Commands.GraphicsCommands.CreatePointSymbolAsync("yellow");
            clickedPtGraphic.Symbol = clickedPtSymbol.MakeSymbolReference();
            clickedPtGraphic.Location = MapPointBuilderEx.CreateMapPoint(SOEArgs.X, SOEArgs.Y, SOEArgs.SR);
            //create custom click point props
            var clickPtElemInfo = new ArcGIS.Desktop.Layouts.ElementInfo()
            {
                CustomProperties = new List<CIMStringMap>()
                {
                    new CIMStringMap(){ Key="saved", Value="false"},
                    new CIMStringMap(){ Key="sessionType", Value="point"},
                    new CIMStringMap(){ Key="eventType", Value="click"}
                }
            };
            graphicsLayer.AddElement(cimGraphic: clickedPtGraphic, elementInfo: clickPtElemInfo);
            var soePtGraphic = new CIMPointGraphic();
            soePtGraphic.Attributes = new Dictionary<string, object>();
            var soePtSymbol = await Commands.GraphicsCommands.CreatePointSymbolAsync("purple");
            soePtGraphic.Symbol = soePtSymbol.MakeSymbolReference();
            soePtGraphic.Location = MapPointBuilderEx.CreateMapPoint(SOEResponse.RouteGeometry.x, SOEResponse.RouteGeometry.y, SOEArgs.SR);
            //create custom route point props
            var routePtElemInfo = new ElementInfo()
            {
                CustomProperties = new List<CIMStringMap>()
                {
                    new CIMStringMap(){ Key="saved", Value="false"},
                    new CIMStringMap(){ Key="sessionType", Value="point"},
                    new CIMStringMap(){ Key="eventType", Value="route"}
                }
            };
            graphicsLayer.AddElement(cimGraphic: soePtGraphic, elementInfo: routePtElemInfo);
            graphicsLayer.ClearSelection();
            #endregion
        }

        /// <summary>
        /// -   Use the geometry of the route point from the SOE response to generate a label that is displayed
        ///     in an overlay on the map.
        /// </summary>
        /// <param name="soeResponse"></param>
        public static async void CreateLabel(SOEResponseModel soeResponse, SOEArgsModel soeArgs)
        {
            var textSymbol = new CIMTextSymbol();
            //define the text graphic
            var textGraphic = new CIMTextGraphic();
            await QueuedTask.Run(() =>
            {
                var labelGeometry = (MapPointBuilderEx.CreateMapPoint(soeResponse.RouteGeometry.x, soeResponse.RouteGeometry.y, soeArgs.SR));
                //Create a simple text symbol
                textSymbol = SymbolFactory.Instance.ConstructTextSymbol(ColorFactory.Instance.BlackRGB, 10, "Arial", "Bold");
                //Sets the geometry of the text graphic
                textGraphic.Shape = labelGeometry;
                //Sets the text string to use in the text graphic
                textGraphic.Text = $"    {soeResponse.Srmp}";
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

        public static async void SetPointGraphicsSelected()
        {

        }

        public static async void SetPointGraphicsDeselected()
        {

        }
    }
}
