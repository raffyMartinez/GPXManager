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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using GPXManager.entities;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using xceedPropertyGrid = Xceed.Wpf.Toolkit.PropertyGrid;
using System.ComponentModel;
using Ookii.Dialogs.Wpf;
using System.Windows.Media.Media3D;
using GPXManager.views;
using System.Windows.Media.Animation;
using System.Data;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using GPXManager.entities.mapping;
using MapWinGIS;
using AxMapWinGIS;

namespace GPXManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _inArchive;
        private string _deviceSerialNumber;
        private string _selectedProperty;
        private DetectedDevice _detectedDevice;
        private ComboBox _cboBrand;
        private ComboBox _cboModel;
        private GPS _gps;
        private GPXFile _gpxFile;
        private bool _isTrackGPX;
        private bool _isNew;
        private Trip _selectedTrip;
        private TripWaypoint _selectedTripWaypoint;
        private bool _gpsPropertyChanged;
        private string _changedPropertyName;
        private TreeViewItem _gpsTreeViewItem;
        private DateTime _tripMonthYear;
        private bool _usbGPSPresent;
        private bool _inDeviceNode;
        private List<TripWaypoint> _tripWaypoints;

        public DataGrid CurrentDataGrid { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
            _usbGPSPresent = false;
            treeDevices.MouseRightButtonDown += TreeDevices_MouseRightButtonDown;
        }

        private void TreeDevices_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_inDeviceNode)
            {
                ContextMenu cm = new ContextMenu();


                MenuItem m = new MenuItem { Header = "Eject device", Name = "menuEjectDevice" };
                m.Click += OnMenuClick;
                cm.Items.Add(m);
                cm.IsOpen = true;
            }
        }

        public void ResetDataGrids()
        {
            dataGridGPXFiles.Items.Refresh();
            dataGridTrips.Items.Refresh();
            dataGridGPSSummary.Items.Refresh();
        }

        private void Cleanup()
        {
            _detectedDevice = null;
            _gps = null;
            _gpxFile = null;
            _selectedTrip = null;
            _selectedTripWaypoint = null;
            MapWindowManager.CleanUp(true);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (_usbGPSPresent)
            {
                Entities.DeviceGPXViewModel.SaveDeviceGPXToRepository();
            }
            Cleanup();
        }

        private void SetupEntities()
        {
            Entities.GPSViewModel = new GPSViewModel();
            Entities.DetectedDeviceViewModel = new DetectedDeviceViewModel();
            Entities.GPXFileViewModel = new GPXFileViewModel();
            Entities.GearViewModel = new GearViewModel();
            Entities.TripViewModel = new TripViewModel();
            Entities.WaypointViewModel = new WaypointViewModel();
            Entities.TrackViewModel = new TrackViewModel();
            Entities.TripWaypointViewModel = new TripWaypointViewModel();
            Entities.DeviceGPXViewModel = new DeviceGPXViewModel();
            Entities.AOIViewModel = new AOIViewModel();

            _cboBrand = new ComboBox();
            _cboModel = new ComboBox();

            _cboModel.Name = "cboModel";
            _cboBrand.Name = "cboBrand";

            _cboBrand.SelectionChanged += OnComboSelectionChanged;
            _cboModel.SelectionChanged += OnComboSelectionChanged;

            _cboBrand.ItemsSource = Entities.GPSViewModel.GPSBrands;
            _cboModel.ItemsSource = Entities.GPSViewModel.GPSModels;

            ConfigureGrids();

            statusLabel.Content = Global.MDBPath;
        }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ResetView();

            if (Global.AppProceed)
            {
                if (File.Exists(Global.MDBPath))
                {
                    SetupEntities();
                }
                else
                {
                    statusLabel.Content = "Path to backend database not found";
                }
            }
            else
            {
                if (Global.MDBPath == null)
                {
                    statusLabel.Content = "Application need to be setup first";
                }
                else if (Global.MDBPath.Length > 0 && File.Exists(Global.MDBPath))
                {
                    statusLabel.Content = "Application need to be setup first";
                }
                else
                {
                    statusLabel.Content = "Path to backend database not found";
                }
            }

            if (Debugger.IsAttached)
            {
                menuClearTables.Visibility = Visibility.Visible;
                //menuMapper.Visibility = Visibility.Visible;
            }

            SetMapButtonsEnabled();
        }

        private void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cbo = (ComboBox)sender;
            if (cbo.SelectedItem != null && cbo.SelectedItem.ToString().Length > 0)
            {
                switch (cbo.Name)
                {
                    case "cboModel":

                        foreach (xceedPropertyGrid.PropertyItem prp in PropertyGrid.Properties)
                        {
                            if (prp.DisplayName == "Model")
                            {
                                prp.Value = cbo.SelectedItem;
                            }
                        }

                        break;
                    case "cboBrand":
                        if (cbo.SelectedItem != null)
                        {
                            _cboModel.ItemsSource = Entities.GPSViewModel.GetModels(cbo.SelectedItem.ToString());
                            _cboModel.SelectedItem = null;
                        }
                        foreach (xceedPropertyGrid.PropertyItem prp in PropertyGrid.Properties)
                        {
                            if (prp.DisplayName == "Brand")
                            {
                                prp.Value = cbo.SelectedItem;
                            }
                        }
                        break;
                }
            }
        }

        private bool ReadUSBDrives()
        {
            if (Entities.DetectedDeviceViewModel == null)
            {
                if (!Global.AppProceed)
                {
                    MessageBox.Show("Application need to be setup first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return false;
            }
            else if (Entities.DetectedDeviceViewModel.ScanUSBDevices() == 0)
            {
                MessageBox.Show("No USB storage devices detected", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void ShowEditTripWindow(bool isNew, int tripID, string operatorName = "", string vesselName = "", string gearCode = "", bool showWaypoints = false)
        {
            using (EditTripWindow etw = new EditTripWindow
            {
                ParentWindow = this,
                IsNew = isNew,
                TripID = tripID,
                DeviceID = _deviceSerialNumber,
                GPS = _gps,
                OperatorName = operatorName,
                VesselName = vesselName,
                GearCode = gearCode,
                GPXFile = _gpxFile
            })
            {
                if (!isNew)
                {
                    etw.TripID = tripID;
                }
                if ((bool)etw.ShowDialog())
                {
                    if (_gpxFile != null)
                    {
                        //ShowTripData();
                        //var parent = ((TreeViewItem)treeDevices.SelectedItem).Parent as TreeViewItem;
                        //int index = parent.Items.IndexOf((TreeViewItem)treeDevices.SelectedItem);
                        //((TreeViewItem)parent.Items[index + 1]).IsSelected = true;
                        //((TreeViewItem)parent.Items[index]).IsSelected = true;
                    }
                    //dataGridTrips.ItemsSource = Entities.TripViewModel.GetAllTrips(_deviceSerialNumber);
                    //dataGridTrips.Items.Refresh();
                    //buttonDeleteTrip.IsEnabled = false;
                    //buttonEditTrip.IsEnabled = false;
                    dataGridGPXFiles.Items.Refresh();
                }
            }
        }
        private void ShowEditTripWaypointWindow(bool isNew)
        {
            using (EditTripWaypointsWindow etw = new EditTripWaypointsWindow { Trip = _selectedTrip, IsNew = isNew })
            {
                if (!isNew)
                {
                    etw.TripWaypointID = _selectedTripWaypoint.RowID;
                }

                if ((bool)etw.ShowDialog())
                {
                    dataGridTripWaypoints.ItemsSource = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                    dataGridTrips.ItemsSource = Entities.TripViewModel.GetAllTrips(_deviceSerialNumber);
                }
            }

        }

        private void AddTrip()
        {
            var trip = Entities.TripViewModel.GetLastTripOfDevice(_deviceSerialNumber);
            if (trip != null)
            {
                ShowEditTripWindow(isNew: true, Entities.TripViewModel.NextRecordNumber, trip.OperatorName, trip.VesselName, trip.Gear.Code);
            }
            else
            {
                ShowEditTripWindow(isNew: true, Entities.TripViewModel.NextRecordNumber);
            }
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonArchiveGPX":
                    if( Entities.DeviceGPXViewModel.SaveDeviceGPXToRepository(_detectedDevice))
                    {
                        buttonArchiveGPX.Visibility = Visibility.Collapsed;
                        dataGridGPXFiles.Items.Refresh();
                        MessageBox.Show("GPX data successfully archived", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "buttonEjectDevice":
                    EjectDevice();
                    break;
                case "buttonGPXDetails":
                    ShowGPXFileDetails();
                    break;
                case "buttonAddWaypoint":
                    ShowEditTripWaypointWindow(true);
                    break;
                case "buttonEditWaypoint":
                    ShowEditTripWaypointWindow(false);
                    break;
                case "buttonDeleteWaypoint":
                    if (Entities.TripWaypointViewModel.DeleteRecordFromRepo(_selectedTripWaypoint.RowID))
                    {
                        dataGridTripWaypoints.ItemsSource = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                        buttonEditWaypoint.IsEnabled = false;
                        buttonDeleteWaypoint.IsEnabled = false;
                        _selectedTripWaypoint = null;
                    }
                    break;
                case "buttonAddTrip":
                    _gpxFile = null;
                    AddTrip();
                    break;
                case "buttonEditTrip":
                    ShowEditTripWindow(false, _selectedTrip.TripID);
                    break;
                case "buttonDeleteTrip":
                    if (Entities.TripViewModel.DeleteRecordFromRepo(_selectedTrip.TripID))
                    {
                        dataGridTrips.ItemsSource = Entities.TripViewModel.GetAllTrips(_deviceSerialNumber);
                        stackPanelTripWaypoints.Visibility = Visibility.Collapsed;
                        buttonDeleteTrip.IsEnabled = false;
                        buttonEditTrip.IsEnabled = false;
                        _selectedTrip = null;
                    }
                    break;
                case "buttonOk":

                    break;
                case "buttonCancel":
                    break;
                case "buttonSave":
                    _gps.Device = _detectedDevice;
                    var result = Entities.GPSViewModel.ValidateGPS(_gps, _isNew, "", "");
                    if (result.ErrorMessage.Length == 0)
                    {
                        Entities.GPSViewModel.AddRecordToRepo(_gps);
                        TreeViewItem selectedItem = (TreeViewItem)treeDevices.SelectedItem;
                        selectedItem.Header = _gps.DeviceName;
                        ((TreeViewItem)selectedItem.Items[0]).Header = $"{_detectedDevice.Disks[0].Caption}\\{_gps.Folder}";
                        ((TreeViewItem)selectedItem.Items[0]).Tag = "gpx_folder";
                        _detectedDevice.GPS = _gps;
                        Entities.GPXFileViewModel.GetFilesFromDevice(_detectedDevice);
                        //Entities.DeviceGPXViewModel.RefreshArchivedGPXCollection(_gps);
                        AddTripNode(selectedItem);
                        ShowGPXMonthNodes((TreeViewItem)selectedItem.Items[0], _gps);
                        buttonSave.Visibility = Visibility.Collapsed;
                        buttonEjectDevice.Visibility = Visibility.Visible;
                        selectedItem.IsExpanded = true;
                    }
                    else
                    {
                        MessageBox.Show(result.ErrorMessage, "Validation error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
            }
        }

        private void SetGPXFileMenuMapVisibility(bool hideMapVisibilityMenu, bool refreshGrid = true)
        {
            if (hideMapVisibilityMenu)
            {
                menuGPXMap.Visibility = Visibility.Collapsed;
                menuGPXRemoveFromMap.Visibility = Visibility.Visible;
                menuGPXRemoveAllFromMap.Visibility = Visibility.Visible;

            }
            else
            {
                menuGPXMap.Visibility = Visibility.Visible;
                menuGPXRemoveFromMap.Visibility = Visibility.Collapsed;
                menuGPXRemoveAllFromMap.Visibility = Visibility.Collapsed;
            }

            if (refreshGrid)
            {
                dataGridGPXFiles.Items.Refresh();
            }
        }
        private void ShowTripMap(bool showInMap = true)
        {
            int h = -1;
            List<int> handles = new List<int>();
            MapWindowManager.OpenMapWindow(this, true);
            if (MapWindowManager.Coastline == null)
            {
                MapWindowManager.LoadCoastline(MapWindowManager.CoastLineFile);
            }

            var datagrid = (DataGrid)LayerSelector;
            if (datagrid.SelectedItems.Count > 0)
            {
                foreach (Trip item in datagrid.SelectedItems)
                {

                    MapWindowManager.MapTrip(item, out h, out handles);
                    item.ShapeIndexes = handles;
                    item.ShownInMap = showInMap;
                }
            }

            if(_tripWaypoints.Count>0)
            {
                MapWindowManager.MapTripWaypoints(_tripWaypoints,out h, out handles, _gps, _tripWaypoints[0].Trip.GPXFileName);

            }


            SetGPXFileMenuMapVisibility(h > 0);
            datagrid.SelectedItems.Clear();
            datagrid.Items.Refresh();
            MapWindowManager.MapControl.Redraw();
        }

        private void SetMapButtonsEnabled()
        {
            bool itemEnabled = Global.MapOCXInstalled;
            double buttonOpacity = .20d;
            buttonMap.IsEnabled = itemEnabled;
            menuMapper.IsEnabled = itemEnabled;
            menuCalendaredTripMap.IsEnabled = itemEnabled;
            menuGPXMap.IsEnabled = itemEnabled;
            menuGPXRemoveAllFromMap.IsEnabled = itemEnabled;
            menuGPXRemoveFromMap.IsEnabled = itemEnabled;
            menuTripMap.IsEnabled = itemEnabled;

            if(!itemEnabled)
            {
                buttonMap.Opacity = buttonOpacity;
                menuMapper.Opacity = buttonOpacity;
                menuCalendaredTripMap.Opacity = buttonOpacity;
                menuGPXMap.Opacity = buttonOpacity;
                menuGPXRemoveAllFromMap.Opacity = buttonOpacity;
                menuGPXRemoveFromMap.Opacity = buttonOpacity;
                menuTripMap.Opacity = buttonOpacity;
            }
        }
        private void ShowGPXOnMap(bool showInMap = true)
        {
            int h = -1;
            List<int> handles = new List<int>();
            string coastLineFile = $@"{globalMapping.ApplicationPath}\Layers\Coastline\philippines_polygon.shp";
            MapWindowManager.OpenMapWindow(this, true);
            if (MapWindowManager.Coastline == null)
            {
                MapWindowManager.LoadCoastline(coastLineFile);
            }


            if (dataGridGPXFiles.SelectedItems.Count > 0)
            {
                foreach (var item in dataGridGPXFiles.SelectedItems)
                {
                    _gpxFile = (GPXFile)item;
                    MapWindowManager.MapGPX(_gpxFile, out h, out handles);
                    _gpxFile.ShapeIndexes = handles;
                    _gpxFile.ShownInMap = showInMap;
                }
            }
            SetGPXFileMenuMapVisibility(h > 0);
            dataGridGPXFiles.SelectedItems.Clear();
            MapWindowManager.MapControl.Redraw();
        }
        private void ShowGPXFileDetails()
        {
            if (_inArchive || Entities.WaypointViewModel.Waypoints.ContainsKey(_gps) )
            {

                //var gpsWaypointSet = Entities.WaypointViewModel.Waypoints[_gps].Where(t => t.FullFileName == _gpxFile.FileInfo.FullName).FirstOrDefault();
                
                using (GPXFIlePropertiesWindow gpw = new GPXFIlePropertiesWindow
                {
                    ParentWindow = this,
                    GPXFile = _gpxFile,
                    Owner = this,
                    //GPSWaypointSet = gpsWaypointSet
                })
                {
                    gpw.ShowDialog();
                }
            }

        }

        private TreeViewItem AddTripNode(TreeViewItem parent)
        {
            TreeViewItem tripData = new TreeViewItem { Header = "Trip log", Tag = "trip_data" };
            parent.Items.Add(tripData);
            return tripData;
        }
        private void SelectBrandModel(ShowMode showMode, string brand = "")
        {
            using (var selectWindow = new GPSBrandModelWindow())
            {
                selectWindow.Owner = this;
                selectWindow.ShowMode = showMode;
                selectWindow.Brand = brand;
                if ((bool)selectWindow.ShowDialog())
                {
                    switch (showMode)
                    {
                        case ShowMode.ShowModeBrand:
                            _cboBrand.ItemsSource = Entities.GPSViewModel.GPSBrands;
                            break;
                        case ShowMode.ShowModeModel:
                            _cboModel.ItemsSource = Entities.GPSViewModel.GPSModels;
                            break;
                    }
                }
            }
        }
        private void ShowCalendarTree()
        {
            labelTitle.Content = "Calendar of tracked fishing operations by GPS";
            treeCalendar.Visibility = Visibility.Visible;
            var root = (TreeViewItem)treeCalendar.Items[0];
            var gps_root = (TreeViewItem)treeCalendar.Items[1];

            root.Items.Clear();
            gps_root.Items.Clear();

            foreach (var month in Entities.TripViewModel.GetMonthYears().OrderBy(t => t.Date))
            {
                root.Items.Add(new TreeViewItem { Header = month.ToString("MMM-yyyy"), Tag = month });
            }
            root.IsExpanded = true;

            foreach (var gps in Entities.GPSViewModel.GPSCollection.OrderBy(t => t.DeviceName))
            {
                gps_root.Items.Add(new TreeViewItem { Header = gps.DeviceName, Tag = gps.DeviceID });
            }
            if (root.Items.Count > 0)
            {
                ((TreeViewItem)root.Items[0]).IsSelected = true;
            }
            gps_root.IsExpanded = true;

            if (gps_root.Items.Count == 0 && root.Items.Count == 0)
            {
                labelNoData.Visibility = Visibility.Visible;
                labelNoData.Content = "There are no trips saved in the database";
                labelTitle.Visibility = Visibility.Hidden;
                treeCalendar.Visibility = Visibility.Collapsed;
            }
        }
        private void HideTrees()
        {
            treeCalendar.Visibility = Visibility.Collapsed;
            treeDevices.Visibility = Visibility.Collapsed;
            treeArchive.Visibility = Visibility.Collapsed;
        }

        private void ShowMap()
        {
            if (Global.AppProceed)
            {
                MapWindowManager.OpenMapWindow(this);
            }
            else
            {
                MessageBox.Show("Application need to be setp first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void EjectDevice()
        {
            string outMessage;
            if (Entities.DetectedDeviceViewModel.EjectDrive(_detectedDevice, out outMessage))
            {
                if(Entities.GPSViewModel.RemoveByEject(_detectedDevice.GPS))
                {
                    TreeViewItem tvi = treeDevices.SelectedItem as TreeViewItem;
                    tvi.IsSelected = false;
                    tvi.Items.Clear();
                    ((TreeViewItem)treeDevices.Items[0]).Items.Remove(tvi);
                }
            }
            MessageBox.Show(outMessage, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            string menuName = ((MenuItem)sender).Name;
            switch (menuName)
            {
                case "menuHelpAbout":
                    AboutWindow aw = new AboutWindow();
                    aw.Owner = this;
                    aw.ShowDialog();
                    break;
                case "menuCalendaredTripMap":
                case "menuTripMap":
                    ShowTripMap();
                    break;
                case "menuArchive":
                    ShowArchive();
                    break;
                case "menuEjectDevice":
                    EjectDevice();
                    break;
                case "menuGPXRemoveAllFromMap":
                    if (dataGridGPXFiles.SelectedItems.Count > 0)
                    {
                        MapWindowManager.RemoveLayerByKey("gpxfile_track");
                        MapWindowManager.RemoveLayerByKey("gpxfile_waypoint");
                        GPXMappingManager.RemoveAllFromMap();
                        dataGridGPXFiles.Items.Refresh();
                    }
                    break;
                case "menuGPXRemoveFromMap":
                    _gpxFile.ShownInMap = false;
                    if (_gpxFile.TrackCount > 0)
                    {
                        ((Shapefile)MapWindowManager.GPXTracksLayer.LayerObject).EditDeleteShape(_gpxFile.ShapeIndexes[0]);
                    }
                    else
                    {
                        foreach (int h in _gpxFile.ShapeIndexes)
                        {
                            ((Shapefile)MapWindowManager.GPXWaypointsLayer.LayerObject).EditDeleteShape(h);
                        }
                    }
                    MapWindowManager.MapControl.Redraw();
                    SetGPXFileMenuMapVisibility(false);
                    break;
                case "menuGPXMap":
                    ShowGPXOnMap();
                    break;
                case "menuMapper":
                    ShowMap();
                    break;
                case "menuGPXFileDetails":
                    ShowGPXFileDetails();
                    break;
                case "menuClearTables":
                    string result;
                    if (Entities.ClearTables())
                    {
                        result = "All tables cleared";
                    }
                    else
                    {
                        result = "Not all tables cleared";
                    }
                    MessageBox.Show(result, "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case "menuAddTripFromTRack":
                    AddTrip();
                    break;
                case "menuTripCalendar":
                    ShowTripCalendar();

                    break;
                case "menuSaveGPS":
                case "menuSaveTrips":
                    string dialogTitle = "";
                    switch (menuName)
                    {
                        case "menuSaveGPS":
                            dialogTitle = "Save GPS devices to an XML file";
                            break;
                        case "menuSaveTrips":
                            dialogTitle = "Save trips to an XML file";
                            break;
                    }

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Title = dialogTitle;
                    sfd.DefaultExt = ".xml";
                    sfd.Filter = "XML|*.xml|Text|*.txt";
                    sfd.FilterIndex = 1;

                    if ((bool)sfd.ShowDialog() && sfd.FileName.Length > 0)
                    {
                        switch (menuName)
                        {
                            case "menuSaveGPS":
                                Entities.GPSViewModel.Serialize(sfd.FileName);
                                break;
                            case "menuSaveTrips":
                                Entities.TripViewModel.Serialize(sfd.FileName);
                                break;
                        }
                    }
                    break;
                case "menuOptions":
                    ShowSettingsWindow();
                    break;
                case "menuGPSBrands":
                    SelectBrandModel(ShowMode.ShowModeBrand);
                    break;
                case "menuGPSModels":
                    SelectBrandModel(ShowMode.ShowModeModel);
                    break;
                case "menuCloseApp":
                    Close();
                    break;
                case "menuScanDevices":
                    HideTrees();
                    treeDevices.Visibility = Visibility.Visible;
                    ScanUSBDevices();
                    break;
                case "menuGPXFolder":
                    LocateGPXFolder();
                    break;
            }
        }

        private void ShowTripCalendar()
        {
            if (Global.AppProceed)
            {
                _inArchive = true;
                HideTrees();
                ResetView();
                ShowCalendarTree();
            }
            else
            {
                MessageBox.Show("Application need to be setup first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowSettingsWindow()
        {
            using (var settingsWindow = new SettingsWindow())
            {
                settingsWindow.Owner = this;
                settingsWindow.ParentWindow = this;
                if ((bool)settingsWindow.ShowDialog() && Global.AppProceed)
                {
                    SetupEntities();
                }
            }
        }
        private void LocateGPXFolder()
        {
            if (_detectedDevice != null)
            {
                VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
                fbd.UseDescriptionForTitle = true;
                fbd.Description = "Locate GPX folder in selected device";
                if (Directory.Exists($"{_detectedDevice.Disks[0].Caption}\\{Global.Settings.DeviceGPXFolder}"))
                {
                    fbd.SelectedPath = $"{_detectedDevice.Disks[0].Caption}\\{Global.Settings.DeviceGPXFolder}";
                }
                if ((bool)fbd.ShowDialog() && fbd.SelectedPath.Length > 0)
                {
                    foreach (xceedPropertyGrid.PropertyItem prp in PropertyGrid.Properties)
                    {
                        if (prp.DisplayName == "Folder")
                        {
                            var path = fbd.SelectedPath.Substring(3);
                            prp.Value = path;
                            if (path != Global.Settings.DeviceGPXFolder)
                            {
                                Global.Settings.DeviceGPXFolder = path;
                                Global.Settings.Save(Global._DefaultSettingspath);
                            }
                        }
                    }
                }
            }
        }
        private void ScanUSBDevices()
        {
            _inArchive = false;
            ResetView();
            ConfigureGPXGrid();
            if (ReadUSBDrives())
            {
                foreach (var device in Entities.DetectedDeviceViewModel.DetectedDeviceCollection)
                {
                    var tvi = new TreeViewItem
                    {
                        Header = $"{device.Caption} ({device.SerialNumber})",
                        Tag = device.SerialNumber
                    };
                    if (device.GPS != null)
                    {
                        var gpsItem = new TreeViewItem
                        {
                            Header = $"{device.Disks[0].Caption}\\{device.GPS.Folder}",
                            Tag = "gpx_folder",
                        };
                        tvi.Header = device.GPS.DeviceName;
                        tvi.Items.Add(gpsItem);
                        AddTripNode(tvi);
                        GetGPXFiles(device);
                        ShowGPXMonthNodes(gpsItem, device.GPS);
                        _usbGPSPresent = true;

                    }
                    else
                    {
                        foreach (var disk in device.Disks)
                        {
                            var subItem = new TreeViewItem
                            {
                                Header = $"Drive {disk.Caption}",
                                Tag = "disk"
                            };
                            tvi.Items.Add(subItem);
                        }
                    }
                    tvi.IsExpanded = true;
                    TreeViewItem root = (TreeViewItem)treeDevices.Items[0];
                    if (root.Items.Count == 0)
                    {
                        root.Items.Add(tvi);
                    }
                    else
                    {
                        if (!TreeItemExists(root, tvi))
                        {
                            root.Items.Add(tvi);
                        }
                    }
                }
                ((TreeViewItem)treeDevices.Items[0]).IsExpanded = true;
            }

        }

        private void ShowGPXMonthNodes(TreeViewItem parent, GPS gps)
        {
            foreach (var item in Entities.GPXFileViewModel.FilesByMonth(gps).Keys)
            {
                int h = parent.Items.Add(new TreeViewItem { Header = item.ToString("MMM-yyyy") });
                TreeViewItem monthNode = parent.Items[h] as TreeViewItem;
                monthNode.Tag = "month_node";
            }
            parent.IsExpanded = true;
        }

        private bool TreeItemExists(TreeViewItem parent, TreeViewItem testItem)
        {
            foreach (TreeViewItem item in parent.Items)
            {
                if (item.Tag.ToString() == testItem.Tag.ToString())
                {
                    return true;
                }
            }
            return false;
        }
        private void SetupDevicePropertyGrid()
        {
            PropertyGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "DeviceName", DisplayName = "Device name", DisplayOrder = 1, Description = "Name assigned to the device" });

            var definition = new xceedPropertyGrid.PropertyDefinition { Name = "Code", DisplayName = "Device code", DisplayOrder = 2, Description = "Code assigned to the device" };
            PropertyGrid.PropertyDefinitions.Add(definition);

            PropertyGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "Brand", DisplayName = "Brand", DisplayOrder = 3, Description = "Brand of device" });
            PropertyGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "Model", DisplayName = "Model", DisplayOrder = 4, Description = "Model of device" });
            PropertyGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "Folder", DisplayName = "Folder", DisplayOrder = 5, Description = "Folder where GPX files are saved" });
            PropertyGrid.PropertyDefinitions.Add(new xceedPropertyGrid.PropertyDefinition { Name = "DeviceID", DisplayName = "Device ID", DisplayOrder = 6, Description = "Identifier of device" });

            foreach (xceedPropertyGrid.PropertyItem prp in PropertyGrid.Properties)
            {
                switch (prp.DisplayName)
                {
                    case "Brand":
                        prp.Editor = _cboBrand;
                        break;
                    case "Model":
                        prp.Editor = _cboModel;
                        break;
                }
            }

        }
        private void NewGPS()
        {
            _isNew = true;
            _cboBrand.SelectedItem = null;
            _cboModel.ItemsSource = null;

            labelTitle.Content = "Add this USB storage as a GPS to the database";
            _gps = new GPS
            {
                DeviceID = _deviceSerialNumber
            };
            //PopertyGrid.Visibility = Visibility.Visible;
            gpsPanel.Visibility = Visibility.Visible;
            buttonEjectDevice.Visibility = Visibility.Collapsed;
            PropertyGrid.SelectedObject = _gps;
            SetupDevicePropertyGrid();
            buttonSave.Visibility = Visibility.Visible;

        }

        private void ConfigureGPXGrid(bool fromDevice = true)
        {
            DataGridTextColumn col;
            dataGridGPXFiles.Columns.Clear();
            dataGridGPXFiles.AutoGenerateColumns = false;

            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "File name", Binding = new Binding("FileName") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Date range", Binding = new Binding("DateRange") });

            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Waypoints", Binding = new Binding("WaypointCount") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Tracks", Binding = new Binding("TrackCount") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Track points", Binding = new Binding("TrackPointsCount") });
            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Time span",  Binding = new Binding("TimeSpanHourMinute") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("TrackLength"),
                Header = "Length (km)"
            };
            col.Binding.StringFormat = "N3";
            dataGridGPXFiles.Columns.Add(col);

            dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Trips", Binding = new Binding("TripCount") });
            dataGridGPXFiles.Columns.Add(new DataGridCheckBoxColumn { Header = "Mapped", Binding = new Binding("ShownInMap") });            

            if (fromDevice)
            {
                dataGridGPXFiles.Columns.Add(new DataGridCheckBoxColumn { Header = "Archived", Binding = new Binding("IsArchived") });
                dataGridGPXFiles.Columns.Add(new DataGridTextColumn { Header = "Size", Binding = new Binding("SizeFormatted") });

                col = new DataGridTextColumn()
                {
                    Binding = new Binding("TimeStampUTC"),
                    Header = "Date created"
                };
                col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
                dataGridGPXFiles.Columns.Add(col);

                col = new DataGridTextColumn()
                {
                    Binding = new Binding("DateModifiedUTC"),
                    Header = "Date modified"
                };
                col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
                dataGridGPXFiles.Columns.Add(col);
            }
            
        }
        private void ConfigureGrids()
        {
            DataGridTextColumn col;

            //setup trip data grid
            dataGridTrips.AutoGenerateColumns = false;
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Trip ID", Binding = new Binding("TripID") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Operator", Binding = new Binding("OperatorName") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Fishing vessel", Binding = new Binding("VesselName") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Gear", Binding = new Binding("Gear.Name") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Other gear", Binding = new Binding("OtherGear") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeDeparture"),
                Header = "Departure"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridTrips.Columns.Add(col);

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeArrival"),
                Header = "Arrival"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridTrips.Columns.Add(col);

            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Track source GPX", Binding = new Binding("Track.FileName") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Waypoints", Binding = new Binding("WaypointCount") });
            dataGridTrips.Columns.Add(new DataGridTextColumn { Header = "Summary (Length:km Duration Hours:Minutes)", Binding = new Binding("TrackSummary") });



            //setup trip waypoints data grid
            dataGridTripWaypoints.AutoGenerateColumns = false;
            dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Row ID", Binding = new Binding("RowID") });
            dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Waypoint", Binding = new Binding("WaypointName") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("TimeStamp"),
                Header = "Waypoint timestamp"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridTripWaypoints.Columns.Add(col);

            dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Waypoint source GPX", Binding = new Binding("WaypointGPXFileName") });
            dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Waypoint type", Binding = new Binding("WaypointType") });
            dataGridTripWaypoints.Columns.Add(new DataGridTextColumn { Header = "Set #", Binding = new Binding("SetNumber") });


            //setup GPS summary grid
            dataGridGPSSummary.AutoGenerateColumns = false;
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Trip ID", Binding = new Binding("TripID") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "GPS", Binding = new Binding("GPS.DeviceName") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Name of operator", Binding = new Binding("OperatorName") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Name of fishing vessel", Binding = new Binding("VesselName") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Gear", Binding = new Binding("Gear.Name") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Other gear", Binding = new Binding("OtherGear") });

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeDeparture"),
                Header = "Date and time departed"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridGPSSummary.Columns.Add(col);

            col = new DataGridTextColumn()
            {
                Binding = new Binding("DateTimeArrival"),
                Header = "Date and time arrived"
            };
            col.Binding.StringFormat = "MMM-dd-yyyy HH:mm";
            dataGridGPSSummary.Columns.Add(col);

            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Waypoints", Binding = new Binding("WaypointCount") });
            dataGridGPSSummary.Columns.Add(new DataGridTextColumn { Header = "Summary (Length:km Duration Hours:Minutes)", Binding = new Binding("TrackSummary") });
            dataGridGPSSummary.Columns.Add(new DataGridCheckBoxColumn { Header = "Mapped", Binding = new Binding("ShownInMap") });
        }
        public GPS GPS { get; set; }

        private void GetGPXFiles(DetectedDevice device)
        {
            Entities.GPXFileViewModel.GetFilesFromDevice(device);
        }

        private void ShowGPXFolderLatest(DetectedDevice device, int latestCount = 10)
        {
            gpxPanel.Visibility = Visibility.Visible;
            buttonGPXDetails.IsEnabled = false;
            _gpxFile = null;

            List<GPXFile> tracks = Entities.GPXFileViewModel.GetFiles(device.SerialNumber)
                .Where(t => t.TrackCount > 0)
                .OrderByDescending(t => t.DateRangeStart)
                .Take(latestCount)
                .ToList();

            List<GPXFile> waypoints = Entities.GPXFileViewModel.GetFiles(device.SerialNumber)
                .Where(t => t.WaypointCount > 0)
                .OrderByDescending(t => t.DateRangeStart)
                .Take(latestCount)
                .ToList();

            PopulateGPXDataGrid(tracks.Union(waypoints).ToList());
            //dataGridGPXFiles.ItemsSource = tracks.Union(waypoints).ToList();
            //CurrentDataGrid = dataGridGPXFiles;
        }

        private void PopulateGPXDataGrid(List<GPXFile> gpxFiles)
        {
            dataGridGPXFiles.ItemsSource = gpxFiles;
            if (gpxFiles.Count(t=>t.IsArchived==false)>0)
            {
                buttonArchiveGPX.Visibility = Visibility.Visible;
            }
            CurrentDataGrid = dataGridGPXFiles;
        }
        private void ShowGPXFolder(DetectedDevice device, string month_year = "")
        {
            labelTitle.Visibility = Visibility.Visible;
            labelTitle.Content = $"GPX files saved in GPS for {DateTime.Parse(month_year).ToString("MMMM, yyyy")}";
            gpxPanel.Visibility = Visibility.Visible;
            buttonGPXDetails.IsEnabled = false;
            _gpxFile = null;
            if (month_year.Length > 0)
            {
                PopulateGPXDataGrid(Entities.GPXFileViewModel.GetFiles(device.SerialNumber, DateTime.Parse(month_year)));
                //dataGridGPXFiles.ItemsSource = Entities.GPXFileViewModel.GetFiles(device.SerialNumber, DateTime.Parse(month_year));
            }
            else
            {
                PopulateGPXDataGrid(Entities.GPXFileViewModel.GetFiles(device.SerialNumber));
                //dataGridGPXFiles.ItemsSource = Entities.GPXFileViewModel.GetFiles(device.SerialNumber);
            }

            //CurrentDataGrid = dataGridGPXFiles;
        }
        private void ShowGPS()
        {
            _isNew = false;
            gpsPanel.Visibility = Visibility.Visible;
            //PropertyGrid.Visibility = Visibility.Visible;
            gpsPanel.Visibility = Visibility.Visible;
            GPSEdited gpsEdited = new GPSEdited(_gps);
            PropertyGrid.SelectedObject = gpsEdited;


            SetupDevicePropertyGrid();
            _cboBrand.SelectedItem = gpsEdited.Brand;
            _cboModel.SelectedItem = gpsEdited.Model;
            //buttonEjectDevice.Visibility = Visibility.Visible;
        }

        private void ResetView()
        {
            gpsPanel.Visibility = Visibility.Collapsed;
            //PropertyGrid.Visibility = Visibility.Collapsed;
            gpxPanel.Visibility = Visibility.Collapsed;
            menuGPXFolder.Visibility = Visibility.Collapsed;
            buttonSave.Visibility = Visibility.Collapsed;
            menuGPSBrands.Visibility = Visibility.Collapsed;
            menuGPSModels.Visibility = Visibility.Collapsed;
            tripPanel.Visibility = Visibility.Collapsed;
            stackPanelTripWaypoints.Visibility = Visibility.Collapsed;
            dataGridCalendar.Visibility = Visibility.Collapsed;
            dataGridGPSSummary.Visibility = Visibility.Collapsed;
            labelNoData.Visibility = Visibility.Collapsed;
            labelTitle.Content = "";
            labelDeviceName.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Collapsed;
            buttonEjectDevice.Visibility = Visibility.Visible;
            labelCalendarMonth.Visibility = Visibility.Collapsed;
        }
        private void ShowTripData()
        {
            dataGridTrips.ItemsSource = Entities.TripViewModel.GetAllTrips(_deviceSerialNumber);
            dataGridTrips.IsReadOnly = true;
            tripPanel.Visibility = Visibility.Visible;
            stackPanelTripWaypoints.Visibility = Visibility.Collapsed;
            buttonDeleteTrip.IsEnabled = false;
            buttonEditTrip.IsEnabled = false;
            _selectedTrip = null;
            CurrentDataGrid = dataGridTrips;
        }

        private void ShowTripWaypoints(bool fromGPSSummary = false)
        {
            //_tripWaypoints = new List<TripWaypoint>();
            if (fromGPSSummary)
            {
                tripPanel.Children.Remove(stackPanelTripWaypoints);
                if (!panelMain.Children.Contains(stackPanelTripWaypoints))
                {
                    panelMain.Children.Add(stackPanelTripWaypoints);
                    stackPanelTripWaypoints.Margin = new Thickness(10);
                }
            }
            else
            {
                if (panelMain.Children.Contains(stackPanelTripWaypoints))
                {
                    panelMain.Children.Remove(stackPanelTripWaypoints);
                    tripPanel.Children.Add(stackPanelTripWaypoints);
                }
            }

            stackPanelTripWaypoints.Visibility = Visibility.Visible;
            buttonEditWaypoint.IsEnabled = false;
            buttonDeleteWaypoint.IsEnabled = false;
            _selectedTripWaypoint = null;

            if (_selectedTrip != null)
            {
                _tripWaypoints = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                if (_tripWaypoints.Count > 0)
                {
                    dataGridTripWaypoints.ItemsSource = _tripWaypoints;
                }
                else
                {
                    dataGridTripWaypoints.ItemsSource = null;
                }
            }
        }
        private void SaveChangesToGPS()
        {
            if (_gpsPropertyChanged)
            {
                //if (MessageBox.Show("Save changes to GPS?", "Save changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                //{

                var editedGPS = (GPSEdited)PropertyGrid.SelectedObject;
                bool gpxFolderExists = Directory.Exists($"{_detectedDevice.Disks[0].Caption}\\{editedGPS.Folder}");

                if (gpxFolderExists)
                {
                    _gps = new GPS
                    {
                        Brand = editedGPS.Brand,
                        Model = editedGPS.Model,
                        DeviceID = editedGPS.DeviceID,
                        DeviceName = editedGPS.DeviceName,
                        Folder = editedGPS.Folder,
                        Code = editedGPS.Code
                    };
                    Entities.GPSViewModel.UpdateRecordInRepo(_gps);


                    switch (_changedPropertyName)
                    {
                        case "Folder":
                            var updatedGPXFolder = $"{_detectedDevice.Disks[0].Caption}\\{_gps.Folder}";

                            var folderNode = (TreeViewItem)_gpsTreeViewItem.Items[0];

                            if (folderNode.Header.ToString() != updatedGPXFolder)
                            {
                                folderNode.Header = updatedGPXFolder;

                                _detectedDevice.GPS = _gps;
                                Entities.GPXFileViewModel.GetFilesFromDevice(_detectedDevice);
                            }


                            break;
                        case "DeviceName":
                            _gpsTreeViewItem.Header = _gps.DeviceName; ;
                            break;
                    }

                }
                else
                {
                    MessageBox.Show("GPX folder is not found", "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                _gpsPropertyChanged = false;
                //}
            }
        }

        private void ResetGrids()
        {

            dataGridTripWaypoints.SelectedItems.Clear();
            dataGridGPXFiles.SelectedItems.Clear();
            dataGridTrips.SelectedItems.Clear();
            //dataGridCalendar.SelectedItems.Clear();

        }
        private void OnTreeViewSelectedItemChange(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _inDeviceNode = false;
            ResetView();
            ResetGrids();
            switch (((TreeView)sender).Name)
            {
                case "treeArchive":
                    treeArchive.Visibility = Visibility.Visible;
                    var selectedNode = (TreeViewItem)treeArchive.SelectedItem;
                    if (selectedNode != null)
                    {
                        GPS gps;
                        DateTime month_year;
                        switch (selectedNode.Tag.GetType().Name)
                        {
                            case "string":
                                break;
                            case "GPS":
                                gps = (GPS)selectedNode.Tag;
                                break;
                            case "DateTime":
                                
                                gps = (GPS)((TreeViewItem)selectedNode.Parent).Tag;
                                month_year = (DateTime)selectedNode.Tag;

                                if(!Entities.DeviceGPXViewModel.ArchivedGPXFiles.Keys.Contains(gps))
                                {
                                    Entities.DeviceGPXViewModel.RefreshArchivedGPXCollection(gps);

                                }
                                if(Entities.DeviceGPXViewModel.DeviceGPXCollection.Count(t=>t.GPS.DeviceID==gps.DeviceID)!= Entities.DeviceGPXViewModel.ArchivedGPXFiles[gps].Count)
                                {
                                    Entities.DeviceGPXViewModel.RefreshArchivedGPXCollection(gps);
                                }

                                List<GPXFile> archivedGPX = Entities.DeviceGPXViewModel.ArchivedGPXFiles[gps]
                                    .Where(t => t.DateRangeStart >= month_year)
                                    .Where(t => t.DateRangeEnd <= month_year.AddMonths(1))
                                    .ToList();
                                gpxPanel.Visibility = Visibility.Visible;
                                dataGridGPXFiles.ItemsSource = archivedGPX;
                                labelTitle.Content = $"Archived content of GPS for {month_year.ToString("MMMM, yyyy")}";
                                labelTitle.Visibility = Visibility.Visible;
                                break;
                        }
                    }
                    break;
                case "treeCalendar":
                    var treeNode = (TreeViewItem)e.NewValue;
                    if (treeNode.Tag.ToString() != "root")
                    {
                        switch (((TreeViewItem)treeNode.Parent).Header)
                        {
                            case "Trip calendar":
                                labelTitle.Content = "Calendar of tracked fishing operations by GPS";
                                _tripMonthYear = (DateTime)(treeNode).Tag;
                                var tripCalendarVM = new TripCalendarViewModel(_tripMonthYear);
                                dataGridCalendar.Visibility = Visibility.Visible;
                                dataGridCalendar.DataContext = tripCalendarVM.DataTable;
                                labelCalendarMonth.Visibility = Visibility.Visible;
                                labelCalendarMonth.Content = _tripMonthYear.ToString("MMMM, yyyy");
                                break;
                            case "Trips by GPS":
                                labelTitle.Content = "Details of trips tracked by GPS";
                                dataGridGPSSummary.Visibility = Visibility.Visible;
                                //dataGridGPSSummary.AutoGenerateColumns = true;
                                _gps = Entities.GPSViewModel.GetGPSEx(treeNode.Tag.ToString());
                                var gpsTrips = Entities.TripViewModel.TripCollection
                                    .Where(t => t.GPS.DeviceID == _gps.DeviceID)
                                    .OrderBy(t => t.DateTimeDeparture).ToList();
                                dataGridGPSSummary.DataContext = gpsTrips;
                                break;
                        }
                    }
                    break;
                case "treeDevices":

                    if (e.NewValue != null)
                    {

                        var tag = ((TreeViewItem)e.NewValue).Tag.ToString();

                        labelTitle.Visibility = Visibility.Visible;
                        labelTitle.Content = "Add this device as GPS";

                        SaveChangesToGPS();
                        _gpsTreeViewItem = null;

                        if (tag != "root")
                        {
                            if (tag == "month_node")
                            {
                                _deviceSerialNumber = ((TreeViewItem)((TreeViewItem)((TreeViewItem)e.NewValue).Parent).Parent).Tag.ToString();
                            }
                            else
                            {
                                _deviceSerialNumber = ((TreeViewItem)((TreeViewItem)e.NewValue).Parent).Tag.ToString();
                            }
                        }



                        switch (tag)
                        {
                            case "root":
                                labelTitle.Visibility = Visibility.Collapsed;
                                if (treeDevices.Items.Count == 1)
                                {
                                    ScanUSBDevices();
                                }
                                return;
                            case "gpx_folder":
                                labelTitle.Content = "Latest GPX files in GPX folder";
                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceSerialNumber);
                                ShowGPXFolderLatest(_detectedDevice, 3);
                                break;
                            case "disk":
                                labelTitle.Visibility = Visibility.Collapsed;
                                labelDeviceName.Visibility = Visibility.Visible;
                                labelDeviceName.Content = ((TreeViewItem)((TreeViewItem)treeDevices.SelectedItem).Parent).Header;

                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceSerialNumber);
                                textBlock.Text = _detectedDevice.DriveSummary;
                                textBlock.Visibility = Visibility.Visible;
                                return;
                            //break;
                            case "trip_data":
                                labelTitle.Content = "Trip log";
                                ShowTripData();
                                break;
                            case "month_node":
                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceSerialNumber);
                                ShowGPXFolder(_detectedDevice, ((TreeViewItem)e.NewValue).Header.ToString());
                                break;
                            default:
                                _gpsTreeViewItem = (TreeViewItem)treeDevices.SelectedItem;
                                _inDeviceNode = true;
                                _deviceSerialNumber = tag;
                                menuGPXFolder.Visibility = Visibility.Visible;
                                _detectedDevice = Entities.DetectedDeviceViewModel.GetDevice(_deviceSerialNumber);
                                labelDeviceName.Visibility = Visibility.Visible;
                                if (_detectedDevice.GPS == null)
                                {
                                    labelDeviceName.Content = ((TreeViewItem)treeDevices.SelectedItem).Header;
                                }
                                else
                                {
                                    labelDeviceName.Content = $"{_detectedDevice.GPS.DeviceName} ({_detectedDevice.GPS.Brand} {_detectedDevice.GPS.Model})";
                                }


                                break;
                        }
                        if (Entities.GPSViewModel.Count > 0)
                        {
                            _gps = Entities.GPSViewModel.GetGPSEx(_deviceSerialNumber);
                            if (_gps != null && _inDeviceNode)
                            {
                                labelTitle.Content = "Details of GPS";
                                ShowGPS();
                            }
                            else if (_gps == null)
                            {
                                NewGPS();
                            }
                        }
                        else
                        {
                            NewGPS();
                        }
                    }
                    break;
            }
        }

        private void OnPropertyMouseDblClick(object sender, MouseButtonEventArgs e)
        {
            switch (_selectedProperty)
            {
                case "Brand":
                    SelectBrandModel(ShowMode.ShowModeBrand);
                    break;
                case "Model":
                    if (_cboBrand.SelectedItem != null)
                    {
                        SelectBrandModel(ShowMode.ShowModeModel, _cboBrand.SelectedItem.ToString());
                    }
                    else
                    {
                        MessageBox.Show("Select a GPS brand", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;
                case "Folder":
                    LocateGPXFolder();
                    break;
            }
        }

        private void OnPropertyChanged(object sender, RoutedPropertyChangedEventArgs<xceedPropertyGrid.PropertyItemBase> e)
        {
            if (e.NewValue != null)
            {
                _selectedProperty = ((xceedPropertyGrid.PropertyItem)e.NewValue).PropertyName;

                menuGPSBrands.Visibility = Visibility.Collapsed;
                menuGPSModels.Visibility = Visibility.Collapsed;

                if (_selectedProperty == "Brand")
                {
                    menuGPSBrands.Visibility = Visibility.Visible;
                }
                else if (_selectedProperty == "Model")
                {
                    menuGPSModels.Visibility = Visibility.Visible;
                }
            }
        }


        private void OnPropertyValueChanged(object sender, xceedPropertyGrid.PropertyValueChangedEventArgs e)
        {
            if (_detectedDevice != null && _detectedDevice.GPS != null)
            {
                //_gpsPropertyChanged = true;
                _changedPropertyName = ((xceedPropertyGrid.PropertyItem)e.OriginalSource).PropertyName;
                _gpsPropertyChanged = _changedPropertyName == "Folder" || _changedPropertyName == "DeviceName";
            }
        }


        public Control LayerSelector { get; set; }
        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayerSelector = (DataGrid)sender;
            if (MapWindowManager.MapWindowForm != null)
            {
                MapWindowManager.MapWindowForm.LayerSelector = LayerSelector;
            }
            switch (((DataGrid)sender).Name)
            {
                case "dataGridGPSSummary":
                    if (dataGridGPSSummary.Items.Count>0 && dataGridGPSSummary.SelectedItems.Count > 0)
                    {
                        _selectedTrip = (Trip)dataGridGPSSummary.SelectedItem;
                        _tripWaypoints = Entities.TripWaypointViewModel.GetAllTripWaypoints(_selectedTrip.TripID);
                        if (dataGridGPSSummary.SelectedItems.Count == 1 && MapWindowManager.MapWindowForm != null)
                        {
                            MapWindowManager.MapLayersHandler.ClearAllSelections();
                            TripMappingManager.RemoveTripLayersFromMap();
                            Entities.TripViewModel.MarkAllNotShownInMap();
                            ShowTripMap();

                        }
                        else if (_selectedTrip != null)
                        {
                            Entities.TripViewModel.GetTrip(_selectedTrip.TripID);
                            ShowTripWaypoints(fromGPSSummary: true);
                        }
                    }
                    break;
                case "dataGridTrips":
                    buttonEditTrip.IsEnabled = true;
                    buttonDeleteTrip.IsEnabled = true;
                    _selectedTrip = (Trip)dataGridTrips.SelectedItem;
                    if(dataGridTrips.SelectedItems.Count==1 && MapWindowManager.MapWindowForm!=null)
                    {
                        MapWindowManager.MapLayersHandler.ClearAllSelections();
                        TripMappingManager.RemoveTripLayersFromMap();
                    }
                    else if (_selectedTrip != null)
                    {
                        _selectedTrip.GPS = _gps;

                        Entities.TripViewModel.GetTrip(_selectedTrip.TripID);
                        ShowTripWaypoints();
                    }
                    break;
                case "dataGridTripWaypoints":
                    buttonEditWaypoint.IsEnabled = true;
                    buttonDeleteWaypoint.IsEnabled = true;
                    _selectedTripWaypoint = (TripWaypoint)dataGridTripWaypoints.SelectedItem;
                    break;
                case "dataGridGPXFiles":
                    if (dataGridGPXFiles.SelectedItem != null)
                    {
                        _gpxFile = (GPXFile)dataGridGPXFiles.SelectedItem;
                        _isTrackGPX = _gpxFile.TrackCount > 0;
                        buttonGPXDetails.IsEnabled = true;
                        SetGPXFileMenuMapVisibility(_gpxFile.ShownInMap, false);

                        MapWindowManager.MapLayersHandler?.ClearAllSelections();



                        if (_gpxFile.ShownInMap)
                        {
                            if (_isTrackGPX)
                            {
                                MapWindowManager.MapLayersHandler.set_MapLayer(MapWindowManager.GPXTracksLayer.Handle);
                            }
                            else
                            {
                                MapWindowManager.MapLayersHandler.set_MapLayer(MapWindowManager.GPXWaypointsLayer.Handle);
                            }

                            foreach (int handle in _gpxFile.ShapeIndexes)
                            {
                                ((Shapefile)MapWindowManager.MapLayersHandler.CurrentMapLayer.LayerObject).ShapeSelected[handle] = true;
                            }
                        }

                    }
                    break;
            }


        }

        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            switch (((DataGrid)sender).Name)
            {
                case "dataGridGPXFiles":
                    ShowGPXFileDetails();
                    break;
                case "dataGridCalendar":
                    if (dataGridCalendar.SelectedCells.Count == 1)
                    {

                        DataGridCellInfo cell = dataGridCalendar.SelectedCells[0];
                        var gridRow = dataGridCalendar.Items.IndexOf(cell.Item);
                        var gridCol = cell.Column.DisplayIndex;
                        var item = dataGridCalendar.Items[gridRow] as DataRowView;
                        var gps = Entities.GPSViewModel.GetGPSEx((string)item.Row.ItemArray[1]);
                        var trips = new List<Trip>();
                        if (((string)item.Row.ItemArray[gridCol]) == "x")
                        {
                            DateTime tripDate = new DateTime(_tripMonthYear.Year, _tripMonthYear.Month, gridCol - 1);
                            trips = Entities.TripViewModel.TripCollection
                                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                                .Where(t => t.DateTimeDeparture.Date == tripDate.Date).ToList();

                        }
                        else if (((string)item.Row.ItemArray[gridCol]).Length > 0)
                        {
                            trips = Entities.TripViewModel.TripCollection
                                 .Where(t => t.GPS.DeviceID == gps.DeviceID)
                                 .Where(t => t.DateTimeDeparture.Year == _tripMonthYear.Year)
                                 .Where(t => t.DateTimeDeparture.Month == _tripMonthYear.Month).ToList();
                        }

                        if (trips.Count == 1)
                        {
                            ShowEditTripWindow(isNew: false, tripID: trips[0].TripID, showWaypoints: true);
                        }
                        else if (trips.Count > 1)
                        {

                        }
                    }
                    break;
                case "dataGridTripWaypoints":
                    ShowEditTripWaypointWindow(false);
                    break;
                case "dataGridTrips":
                    ShowEditTripWindow(false, _selectedTrip.TripID);
                    break;
            }
        }

        private void OnDataGridMouseUp(object sender, MouseButtonEventArgs e)
        {
            //switch(((DataGrid)sender).Name)
            //{
            //    case "dataGridGPXFiles":
            //         if(!_isTrackGPX)
            //        {

            //        }
            //        break;
            //}
        }


        private void OnGridAutogeneratedColumns(object sender, EventArgs e)
        {
            if (dataGridCalendar.Columns.Count > 0)
            {
                dataGridCalendar.Columns[1].Visibility = Visibility.Collapsed;
            }
        }

        private void OnGPSGridDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!_isTrackGPX)
            {
                menuAddTripFromTRack.Visibility = Visibility.Collapsed;
            }
            else
            {
                menuAddTripFromTRack.Visibility = Visibility.Visible;
            }
        }

        private void ShowArchive()
        {
            if (Global.AppProceed)
            {
                _inArchive = true;
                ResetView();
                HideTrees();
                ConfigureGPXGrid(false);
                treeArchive.Visibility = Visibility.Visible;
                treeArchive.Items.Clear();
                labelTitle.Content = "Archived GPX files";
                var nd = treeArchive.Items.Add(new TreeViewItem { Header = "Archive", Tag = "root" });
                var root = ((TreeViewItem)treeArchive.Items[nd]);
                foreach (var gps in Entities.DeviceGPXViewModel.GetAllGPS())
                {
                    nd = root.Items.Add(new TreeViewItem { Header = gps.DeviceName, Tag = gps });
                    var gpsNode = root.Items[nd] as TreeViewItem;
                    foreach (var month in Entities.DeviceGPXViewModel.GetMonthsInArchive(gps))
                    {
                        gpsNode.Items.Add(new TreeViewItem { Header = month.ToString("MMM, yyyy"), Tag = new DateTime(month.Year, month.Month, 1) });
                    }
                    gpsNode.IsExpanded = true;
                }
                root.IsExpanded = true;
                if(root.Items.Count==0)
                {
                    //MessageBox.Show("There are no archived GPX files in the database","GPX Manager",MessageBoxButton.OK,MessageBoxImage.Information);

                    labelNoData.Visibility = Visibility.Visible;
                    labelNoData.Content = "There are no archived GPX files in the database";
                    labelTitle.Visibility = Visibility.Hidden;
                    treeArchive.Visibility = Visibility.Collapsed;

                }
            }
            else
            {
                MessageBox.Show("Application need to be setup first", "GPX Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void ToBeImplemented(string usage)
        {
            System.Windows.MessageBox.Show($"The {usage} functionality is not yet implemented", "Placeholder and not yet working", MessageBoxButton.OK, MessageBoxImage.Information); ;
        }
        private void OnToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "buttonArchive":
                    ShowArchive();
                    break;
                case "buttonUploadCloud":
                    ToBeImplemented("upload to cloud");
                    break;
                case "buttonCalendar":
                    ShowTripCalendar();
                    break;
                case "buttonSettings":
                    ShowSettingsWindow();
                    break;
                case "buttonExit":
                    Close();
                    break;
                case "buttonUSB":
                    HideTrees();
                    treeDevices.Visibility = Visibility.Visible;
                    ScanUSBDevices();
                    break;
                case "buttonMap":
                    ShowMap();
                    break;
            }
        }

        private void OnStatusLabelDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start($"{System.IO.Path.GetDirectoryName(((System.Windows.Controls.Label)sender).Content.ToString())}");
        }
    }
}
