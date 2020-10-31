using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management;
using System.IO;

namespace GPXManager.entities
{
    public class DetectedDeviceViewModel
    {
        public ObservableCollection<DetectedDevice> DetectedDeviceCollection { get; set; }
        public DetectedDeviceViewModel()
        {
            DetectedDeviceCollection = new ObservableCollection<DetectedDevice>();
            DetectedDeviceCollection.CollectionChanged += DetectedDeviceCollection_CollectionChanged;
        }
        public List<DetectedDevice> GetAllDevices()
        {
            return DetectedDeviceCollection.ToList();
        }

        public DetectedDevice GetDevice(string serialNumber)
        {
            CurrentEntity = DetectedDeviceCollection.FirstOrDefault(n => n.SerialNumber == serialNumber);
            return CurrentEntity;
        }

        public int ScanUSBDevices()
        {
            int deviceCount = 0;
            var drives = new ManagementObjectSearcher("select * from Win32_DiskDrive where InterfaceType='USB'").Get();
            if (drives.Count > 0)
            {

                foreach (ManagementObject drive in drives)
                {
                    var device = new DetectedDevice
                    {
                        SerialNumber = drive.Properties["SerialNumber"].Value.ToString(),
                        Caption = drive.Properties["Caption"].Value.ToString(),
                        Index = int.Parse(drive.Properties["Index"].Value.ToString()),
                        InterfaceType = drive.Properties["InterfaceType"].Value.ToString(),
                        Model = drive.Properties["Model"].Value.ToString(),
                        Partitions = int.Parse(drive.Properties["Partitions"].Value.ToString()),
                        PNPDeviceID = drive.Properties["PNPDeviceID"].Value.ToString(),
                        Signature = drive.Properties["Signature"].Value.ToString(),
                        Size = long.Parse(drive.Properties["Size"].Value.ToString()),
                        Status = drive.Properties["Status"].Value.ToString(),
                        BytesPerSector = int.Parse(drive.Properties["BytesPerSector"].Value.ToString()),
                        CreationClassName = drive.Properties["CreationClassName"].Value.ToString(),
                        Description = drive.Properties["Description"].Value.ToString(),
                        DeviceID = drive.Properties["DeviceID"].Value.ToString(),
                        MediaLoaded = bool.Parse(drive.Properties["MediaLoaded"].Value.ToString()),
                        Name = drive.Properties["Name"].Value.ToString(),
                        SectorsPerTrack = int.Parse(drive.Properties["SectorsPerTrack"].Value.ToString()),
                        TotalHeads = int.Parse(drive.Properties["TotalHeads"].Value.ToString()),
                        TotalCylinders = int.Parse(drive.Properties["TotalCylinders"].Value.ToString()),
                        TotalSectors = int.Parse(drive.Properties["TotalSectors"].Value.ToString()),
                        TracksPerCylinder = int.Parse(drive.Properties["TracksPerCylinder"].Value.ToString())
                    };

                    var mo = new ManagementObject("Win32_PhysicalMedia.Tag='" + drive["DeviceID"] + "'");

                    foreach (ManagementObject partition in new ManagementObjectSearcher
                        ("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive["DeviceID"] +
                        "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
                    {
                        foreach (ManagementObject disk in new ManagementObjectSearcher
                            ("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partition["DeviceID"] +
                            "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())

                        {
                            Disk dsk = null;
                            try
                            {
                                dsk = new Disk
                                {
                                    Caption = disk.Properties["Caption"].Value.ToString(),
                                    Compressed = bool.Parse(disk.Properties["Compressed"].Value.ToString()),
                                    Description = disk.Properties["Description"].Value.ToString(),
                                    //FreeSpace = long.Parse(drive.Properties["FreeSpace"].Value.ToString()),
                                    Size = long.Parse(drive.Properties["Size"].Value.ToString()),
                                    DeviceID = disk.Properties["DeviceID"].Value.ToString(),
                                    FileSystem = disk.Properties["FileSystem"].Value.ToString(),
                                    VolumeSerialNumber = disk.Properties["VolumeSerialNumber"].Value.ToString(),
                                    VolumeName = disk.Properties["VolumeName"].Value.ToString()
                                };
                            }
                            catch(Exception ex)
                            {
                                Logger.Log(ex);
                                dsk = null;
                            }
                            if (device.Disks == null)
                            {
                                device.Disks = new List<Disk>();
                            }
                            device.Disks.Add(dsk);

                        }
                    }
                    AddDevice(device);
                    deviceCount++;
                }
                return deviceCount;
            }
            else
            {
                return 0;
            }
        }
        public DetectedDevice CurrentEntity { get; set; }
        private void DetectedDeviceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        DetectedDevice newDevice = DetectedDeviceCollection[newIndex];
                        CurrentEntity = newDevice;
                        
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        //List<GPS> tempListOfRemovedItems = e.OldItems.OfType<GPS>().ToList();
                        //GPSes.Delete(tempListOfRemovedItems[0].Code);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        //List<GPS> tempList = e.NewItems.OfType<GPS>().ToList();
                        //GPSes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public int Count
        {
            get { return DetectedDeviceCollection.Count; }
        }

        public void AddDevice(DetectedDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("Error: The argument is Null");
            DetectedDeviceCollection.Add(device);

            var gps = Entities.GPSViewModel.GetGPSEx(device.SerialNumber);
            if(gps!=null)
            {
                device.GPS = gps;
            }
        }

        public void UpdateDeviceInCollection(DetectedDevice device)
        {
            if (device.SerialNumber == null)
                throw new Exception("Error: Serial number cannot be null");

            int index = 0;
            while (index < DetectedDeviceCollection.Count)
            {
                if (DetectedDeviceCollection[index].SerialNumber == device.SerialNumber)
                {
                    DetectedDeviceCollection[index] = device;
                    break;
                }
                index++;
            }
        }

        public void DeleteDeviceFromCollection(string serialNumber)
        {
            if (serialNumber == null)
                throw new Exception("Serial number cannot be null");

            int index = 0;
            while (index < DetectedDeviceCollection.Count)
            {
                if (DetectedDeviceCollection[index].SerialNumber == serialNumber)
                {
                    DetectedDeviceCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
