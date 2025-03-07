using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Dialogs;
using CsvHelper.Configuration;
using CsvHelper;
using MapAMilepost.Models;
using static ArcGIS.Desktop.Internal.Mapping.Symbology.SymbolUtil;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xaml.Behaviors.Core;
using System.Reflection;

namespace MapAMilepost.Utils
{
    class TableUtils
    {
        public sealed class CSVMap : ClassMap<TableFormInfoModel>
        {
            public CSVMap(TableFormInfoModel formInfo)
            {
                Map(m => m.RouteColumn).Name(formInfo.RouteColumn);
                Map(m => m.SRMPColumn).Name(formInfo.SRMPColumn);
            }
        }
        public static string[] getCSVHeaders(string filePath)
        {
            var reader = new StreamReader(filePath);
            string[] csvHeaders;
            try
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    csvHeaders = csv.HeaderRecord;
                }
            }
            catch(IOException exception){
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(exception.Message);
                return [];
            }
            return csvHeaders;
        }
        public static List<TableFormInfoModel> loadCSV(string filePath,TableFormInfoModel formInfo)
        {
            List<TableFormInfoModel> records = new List<TableFormInfoModel>();
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                CSVMap map = new(formInfo);
                csv.Context.RegisterClassMap(new CSVMap(formInfo));
                records = csv.GetRecords<TableFormInfoModel>().ToList();
            }
           
            return records;
        }
        public static BooleanWithError getBack(string back)
        {
            BooleanWithError returnVal = new BooleanWithError 
            {
                Error = true,
                BoolVal = false
            };
            List<string>TrueTerms = new List<string> {
                "true",
                "t",
                "y",
                "yes",
                "b",
                "back"
            };
            List<string> FalseTerms = new List<string>
            {
                "false",
                "f",
                "n",
                "no",
                "a",
                "ahead"
                
            };
            if (TrueTerms.Contains(back.Trim().ToLower()))
            {
                returnVal.BoolVal = true;
                returnVal.Error = false;
            }
            if (FalseTerms.Contains(back.Trim().ToLower()))
            {
                returnVal.BoolVal = false;
                returnVal.Error = false;
            }
            return returnVal;
        }
        public static BooleanWithError getDirection(string direction)
        {
            BooleanWithError returnVal = new BooleanWithError
            {
                Error = true,
                BoolVal = false
            };
            List<string> DirectionTermsDecrease = new List<string> {
                "decrease",
                "decreasing",
                "d",
                "south",
                "southbound",
                "s",
                "west",
                "westbound",
                "w"
            };
            List<string> DirectionTermsIncrease = new List<string> {
                "increase",
                "increasing",
                "i",
                "north",
                "northbound",
                "n",
                "east",
                "eastbound",
                "e"
            };
            if (DirectionTermsIncrease.Contains(direction.Trim().ToLower()))
            {
                returnVal.BoolVal = true;
                returnVal.Error = false;
            }
            if (DirectionTermsDecrease.Contains(direction.Trim().ToLower()))
            {
                returnVal.BoolVal = false;
                returnVal.Error = false;
            }
            return returnVal;
        }
        public static StringWithError getDate(string date)
        {
            StringWithError returnVal = new StringWithError()
            {
                Error = true,
                StringVal = date
            };
            if (DateTime.TryParse(date, out DateTime result))
            {
                returnVal.StringVal = result.ToString("MM/dd/yyyy");
                returnVal.Error = false;
            }
            return returnVal;
        }
    }
}
