using AxMapWinGIS;
using GPXManager.views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MapWinGIS;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Management;
using System.Security.Policy;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Threading;
using GPXManager.entities.mapping.Views;

namespace GPXManager.entities.mapping
{
    public static class MapWindowManager
    {
        public static ShapeFileAttributesWindow ShapeFileAttributesWindow { get; set; }
        public static MapLayersWindow MapLayersWindow { get; set; }
        public static MapWindowForm MapWindowForm { get; private set; }

        public static MapLayer GPXTracksLayer { get; set; }

        public static MapLayer GPXWaypointsLayer { get; set; }


        public static MapLayersHandler MapLayersHandler { get; private set; }
        public static MapInterActionHandler MapInterActionHandler { get; private set; }
        public static Shapefile Coastline { get; private set; }
        public static AxMap MapControl { get; private set; }
        public static Dictionary<int, string> TileProviders { get; set; } = new Dictionary<int, string>();
        public static void CleanUp(bool applicationIsClosing = false)
        {
            MapInterActionHandler = null; 
            MapLayersHandler = null; ;
            MapControl = null;
            Coastline = null;
            MapWindowForm = null;
            MapLayersWindow = null;
            GPXTracksLayer = null;
            GPXWaypointsLayer = null;

            if(applicationIsClosing)
            {
                TileProviders = null;
            }
        }



        static MapWindowManager()
        {
            TileProviders.Add(0, "OpenStreetMap");
            TileProviders.Add(1, "OpenCycleMap");
            TileProviders.Add(2, "OpenTransportMap");
            TileProviders.Add(3, "BingMaps");
            TileProviders.Add(4, "BingSatellite");
            TileProviders.Add(5, "BingHybrid");
            TileProviders.Add(6, "GoogleMaps");
            TileProviders.Add(7, "GoogleSatellite");
            TileProviders.Add(8, "GoogleHybrid");
            TileProviders.Add(9, "GoogleTerrain");
            TileProviders.Add(10, "HereMaps");
            TileProviders.Add(11, "HereSatellite");
            TileProviders.Add(12, "HereHybrid");
            TileProviders.Add(13, "HereTerrain");
            TileProviders.Add(21, "Rosreestr");
            TileProviders.Add(22, "OpenHumanitarianMap");
            TileProviders.Add(23, "MapQuestAerial");


        }
        public static MapWindowForm OpenMapWindow(MainWindow ownerWindow, string coastlineShapefile = "")
        {
            MapWindowForm mwf = MapWindowForm.GetInstance();
            if (mwf.Visibility == Visibility.Visible)
            {
                MapWindowForm.BringIntoView();
            }
            else
            {
                MapWindowForm = mwf;
                MapWindowForm.Closing += MapWindowForm_Closing;
                MapWindowForm.Owner = ownerWindow;
                MapWindowForm.ParentWindow = ownerWindow;
                MapWindowForm.Show();
                MapControl = MapWindowForm.MapControl;
                MapLayersHandler = MapWindowForm.MapLayersHandler;
                MapInterActionHandler = MapWindowForm.MapInterActionHandler;

                AOIManager.Setup();

                if (Coastline == null &&  coastlineShapefile.Length > 0)
                {
                    LoadCoastline(coastlineShapefile);
                }
            }


            if(MapLayersWindow!=null)
            {
                MapLayersWindow.Visibility = Visibility.Visible;
                MapLayersWindow.BringIntoView();
            }

            if(ShapeFileAttributesWindow!=null)
            {
                ShapeFileAttributesWindow.Visibility = Visibility.Visible;
                ShapeFileAttributesWindow.BringIntoView();
            }


            return mwf;
        }

