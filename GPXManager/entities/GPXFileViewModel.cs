using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using MapWinGIS;

namespace GPXManager.entities
{
    public class GPXFileViewModel
    {
        public ObservableCollection<GPXFile> GPXFileCollection { get; set; }
        public GPXFileViewModel()
        {
            GPXFileCollection = new ObservableCollection<GPXFile>();
            GPXFileCollection.CollectionChanged += GPXFileCollection_CollectionChanged;
        }



        public List<GPXFile>GetFiles(string deviceID)
        {
            if(GPXFileCollection.Where(t=>t.GPS.DeviceID==deviceID).ToList().Count==0)
            {
                GetFilesFromDevice(Entities.DetectedDeviceViewModel.GetDevice(deviceID));
            }
            return GPXFileCollection.Where(t => t.GPS.DeviceID == deviceID).ToList();
        }

        public void Clear()
        {
            GPXFileCollection = new ObservableCollection<GPXFile>();
        }
        public bool Contains(GPXFile file)
        {
            return GPXFileCollection
               .Where(t => t.GPS.DeviceID == file.GPS.DeviceID)
               .Where(t => t.FileName == file.FileName).FirstOrDefault() != null;
        }
        public GPXFile CurrentEntity { get; set; }
        private void GPXFileCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        GPXFile newFile = GPXFileCollection[newIndex];
                        CurrentEntity = newFile;

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
            get { return GPXFileCollection.Count; }
        }

        public void GetFilesFromDevice(DetectedDevice device)
        {
            var gpxFolder = $"{device.Disks[0].Caption }\\{ device.GPS.Folder}";
            if (Directory.Exists(gpxFolder))
            {
                List<FileInfo> myFiles = new DirectoryInfo(gpxFolder)
                    .EnumerateFiles()
                    .Where(f => f.Extension == ".gpx").ToList();


                foreach (var file in myFiles)
                {
                    Entities.WaypointViewModel.ReadWaypointsFromFile(file.FullName, device.GPS);

                    GPXFile gf = new GPXFile(file)
                    {
                        GPS = device.GPS,
                        DriveName = device.Disks[0].Caption
                    };


                    if (!Contains(gf))
                    {
                        Add(gf);
                        gf.ComputeStats();
                    }
                }
            }
        }
        public void Add(GPXFile file)
        {
            if (file == null)
                throw new ArgumentNullException("Error: The argument is Null");
            GPXFileCollection.Add(file);
        }

        public void Update(GPXFile file)
        {
            if (file.FileName == null)
                throw new Exception("Error: File name cannot be null");

            int index = 0;
            while (index < GPXFileCollection.Count)
            {
                if (GPXFileCollection[index].FileName == file.FileName)
                {
                    GPXFileCollection[index] = file;
                    break;
                }
                index++;
            }
        }

        public void Delete(string fileName)
        {
            if (fileName == null)
                throw new Exception("File name cannot be null");

            int index = 0;
            while (index < GPXFileCollection.Count)
            {
                if (GPXFileCollection[index].FileName == fileName)
                {
                    GPXFileCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
