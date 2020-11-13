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
        private static MapInterActionHandler _mapInterActionHandler;
        public static MapInterActionHandler MapInteractionHandler 
        { 
            get { return _mapInterActionHandler; }
            set
            {
                _mapInterActionHandler = value;
                _mapInterActionHandler.ShapesSelected += _mapInterActionHandler_ShapesSelected;
            }
        }

        private static void _mapInterActionHandler_ShapesSelected(MapInterActionHandler s, LayerEventArg e)
        {

        }




        public static void Cleanup()
        {
            Entities.GPXFileViewModel.MarkAllNotShownInMap();
            _mapInterActionHandler.ShapesSelected -= _mapInterActionHandler_ShapesSelected;
            _mapInterActionHandler = null;
        }
        public static void RemoveAllFromMap()
        {
            Entities.GPXFileViewModel.MarkAllNotShownInMap();
        }
        public static Shapefile TrackShapefile { get; set; }
        public static Shapefile WaypointsShapefile { get; set; }
    
    }
}
