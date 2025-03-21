﻿using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Xps.Serialization;

namespace MapAMilepost.Utils
{
    public class CustomGraphics
    {
        public Dictionary<string, object> SymbolsLibrary { get; set; }

        public async static Task<CustomGraphics> CreateCustomGraphicSymbolsAsync()
        {
            //selected graphics
            //var SelectedSymbolsDictionary = new Dictionary<string, CIMPointSymbol>();
            var SymbolsDictionary = new Dictionary<string, object>{
                { "ClickPoint", await CreatePointSymbolAsync("yellow","cross") },
                { "RoutePoint", await CreatePointSymbolAsync("darkblue", "circle") },
                { "SavedRoutePoint", await CreatePointSymbolAsync("blue", "circle") },
                { "StartRoutePoint", await CreatePointSymbolAsync("darkgreen", "circle") },
                { "SavedStartRoutePoint", await CreatePointSymbolAsync("green", "circle") },
                { "EndRoutePoint", await CreatePointSymbolAsync("darkred", "circle") },
                { "SavedEndRoutePoint", await CreatePointSymbolAsync("red", "circle") },
                { "SelectedRoutePoint", await CreatePointSymbolAsync("selected", "circle") },
                { "RouteLine", await CreateLineSymbolAsync("blue", "dash") },
                { "SavedRouteLine", await CreateLineSymbolAsync("lightblue", "solid") },
                { "SelectedRouteLine", await CreateLineSymbolAsync("selected", "solid") }
            };
            return new CustomGraphics(SymbolsDictionary);
        }
        private CustomGraphics(Dictionary<string, object> Data)
        {
            this.SymbolsLibrary = Data;
        }


        /// <summary>
        /// -   Create a point symbol, used when a click or route point is created or a route point is updated to its saved state.
        /// </summary>
        /// <param name="fillColor"></param>
        /// <returns></returns>
        private static Task<CIMPointSymbol> CreatePointSymbolAsync(string fillColor, string style)
        {
            return QueuedTask.Run(() =>
            {
                SimpleMarkerStyle Style = new(); 
                switch (style){
                    case "cross":
                        Style = SimpleMarkerStyle.Cross;
                        break;
                    case "circle":
                        Style = SimpleMarkerStyle.Circle;
                        break;
                }
                CIMPointSymbol ptSymbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreenRGB, 8, Style);
                var marker = ptSymbol.SymbolLayers[0] as CIMVectorMarker;
                var polySymbol = marker.MarkerGraphics[0].Symbol as CIMPolygonSymbol;
                switch (fillColor)
                {
                    case "yellow":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(224, 227, 66)); //This is the fill
                        break;
                    case "green":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(2, 222, 28));
                        break;
                    case "darkgreen":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(0, 153, 18));
                        break;
                    case "red":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(227, 13, 9));
                        break;
                    case "darkred":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor( 135, 2, 0));
                        break;
                    case "blue":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(62, 108, 214)); //This is the fill
                        break;
                    case "darkblue":
                        polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(29, 53, 110)); //This is the fill
                        break;
                }
                polySymbol.SymbolLayers[0] = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.WhiteRGB, 1, SimpleLineStyle.Solid); //This is the outline
                return ptSymbol;
            });
        }

        private static Task<CIMLineSymbol> CreateLineSymbolAsync(string fillColor, string style)
        {
            return QueuedTask.Run(() =>
            {
                SimpleLineStyle Style = new();
                switch (style)
                {
                    case "dash":
                        Style = SimpleLineStyle.Dash;
                        break;
                    case "solid":
                        Style = SimpleLineStyle.Solid;
                        break;
                }
                CIMLineSymbol lineSymbol = new CIMLineSymbol();
                switch (fillColor){
                    case "blue":
                        lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.CreateRGBColor(52, 101, 217), 4, Style);
                        break;
                    case "lightblue":
                        lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.CreateRGBColor(52, 146, 217), 4, Style);
                        break;
                    case "selected":
                        lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(ColorFactory.Instance.CreateRGBColor(0, 0, 255, 50), 6, Style);
                        break;
                }
                return lineSymbol;
            });
        }

        public static CIMGraphic GetSelectedPointGraphicSymbol(GraphicElement targetGraphic)
        {
            var newCIMGraphic = targetGraphic.GetGraphic();
            var polyFill = SymbolFactory.Instance.ConstructSolidFill(ColorFactory.Instance.CreateRGBColor(0, 0, 255, 50));
            var polyStroke = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.BlackRGB, 0);
            var haloPoly = SymbolFactory.Instance.ConstructPolygonSymbol(polyFill, polyStroke);
            (newCIMGraphic.Symbol.Symbol as CIMPointSymbol).HaloSize = 3;
            (newCIMGraphic.Symbol.Symbol as CIMPointSymbol).HaloSymbol = haloPoly;
            return newCIMGraphic;
        }
    }
}
