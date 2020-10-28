using AxMapWinGIS;
using GPXManager.entities;
using GPXManager.entities.mapping;
using GPXManager.entities.mapping.Views;
using MapWinGIS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private MapInterActionHandler _mapInterActionHandler;
        private MapLayersHandler _mapLayersHandler;
        public AxMapWinGIS.AxMap MapControl { get; set; }
        public MapWindowForm()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
        }

        public MapInterActionHandler MapInterActionHandler
        {
            get { return _mapInterActionHandler; }
        }
        public MapLayersHandler MapLayersHandler 
        {
            get { return _mapLayersHandler; }
        }
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
        }

        public GPXFile GPXFile { get; set; }
        public MainWindow ParentWindow { get; set; }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();

            MapControl = new AxMapWinGIS.AxMap();
            host.Child = MapControl;
            MapGrid.Children.Add(host);

            _mapLayersHandler = new MapLayersHandler(MapControl);
            _mapInterActionHandler = new MapInterActionHandler(MapControl, _mapLayersHandler);
            MapControl.ZoomBehavior = tkZoomBehavior.zbDefault;
            MapWindowManager.RestoreMapState(this);
            menuMapTilesVisible.IsChecked = MapControl.TileProvider != tkTileProvider.ProviderNone;
            menuMapTilesSelectProvider.IsEnabled = MapControl.TileProvider != tkTileProvider.ProviderNone;
            menuMapCoastlineVisible.IsChecked = MapLayersHandler.get_MapLayer("Coastline").Visible;
        }


        public string CoastlineShapeFile_FileName { get; set; }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void SelectTileProvider()
        {
            //if (MapControl.TileProvider != tkTileProvider.ProviderNone)
            //{
                SelectTileProviderWindow stpw = new SelectTileProviderWindow();
                if (stpw.ShowDialog() == true)
                {
                    MapControl.TileProvider = (tkTileProvider)Enum.Parse(typeof(tkTileProvider), stpw.TileProviderID.ToString());
                }
            //}
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
    }
}
