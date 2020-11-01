using MapWinGIS;
using System;
using System.Collections.Generic;

namespace GPXManager.entities.mapping
{

    public static class ShapefileFactory
    {
        private static MapWinGIS.Utils _mapWinGISUtils = new MapWinGIS.Utils();
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
        //public static Shapefile TrackFromGPX(GPXFile gpxFile, out int shpIndex)
        public static Shapefile TrackFromGPX(GPXFile gpxFile, out List<int>handles)
        {
            handles = new List<int>();
            var shpIndex = -1;
            Shapefile sf; ;
            if (gpxFile.TrackWaypoinsInLocalTime.Count > 0)
            {
                if (GPXMappingManager.TrackShapefile == null || GPXMappingManager.TrackShapefile.NumFields == 0)
                {
                    sf = new Shapefile();
                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POLYLINE))
                    {
                        sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Filename", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("Length", FieldType.DOUBLE_FIELD, 1, 1);
                        sf.Key = "gpxfile_track";
                        sf.GeoProjection = GPXManager.entities.mapping.globalMapping.GeoProjection;
                        GPXMappingManager.TrackShapefile = sf;
                    }
                }
                else
                {
                    sf = GPXMappingManager.TrackShapefile;
                }

                var shp = new Shape();
                if (shp.Create(ShpfileType.SHP_POLYLINE))
                {
                    foreach (var wlt in gpxFile.TrackWaypoinsInLocalTime)
                    {
                        shp.AddPoint(wlt.Longitude, wlt.Latitude);
                    }
                }
                shpIndex = sf.EditAddShape(shp);
                handles.Add(shpIndex);
                sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, gpxFile.GPS.DeviceName);
                sf.EditCellValue(sf.FieldIndexByName["FileName"], shpIndex, gpxFile.FileName);
                sf.EditCellValue(sf.FieldIndexByName["Length"], shpIndex, gpxFile.TrackLength);

                return sf;
            }
            else
            {
                return null;
            }
        }

        public static Shapefile NamedPointsFromGPX(GPXFile gpxFile, out List<int> shpIndexes)
        {
            shpIndexes = new List<int>();
            Shapefile sf;
            if (gpxFile.NamedWaypointsInLocalTime.Count > 0)
            {
                if (GPXMappingManager.WaypointsShapefile == null || GPXMappingManager.WaypointsShapefile.NumFields == 0)
                {
                    sf = new Shapefile();

                    if (sf.CreateNewWithShapeID("", ShpfileType.SHP_POINT))
                    {
                        sf.GeoProjection = globalMapping.GeoProjection;
                        sf.Key = "gpxfile_waypoint";
                        sf.EditAddField("Name", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("TimeStamp", FieldType.DATE_FIELD, 1, 1);
                        sf.EditAddField("GPS", FieldType.STRING_FIELD, 1, 1);
                        sf.EditAddField("File name", FieldType.STRING_FIELD, 1, 1);
                        GPXMappingManager.WaypointsShapefile = sf;
                    }

                }
                else
                {
                    sf = GPXMappingManager.WaypointsShapefile;
                }

                foreach (var wlt in gpxFile.NamedWaypointsInLocalTime)
                {
                    var shp = new Shape();
                    if (shp.Create(ShpfileType.SHP_POINT))
                    {
                        if (shp.AddPoint(wlt.Longitude, wlt.Latitude) >= 0)
                        {
                            var shpIndex = sf.EditAddShape(shp);
                            if (shpIndex >= 0)
                            {
                                sf.EditCellValue(sf.FieldIndexByName["Name"], shpIndex, wlt.Name);
                                sf.EditCellValue(sf.FieldIndexByName["TimeStamp"], shpIndex, wlt.Time);
                                sf.EditCellValue(sf.FieldIndexByName["GPS"], shpIndex, gpxFile.GPS.DeviceName);
                                sf.EditCellValue(sf.FieldIndexByName["File name"], shpIndex, gpxFile.FileInfo.Name);
                                shpIndexes.Add(shpIndex);
                            }
                        }
                    }
                }

                sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
                sf.DefaultDrawingOptions.PointSize = 12;
                sf.DefaultDrawingOptions.FillColor = _mapWinGISUtils.ColorByName(tkMapColor.Red);
                return sf;
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