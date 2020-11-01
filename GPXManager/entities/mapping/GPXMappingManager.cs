using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public static class GPXMappingManager
    {
        public static void Cleanup()
        {
        }
            public static void RemoveAllFromMap()
            {
                foreach (var item in Entities.GPXFileViewModel.GPXFileCollection)
                {
                    item.ShownInMap = false;
                }
            }
        public static Shapefile TrackShapefile { get; set; }
        public static Shapefile WaypointsShapefile { get; set; }
    
    }
}