        public static void RestoreMapState(MapWindowForm mwf)
        {

            string path = $"{AppDomain.CurrentDomain.BaseDirectory}/mapstate.txt";
            if (File.Exists(path))
            {
                double extentsLeft = 0;
                double extentsRight = 0;
                double extentsTop = 0;
                double extentsBottom = 0;
                bool hasCoastline = false;
                bool isCoastlineVisible = false;

                if (MapControl == null || MapLayersHandler == null || MapInterActionHandler == null)
                {
                    MapControl = mwf.MapControl;
                    MapLayersHandler = mwf.MapLayersHandler;
                    MapInterActionHandler = mwf.MapInterActionHandler;
                }


                using (XmlReader reader = XmlReader.Create(path))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch(reader.Name)
                            {
                                case "MapState":
                                    extentsLeft = double.Parse(reader.GetAttribute("ExtentsLeft"));
                                    extentsRight = double.Parse(reader.GetAttribute("ExtentsRight"));
                                    extentsTop = double.Parse(reader.GetAttribute("ExtentsTop"));
                                    extentsBottom = double.Parse(reader.GetAttribute("ExtentsBottom"));
                                    hasCoastline = reader.GetAttribute("HasCoastline") == "1";

                                    if(hasCoastline)
                                    {
                                        isCoastlineVisible = reader.GetAttribute("CoastlineVisible") == "1";
                                    }
                                    break;
                                case "Layers":
                                    break;
                                case "Tiles":
                                    if(reader.GetAttribute("Visible")=="0")
                                    {
                                        MapControl.TileProvider = tkTileProvider.ProviderNone;
                                    }
                                    else
                                    {
                                        MapControl.TileProvider = (tkTileProvider)Enum.Parse(typeof(tkTileProvider), reader.GetAttribute("Provider"));
                                    }
                                    break;
                                case "Layer":
                                    switch(reader.GetAttribute("LayerKey"))
                                    {
                                        case "coastline":
                                            if (hasCoastline)
                                            {
                                                var coastlineFile = reader.GetAttribute("Filename");
                                                if (File.Exists(coastlineFile))
                                                {
                                                    MapWindowManager.LoadCoastline(coastlineFile, isCoastlineVisible);
                                                }
                                            }
                                            break;
                                        case "aoi_boundary":
                                            var layerName = reader.GetAttribute("LayerName");
                                            if (Entities.AOIViewModel.Count > 0)
                                            {
                                                var aoi = Entities.AOIViewModel.GetAOI(layerName);
                                                if (aoi != null)
                                                {
                                                    aoi.MapLayerHandle = MapWindowManager.MapLayersHandler.AddLayer(aoi.ShapeFile, aoi.Name, uniqueLayer: true, layerKey: "aoi_boundary");
                                                    AOIManager.UpdateAOIName(aoi.MapLayerHandle, aoi.Name);
                                                }
                                            }
                                            break;
                                    }
                                    break;
                                  
                            }
                        }
                    }
                }

                var ext = new Extents();
                ext.SetBounds(extentsLeft, extentsBottom, 0, extentsRight, extentsTop, 0);
                MapControl.Extents = ext;

