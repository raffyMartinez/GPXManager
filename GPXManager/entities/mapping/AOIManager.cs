using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxMapWinGIS;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public static class AOIManager
    {
        private static int _editedAOI_ID;
        public static string AOIName { get; set; } = "";
        public static int hAOI { get; set; } = -1;
        private static Shapefile _sfAOI;
        public static void Setup()
        {
            MapWindowManager.MapInterActionHandler.ExtentCreated += OnExentCreated;
        }
        public static void AddNew ()
        {
            MapWindowManager.MapControl.MapCursor = tkCursor.crsrCross;
            MapWindowManager.MapControl.CursorMode = tkCursorMode.cmSelection;
        }
        public static void Edit(AOI aoi)
        {
            hAOI = aoi.MapLayerHandle;
            AOIName = aoi.Name;
            _editedAOI_ID = aoi.ID; ;
            MapWindowManager.MapControl.MapCursor = tkCursor.crsrCross;
            MapWindowManager.MapControl.CursorMode = tkCursorMode.cmSelection;
        }


        public static AOI  SaveAOI(string name, bool isEdited=false)
        {
            if (hAOI >= 0)
            {
                var aoi = new AOI
                {
                    Name = name,
                    UpperLeftX = _sfAOI.Extents.xMin,
                    UpperLeftY = _sfAOI.Extents.yMax,
                    LowerRightX = _sfAOI.Extents.xMax,
                    LowerRightY = _sfAOI.Extents.yMin,
                    Visibility = true,
                    MapLayerHandle = hAOI
                };
                if (!isEdited)
                {
                    aoi.ID = Entities.AOIViewModel.NextRecordNumber;
                    Entities.AOIViewModel.AddRecordToRepo(aoi);
                }
                else
                {
                    aoi.ID = _editedAOI_ID;
                    Entities.AOIViewModel.UpdateRecordInRepo(aoi);
                }
                MapWindowManager.ResetCursor();
                return aoi;
            }
            return null;
        }

        private static void OnExentCreated(MapInterActionHandler s, LayerEventArg e)
        {
            if(hAOI >= 0)
            {
                MapWindowManager.MapLayersHandler.RemoveLayer(hAOI);
            }
            else
            {
                AOIName = "New AOI"; 
            }

            _sfAOI = new Shapefile();
            if (_sfAOI.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
            {
                if(_sfAOI.EditAddShape(e.SelectionExtent.ToShape())>=0)
                {
                    hAOI = MapWindowManager.MapLayersHandler.AddLayer(_sfAOI, AOIName);
                    if (hAOI >= 0)
                    {
                        _sfAOI.Key = "aoi";
                        FormatAOI(_sfAOI);
                    }
                }
            }
        }

        public static void FormatAOI(Shapefile aoiShapeFile)
        {
            aoiShapeFile.DefaultDrawingOptions.FillTransparency = 0.25F;
            MapWindowManager.MapLayersHandler.ClearAllSelections();
            MapWindowManager.RedrawMap();
        }
    }
}
