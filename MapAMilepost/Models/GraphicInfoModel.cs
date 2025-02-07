using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Models
{
    public class GraphicInfoModel
    {
        public CIMGraphic CGraphic { get; set; }
        public ElementInfo EInfo { get; set; }
    }
}
