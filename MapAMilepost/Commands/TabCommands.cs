using ArcGIS.Desktop.Internal.Mapping.Symbology;
using MapAMilepost.Utils;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MapAMilepost.Commands
{
    internal class TabCommands
    {
        public static void SwitchTab(object control, MainViewModel VM)
        {
            if(control == null)
            {
                return;
            }
            else
            {
                TabItem tabItem = ((TabItem)control);
                string Name = tabItem.Name;
                switch (Name)
                {
                    case "MapPointTab":
                        VM.SelectedViewModel = VM.MapPointVM;
                        break;
                    case "MapLineTab":
                        VM.SelectedViewModel = VM.MapLineVM;
                        break;
                    case "MapTableTab":
                        VM.SelectedViewModel = VM.MapTableVM;
                        break;
                    default:
                        throw new ArgumentException("Control name not found");
                }
            }
        }
    }
}
