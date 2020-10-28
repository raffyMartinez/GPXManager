using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXManager.entities
{
    public class DeviceWaypointGPX
    {
        public GPS GPS { get; set; }
        public string Filename { get; set; }
        public string GPX { get; set; }
        public int RowID { get; set; }

        public string MD5 { get; set; }
    }
}
