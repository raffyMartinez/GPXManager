using GPXManager.views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for MapLayersWindow.xaml
    /// </summary>
    public partial class MapLayersWindow : Window
    {
        private bool _gridIsClicked;
        public MapLayersHandler MapLayersHandler { get; set; }

        private static MapLayersWindow _instance;
        public MapLayersWindow()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }

        public MapWindowForm ParentForm { get; set; }

        private void Cleanup()
        {
            MapLayersHandler.LayerRead -= MapLayersHandler_LayerRead;
            MapLayersHandler.LayerRemoved -= MapLayersHandler_LayerRemoved;
            MapLayersHandler.CurrentLayer -= MapLayersHandler_CurrentLayer;

            _instance = null;
            this.SavePlacement();
            MapWindowManager.MapLayersWindow = null;
        }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cleanup();
        }

        public static MapLayersWindow GetInstance()
        {
            if (_instance == null) _instance= new MapLayersWindow();
            return _instance;
        }

        public void RefreshLayers()
        {
            dataGridLayers.Items.Refresh();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            MapLayersHandler.LayerRead += MapLayersHandler_LayerRead;
            MapLayersHandler.LayerRemoved += MapLayersHandler_LayerRemoved;
            MapLayersHandler.CurrentLayer += MapLayersHandler_CurrentLayer;
            dataGridLayers.SelectionChanged += DataGridLayers_SelectionChanged;
            dataGridLayers.DataContextChanged += DataGridLayers_DataContextChanged;
            ParentForm.Closing += ParentForm_Closing;

            MapLayersHandler.OnLayerVisibilityChanged += MapLayersHandler_OnLayerVisibilityChanged;
            MapWindowManager.MapLayersWindow = this;

            dataGridLayers.Columns.Add(new DataGridCheckBoxColumn { Header = "Visible", Binding = new Binding("Visible") });
            dataGridLayers.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Image));
            Binding bind = new Binding("image");//please keep "image" name as you have set in your class data member name
            factory.SetValue(Image.SourceProperty, bind);
            DataTemplate cellTemplate = new DataTemplate() { VisualTree = factory };
            DataGridTemplateColumn imgCol = new DataGridTemplateColumn()
            {
                Header = "image", //this is upto you whatever you want to keep, this will be shown on column to represent the data for helping the user...
                CellTemplate = cellTemplate
            };
            dataGridLayers.Columns.Add(imgCol);

            dataGridLayers.AutoGenerateColumns = false;
            RefreshLayerGrid();



        }

        private void ParentForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cleanup();
        }

        private void DataGridLayers_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => SelectCurrentLayerInGrid()));
            
        }

        private void SelectCurrentLayerInGrid()
        {
            _gridIsClicked = false;
            foreach (var item in dataGridLayers.Items)
            {
                if (((MapLayer)item).Handle == MapLayersHandler.CurrentMapLayer.Handle)
                {
                    dataGridLayers.SelectedItem = item;
                    break;
                }
            }
        }

        private void DataGridLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (_gridIsClicked)
            {
                MapLayersHandler.set_MapLayer(((MapLayer)dataGridLayers.SelectedItem).Handle);
            }
        }

        private void MapLayersHandler_CurrentLayer(MapLayersHandler s, LayerEventArg e)
        {
            SelectCurrentLayerInGrid();
        }

        private void MapLayersHandler_OnLayerVisibilityChanged(MapLayersHandler s, LayerEventArg e)
        {
            RefreshLayerGrid();
        }

        private void RefreshLayerGrid()
        {
            dataGridLayers.DataContext = MapLayersHandler.MapLayers;
            dataGridLayers.Items.Refresh();
        }
        private void MapLayersHandler_LayerRemoved(MapLayersHandler s, LayerEventArg e)
        {
            RefreshLayerGrid();
        }

        private void MapLayersHandler_LayerRead(MapLayersHandler s, LayerEventArg e)
        {
            RefreshLayerGrid();
        }

        private void OnDataGridPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _gridIsClicked = true;
        }

        private void OnToolbarButtonClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
