using GPXManager.entities;
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
using GPXManager;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Windows.Media.TextFormatting;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for GPXFIlePropertiesWindow.xaml
    /// </summary>
    public partial class GPXFIlePropertiesWindow : Window,IDisposable
    {
        public GPXFIlePropertiesWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {


 
            
            labelTrackCount.Content= GPXFile.TrackCount.ToString();
            if (GPXFile.FileInfo != null)
            {
                labelFileName.Content = GPXFile.FileInfo.FullName;
            }
            else
            {
                labelFileName.Content = GPXFile.FileName;
            }



            if (GPXFile.WaypointCount > 0)
            {
                labelWaypointLabel.Content = "Number of waypoints";
                labelWaypointCount.Content = GPXFile.WaypointCount.ToString();
                labelGPXType.Content = "Waypoints";
                dataGridNamedWaypoints.DataContext = GPXFile.NamedWaypointsInLocalTime;
            }
            else
            {
                //foreach(var item in GPXFile.w)
                labelGPXType.Content = "Track waypoints";
                dataGridNamedWaypoints.DataContext = GPXFile.TrackWaypoinsInLocalTime;
                labelWaypointCount.Content = GPXFile.TrackPointsCount.ToString();
                labelWaypointLabel.Content = "Number of track pts.";

            }
            dataGridNamedWaypoints.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });
            dataGridNamedWaypoints.Columns.Add(new DataGridTextColumn { Header = "Longitude", Binding = new Binding("Longitude") });
            dataGridNamedWaypoints.Columns.Add(new DataGridTextColumn { Header = "Latitude", Binding = new Binding("Latitude") });
            //dataGridNamedWaypoints.Columns.Add(new DataGridTextColumn { Header = "Time", Binding = new Binding("Time") });
            
            var col = new DataGridTextColumn()
            {
                Binding = new Binding("Time"),
                Header = "Time stamp"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridNamedWaypoints.Columns.Add(col);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            this.SavePlacement(); 
        }

        public GPXFile GPXFile { get; set; }
        public GPSWaypointSet GPSWaypointSet { get; set; }
        public void Dispose()
        {

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        public MainWindow ParentWindow { get; set; }
        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            switch(((Button)sender).Name)
            {
                case "buttonOk":
                    Close();
                    break;
            }
        }
    }
}