               // MapControl.Redraw();
            }
        }
        public static void RedrawMap()
        {
            MapControl.Redraw();
        }

        public static string LastError { get; set; }
        

        public static bool SaveMapState()
        {
            var mapState = MapControl.SerializeMapState(false, mapping.globalMapping.ApplicationPath);
            var filepath = $"{AppDomain.CurrentDomain.BaseDirectory}/mapstate.txt";
            if (File.Exists(filepath))
            {
                try
                {
                    File.Delete(filepath);

                }
                catch(IOException)
                {
                    try
                    {
                        File.WriteAllText(filepath, String.Empty);
                    }
                    catch(Exception ex)
                    {
                        LastError = ex.Message;
                        return false; ;
                    }

                }
                catch(Exception ex)
                {
                    Logger.Log(ex);
                    LastError = ex.Message; 
                    return false;
                }
            }

            //insert custom settings at this point
            string xml= InsertCustomSettingToMapState(mapState);

            using (StreamWriter writer = new StreamWriter(filepath, true))
            {
                writer.Write(PrettyXML.PrettyPrint(xml));
            }


            return true;
        }

        private static string  InsertCustomSettingToMapState(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            var mapstate = doc.GetElementsByTagName("MapState").Item(0);

            var coastlineLayer = MapLayersHandler.get_MapLayer("Coastline");
            XmlAttribute attr = doc.CreateAttribute("HasCoastline");
            attr.Value = coastlineLayer!=null ? "1" : "0";
            mapstate.Attributes.SetNamedItem(attr);

            if (coastlineLayer!=null)
            {
                attr = doc.CreateAttribute("CoastlineVisible");
                attr.Value = coastlineLayer.Visible ? "1" : "0";
                mapstate.Attributes.SetNamedItem(attr);
            }
            return doc.OuterXml;

        }
        public static void ResetCursor()
        {
            MapControl.CursorMode = tkCursorMode.cmNone;
            MapControl.MapCursor = tkCursor.crsrArrow;
        }

        public static void ZoomToShapeFileExtent(Shapefile sf)
        {
            MapControl.Extents = sf.Extents;
            MapControl.Redraw();
        }

        public static void SetLayerVisibility(int layerHandle, bool visibility)
        {

        }

        public static bool LoadCoastline(string coastlineShapeFile_FileName, bool visible=true)
        {
            bool coastlineLoaded = false;
            for (int h = 0; h < MapControl.NumLayers - 1; h++)
            {
                var sf = MapControl.get_GetObject(h) as Shapefile;
                if (sf.Key == "coastline")
                {
                    coastlineLoaded = true;
                    Coastline = sf;
                    break;
                }
            }


            if (!coastlineLoaded && coastlineShapeFile_FileName != null && coastlineShapeFile_FileName.Length > 0)
            {
                Shapefile sf = new Shapefile();
                if (sf.Open(coastlineShapeFile_FileName))
                {
                    sf.Key = "coastline";
                    var h = MapLayersHandler.AddLayer(sf, "Coastline", uniqueLayer:true, isVisible:visible, layerKey: "coastline", rejectIfExisting:true);
                    Coastline = sf;
                }
            }
            return Coastline != null;
        }
        private static void MapWindowForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MapWindowForm = null;
        }

        public static void RemoveLayerByKey(string key)
        {
            MapLayersHandler.RemoveLayerByKey(key);
        }

        public static int MapGPX(GPXFile gpxFile,  out int shpIndex,  out List<int>handles,  bool showInMap = true)
        {
            shpIndex = -1;
            handles = new List<int>();
            var utils = new MapWinGIS.Utils();
            var shpfileName = "";
            if (showInMap)
            {
                if (gpxFile != null)
                {
                    Shapefile sf = null;
                    if (gpxFile.TrackCount > 0)
                    {
                        //sf = ShapefileFactory.TrackFromGPX(gpxFile,out shpIndex);
                        sf = ShapefileFactory.TrackFromGPX(gpxFile, out handles);
                        shpfileName = "GPX tracks";
                    }
                    else if (gpxFile.WaypointCount > 0)
                    {
                        sf = ShapefileFactory.NamedPointsFromGPX(gpxFile, out handles);
                        shpfileName = "GPX waypoints";

                    }
                    //MapWindowForm.Title =$"Number of layers:{MapControl.NumLayers}";
                    MapLayersHandler.AddLayer(sf, shpfileName, uniqueLayer: true, layerKey: sf.Key, rejectIfExisting: true);

                    if (gpxFile.TrackCount > 0)
                    {
                        GPXTracksLayer = MapLayersHandler.CurrentMapLayer;
                    }
                    else if (gpxFile.WaypointCount > 0)
                    {

                        GPXWaypointsLayer = MapLayersHandler.CurrentMapLayer;

                    }

                    return MapLayersHandler.CurrentMapLayer.Handle;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                var ly = MapLayersHandler.get_MapLayer(gpxFile.FileName);
                MapLayersHandler.RemoveLayer(ly.Handle);
                return -1;
            }
            
        }
    }
}
