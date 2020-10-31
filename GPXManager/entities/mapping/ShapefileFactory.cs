using MapWinGIS;
using System;
using System.Collections.Generic;

namespace GPXManager.entities.mapping
{
    public static class ShapefileFactory
    {
        public static List<WaypointLocalTime> WaypointsinLocalTine { get; set; }

        public static void ClearWaypoints()
        {
            WaypointsinLocalTine.Clear();
        }

        static ShapefileFactory()
        {
            WaypointsinLocalTine = new List<WaypointLocalTime>();
        }

        public static Shapefile AOIShapefileFromAOI(AOI aoi)
        {
            var sf = new Shapefile();
            if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYGON))
            {
                var extent = new Extents();
                extent.SetBounds(aoi.UpperLeftX, aoi.LowerRightY, 0, aoi.LowerRightX, aoi.UpperLeftY, 0);
                if (sf.EditAddShape(extent.ToShape()) >= 0)
                {
                    sf.DefaultDrawingOptions.FillTransparency = 0.25F;
                    return sf;
                }
            }
            return null;
        }
        public static Shapefile TrackFromGPX(GPXFile gpxFile)
        {
            if (gpxFile.TrackWaypoinsInLocalTime.Count > 0)
            {
                Shapefile shpFile = new Shapefile();
                if (shpFile.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                {
                    shpFile.GeoProjection = globalMapping.GeoProjection;
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POLYLINE))
                    {
                        foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                        {
                            var shpIndex = shp.AddPoint(wlt.Longitude, wlt.Latitude);
                        }
                    }
                    shpFile.EditAddShape(shp);
                }
                return shpFile;
            }
            else
            {
                return null;
            }
        }

        public static Shapefile NamedPointsFromGPX(GPXFile gpxFile)
        {
            if (gpxFile.NamedWaypointsInLocalTime.Count > 0)
            {
                Shapefile shpFile = new Shapefile();
                if (shpFile.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                {
                    shpFile.GeoProjection = globalMapping.GeoProjection;
                    foreach (var wlt in gpxFile.NamedWaypointsInLocalTime)
                    {
                        var shp = new Shape();
                        if (shp.Create(ShpfileType.SHP_POINT))
                        {
                           if(shp.AddPoint(wlt.Longitude, wlt.Latitude)>=0)
                           {
                                shpFile.EditAddShape(shp);
                           }
                        }
                    }
                }
                return shpFile;
            }
            else
            {
                return null;
            }
        }

        public static Shapefile TrackFromWaypoints()
        {
            if (WaypointsinLocalTine.Count > 0)
            {
                Shapefile shpFile = new Shapefile();
                if (shpFile.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                {
                    shpFile.GeoProjection = globalMapping.GeoProjection;
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POLYLINE))
                    {
                        foreach (var wlt in WaypointsinLocalTine)
                        {
                            var shpIndex = shp.AddPoint(wlt.Longitude, wlt.Latitude);
                        }
                    }
                    shpFile.EditAddShape(shp);
                }

                return shpFile;
            }
            else
            {
                throw new ArgumentException("Waypoint source cannot be null or cannot have zero elements");
            }
        }
    }
}