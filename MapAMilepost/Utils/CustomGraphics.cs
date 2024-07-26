using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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
        public Dictionary<string, Dictionary<string, CIMPointSymbol>> SymbolsLibrary { get; set; }

        public async static Task<CustomGraphics> CreateCustomGraphicSymbolsAsync()
        {
            Dictionary<string, Dictionary<string, CIMPointSymbol>> tempSymbolsLibrary = new();
            //selected graphics
            //var SelectedSymbolsDictionary = new Dictionary<string, CIMPointSymbol>();
            //deselected graphics 
            var DeselectedSymbolsDictionary = new Dictionary<string, CIMPointSymbol>{
                { "ClickPoint", await CreatePointSymbolAsync("yellow") },
                { "RoutePoint", await CreatePointSymbolAsync("purple") },
                { "SavedRoutePoint", await CreatePointSymbolAsync("green") },
                { "StartRoutePoint", await CreatePointSymbolAsync("green") },
                { "EndRoutePoint", await CreatePointSymbolAsync("red") },
                { "SelectedRoutePoint", await CreatePointSymbolAsync("selected") }
            };
            tempSymbolsLibrary.Add("DeselectedSymbols", DeselectedSymbolsDictionary);
            return new CustomGraphics(tempSymbolsLibrary);
        }
        private CustomGraphics(Dictionary<string, Dictionary<string, CIMPointSymbol>> Data)
        {
            this.SymbolsLibrary = Data;
        }


        /// <summary>
        /// -   Create a point symbol, used when a click or route point is created or a route point is updated to its saved state.
        /// </summary>
        /// <param name="fillColor"></param>
        /// <returns></returns>
        private static Task<CIMPointSymbol> CreatePointSymbolAsync(string fillColor)
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
    }
}
