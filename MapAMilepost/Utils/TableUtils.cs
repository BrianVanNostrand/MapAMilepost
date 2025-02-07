using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Dialogs;
using MapAMilepost.Models;

namespace MapAMilepost.Utils
{
    class TableUtils
    {
        public static DataTable readCSV(string filePath)
        {
            var dt = new DataTable();
            // Creating the columns
            try
            {
                File.ReadLines(filePath);
            }
            catch(IOException exception){
                MessageBox.Show(exception.Message);
                return null;
            }
            foreach (var headerLine in File.ReadLines(filePath).Take(1))
            {
                foreach (var headerItem in headerLine.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    dt.Columns.Add(headerItem.Trim());
                }
            }

            // Adding the rows
            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                dt.Rows.Add(line.Split(','));
            }
            return dt;
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
