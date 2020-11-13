using GPXManager.views;
using MapWinGIS;
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
        private bool _isDragDropDone;
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
            if (MapWindowManager.MapLayersHandler != null)
            {
                MapWindowManager.MapLayersViewModel.LayerRead -= MapLayersViewModel_LayerRead;
                MapWindowManager.MapLayersViewModel.LayerRemoved -= MapLayersViewModel_LayerRemoved;
                MapWindowManager.MapLayersViewModel.CurrentLayer -= MapLayersViewModel_CurrentLayer;
            }

            MapLayersHandler.OnLayerVisibilityChanged -= MapLayersHandler_OnLayerVisibilityChanged;

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
            if (_instance == null) _instance = new MapLayersWindow();
            return _instance;
        }

        public void RefreshLayers()
        {
            dataGridLayers.Items.Refresh();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            MapWindowManager.MapLayersViewModel.LayerRead += MapLayersViewModel_LayerRead;
            MapWindowManager.MapLayersViewModel.LayerRemoved += MapLayersViewModel_LayerRemoved;
            MapWindowManager.MapLayersViewModel.CurrentLayer += MapLayersViewModel_CurrentLayer;

            dataGridLayers.SelectionChanged += DataGridLayers_SelectionChanged;
            dataGridLayers.DataContextChanged += DataGridLayers_DataContextChanged;
            dataGridLayers.PreviewDrop += DataGridLayers_PreviewDrop;
            dataGridLayers.PreviewMouseDown += DataGridLayers_PreviewMouseDown;
            dataGridLayers.LayoutUpdated += DataGridLayers_LayoutUpdated;
            ParentForm.Closing += ParentForm_Closing;

            MapLayersHandler.OnLayerVisibilityChanged += MapLayersHandler_OnLayerVisibilityChanged;
            MapWindowManager.MapLayersWindow = this;

            dataGridLayers.Columns.Add(new DataGridCheckBoxColumn { Header = "Visible", Binding = new Binding("Visible") });
            dataGridLayers.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(System.Windows.Controls.Image));
            Binding bind = new Binding("image");//please keep "image" name as you have set in your class data member name
            factory.SetValue(System.Windows.Controls.Image.SourceProperty, bind);
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

        private void MapLayersViewModel_CurrentLayer(MapLayersViewModel s, LayerEventArg e)
        {
            CurrentLayer = MapLayersHandler.CurrentMapLayer;
            SelectCurrentLayerInGrid();
        }

        private void MapLayersViewModel_LayerRemoved(MapLayersViewModel s, LayerEventArg e)
        {
            RefreshLayerGrid(s);
        }

        private void MapLayersViewModel_LayerRead(MapLayersViewModel s, LayerEventArg e)
        {
            RefreshLayerGrid(s);
        }

        private void DataGridLayers_LayoutUpdated(object sender, EventArgs e)
        {
            if (_isDragDropDone)
            {
                List<MapLayerSequence> layersSequence = new List<MapLayerSequence>();
                int sequence = dataGridLayers.Items.Count - 1;
                foreach (MapLayer ly in dataGridLayers.Items)
                {
                    layersSequence.Add(new MapLayerSequence { MapLayer = ly, Sequence = sequence });
                    sequence--;
                }
                MapLayersHandler.LayersSequence(layersSequence);
                _isDragDropDone = false;


            }

        }

        private void DataGridLayers_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _gridIsClicked = true;

        }

        private void DataGridLayers_PreviewDrop(object sender, DragEventArgs e)
        {
            _isDragDropDone = true;

        }

        private void ParentForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Cleanup();
        }

        private void DataGridLayers_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => SelectCurrentLayerInGrid()));

        }

        public MapLayer CurrentLayer { get; private set; }

        private void SelectCurrentLayerInGrid()
        {
            _gridIsClicked = false;
            if(CurrentLayer==null)
            {
                CurrentLayer = MapLayersHandler.CurrentMapLayer;
            }

            foreach (var item in dataGridLayers.Items)
            {
                if (((MapLayer)item).Handle == CurrentLayer.Handle)
                {
                    dataGridLayers.SelectedItem = item;
                    break;
                }
            }
        }

        private void DataGridLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (_gridIsClicked && dataGridLayers.SelectedItems.Count>0)
            {
                MapLayersHandler.set_MapLayer(((MapLayer)dataGridLayers.SelectedItem).Handle);
            }

        }


        private void MapLayersHandler_OnLayerVisibilityChanged(MapLayersHandler s, LayerEventArg e)
        {
            RefreshLayerGrid();
        }

        private void RefreshLayerGrid(MapLayersViewModel mlvm)
        {
            dataGridLayers.DataContext = mlvm.MapLayerCollection;
            try
            {
                dataGridLayers.Items.Refresh();
            } 
            catch(Exception)
            {
                //ignore;
            }

        }
        private void RefreshLayerGrid()
        {
            dataGridLayers.DataContext = MapWindowManager.MapLayersViewModel.MapLayerCollection;
            dataGridLayers.Items.Refresh();
        }


        private void OnToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonAttributes":
                    ShapeFileAttributesWindow sfw = ShapeFileAttributesWindow.GetInstance(MapWindowManager.MapInterActionHandler);
                    if(sfw.Visibility==Visibility.Visible)
                    {
                        sfw.BringIntoView();
                    }
                    else
                    {
                        sfw.Show();
                        sfw.Owner = this;
                        sfw.ShapeFile = MapLayersHandler.CurrentMapLayer.LayerObject as Shapefile;
                        sfw.ShowShapeFileAttribute();
                    }
                    MapWindowManager.ShapeFileAttributesWindow = sfw;
                    break;
                case "buttonRemove":
                    break;

                case "buttonAdd":
                    break;
            }
        }


    }
}
