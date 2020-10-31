using AxMapWinGIS;
using GPXManager.entities;
using GPXManager.entities.mapping;
using GPXManager.entities.mapping.Views;
using MapWinGIS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1;
using Xceed.Wpf.AvalonDock.Controls;
using WindowMenuItem = System.Windows.Controls.MenuItem;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for MapWindowForm.xaml
    /// </summary>
    /// 
   
    public partial class MapWindowForm : Window
    {

        private static MapWindowForm  _instance;

        public AxMapWinGIS.AxMap MapControl { get; set; }
        public MapWindowForm()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
        }

        public MapInterActionHandler MapInterActionHandler { get; private set; }
        public MapLayersHandler MapLayersHandler { get; private set; }
        public static MapWindowForm GetInstance()
        {
            if (_instance == null) _instance = new MapWindowForm();
            return _instance;
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _instance = null;
            MapWindowManager.CleanUp();
            this.SavePlacement();

            Entities.GPXFileViewModel.RemoveAllFromMap();
            ParentWindow.ResetDataGrids();
        }



        public GPXFile GPXFile { get; set; }
        public MainWindow ParentWindow { get; set; }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();

            MapControl = new AxMapWinGIS.AxMap();
            host.Child = MapControl;
            MapGrid.Children.Add(host);

            MapLayersHandler = new MapLayersHandler(MapControl);
            MapInterActionHandler = new MapInterActionHandler(MapControl, MapLayersHandler);
            MapControl.ZoomBehavior = tkZoomBehavior.zbDefault;
            MapWindowManager.RestoreMapState(this);
            menuMapTilesVisible.IsChecked = MapControl.TileProvider != tkTileProvider.ProviderNone;
            menuMapTilesSelectProvider.IsEnabled = MapControl.TileProvider != tkTileProvider.ProviderNone;
            menuMapCoastlineVisible.IsChecked = MapLayersHandler.get_MapLayer("Coastline").Visible;
            MapWindowManager.ResetCursor();

            
        }


        public string CoastlineShapeFile_FileName { get; set; }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void SelectTileProvider()
        {
            SelectTileProviderWindow stpw = new SelectTileProviderWindow();
            if (stpw.ShowDialog() == true)
            {
                MapControl.TileProvider = (tkTileProvider)Enum.Parse(typeof(tkTileProvider), stpw.TileProviderID.ToString());
            } 
        }
        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            switch(((WindowMenuItem)sender).Name)
            {
                case "menuMapTilesSelectProvider":
                    SelectTileProvider();
                    break;
                case "menuClose":
                    Close(); 
                    break; 
                case "menuSaveMapState":
                    if(MapWindowManager.SaveMapState()==false)
                    {
                        System.Windows.MessageBox.Show(MapWindowManager.LastError, "GPXManager", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    break;
                case "menuAOICreate":
                    var aoiw = new AOIWindow();
                    aoiw.Owner = this;
                    aoiw.AddNewAOI();
                    aoiw.Show(); 
                    AOIManager.AddNew();
                    break;
                case "menuAOIList":
                    aoiw = new AOIWindow();
                    aoiw.ShowAOIList();
                    aoiw.Owner = this;
                    aoiw.Show();
                    break;
                case "menuIslandLabels":
                    break;
            }
        }

        private void OnMenuChecked(object sender, RoutedEventArgs e)
        {
            var menuItem = (WindowMenuItem)sender;
            switch(menuItem.Name)
            {
                case "menuMapCoastlineVisible":
                    var coast = MapLayersHandler.get_MapLayer("Coastline");
                    MapLayersHandler.EditLayer(coast.Handle, coast.Name, menuItem.IsChecked);
                    break;
                case "menuMapTilesVisible":
                    if(menuItem.IsChecked)
                    {
                        menuMapTilesSelectProvider.IsEnabled = true;
                        if(MapControl.TileProvider==tkTileProvider.ProviderNone)
                        {
                            SelectTileProvider();
                        }
                    }
                    else
                    {
                        MapControl.TileProvider = tkTileProvider.ProviderNone;
                        menuMapTilesSelectProvider.IsEnabled = false;
                    }
                    break;
            }
        }

        private void ToBeImplemented(string usage)
        {
            System.Windows.MessageBox.Show($"The {usage} functionality is not yet implemented", "Placeholder and not yet working", MessageBoxButton.OK, MessageBoxImage.Information); ;
        }

        private void OnToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            tkCursorMode cursorMode = tkCursorMode.cmNone;
            tkCursor cursor = tkCursor.crsrArrow;
            switch(((System.Windows.Controls.Button)sender).Name)
            {
                case "buttonExit":
                    Close();
                    break;
                case "buttonRuler":
                    cursorMode = tkCursorMode.cmMeasure;
                    cursor = tkCursor.crsrCross;
                    break;
                case "buttonPan":
                    cursorMode = tkCursorMode.cmPan;
                    cursor = tkCursor.crsrSizeAll;
                    break;
                case "buttonZoomPlus":
                    cursorMode = tkCursorMode.cmZoomIn;
                    cursor = tkCursor.crsrCross;
                    MakeCursor("zoom_plus");
                    break;
                case "buttonZoomMinus":
                    cursorMode = tkCursorMode.cmZoomOut;
                    cursor = tkCursor.crsrCross;
                    break;
                case "buttonSelect":
                    cursorMode = tkCursorMode.cmSelection;
                    cursor = tkCursor.crsrHand;
                    break;
                case "buttonSelectNone":
                    MapLayersHandler.ClearAllSelections();
                    break;
                case "buttonAttributes":
                    if (MapLayersHandler.MapHasSelection)
                    {
                        var saw = new ShapeFileAttributesWindow();
                        saw.ShowShapeFileAttribute(MapWindowManager.MapLayersHandler.CurrentMapLayer.LayerObject as Shapefile);
                        saw.Owner = this;
                        saw.Show();
                    }
                    break;
                case "buttonGears":
                    ToBeImplemented("mapping options");
                    break;
                case "buttonUploadCloud":
                    ToBeImplemented("upload to cloud");
                    break;
                case "buttonCalendar":
                    ToBeImplemented("calendar");
                    break;
                case "buttonArchive":
                    ToBeImplemented("archive");
                    break;
                case "buttonTrack":
                    ToBeImplemented("track");
                    break;
                case "buttonGPS":
                    ToBeImplemented("gps");
                    break;
                case "buttonLayers":
                    ToBeImplemented("manage layers");
                    break;
                case "buttonAddLayer":
                    ToBeImplemented("add a layer");
                    break;

            }
            MapControl.CursorMode = cursorMode;
            MapControl.MapCursor = cursor;
        }

    }
}
