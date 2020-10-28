using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Dynamic;
using System.Windows.Media.Media3D;
using System.IO;

namespace GPXManager.entities
{
    public class GPSViewModel
    {
        public ObservableCollection<GPS> GPSCollection { get; set; }
        private GPSRepository GPSes { get; set; }


        public void Serialize(string fileName)
        {
            SerializeGPS serializeGPS = new SerializeGPS { GPSList = GPSCollection.ToList() };
            serializeGPS.Save(fileName);
        }
        public GPSViewModel()
        {
            GPSes = new GPSRepository();
            GPSCollection = new ObservableCollection<GPS>(GPSes.GPSes);
            GPSCollection.CollectionChanged += GPSCollection_CollectionChanged;

            if (GPSCollection.Count > 0)
            {
                GPSBrands = GetBrands();
            }
        }

        public bool ClearRepository()
        {
            return GPSes.ClearTable();
        }
        public void AddBrand(string brand)
        {
            if(GPSBrands==null)
            {
                GPSBrands = new List<string>();
                GPSModels = new List<string>();
            }
            GPSBrands.Add(brand);
        }

        public List<string> GPSModels { get; set; }
        public List<GPS> GetGPS()
        {
            return GPSCollection.ToList();
        }
        public bool GPSDeviceNameExist(string name)
        {
            foreach (GPS gps in GPSCollection)
            {
                if (gps.DeviceName == name)
                {
                    return true;
                }
            }
            return false;
        }
        public List<string> GPSBrands { get; set; }
        public GPS CurrentEntity { get; set; }
        public bool GPSCodeExist(string code)
        {
            foreach (GPS gps in GPSCollection)
            {
                if (gps.Code == code)
                {
                    return true;
                }
            }
            return false;
        }

        public List<string> GetModels(string brand)
        {
            var list = GPSCollection
                .Where(t => t.Brand == brand).ToList();

            return (from gps in list select gps.Model).Distinct().ToList();

        }
        public List<string> GetBrands()
        {
            return (from gps in GPSCollection select gps.Brand).Distinct().ToList();
        }
        public void MakeNewLists()
        {
            GPSBrands = new List<string>();
            GPSModels = new List<string>();
        }
        public GPS GetGPSEx (string deviceID)
        {
            CurrentEntity = GPSCollection.FirstOrDefault(n => n.DeviceID == deviceID);
            if (CurrentEntity != null)
            {
                GPSModels = GetModels(CurrentEntity.Brand);
            }
            else
            {
                if (GPSModels != null)
                {
                    GPSModels.Clear();
                }
            }
            return CurrentEntity;
        }
        public GPS GetGPS(string code)
        {
            CurrentEntity = GPSCollection.FirstOrDefault(n => n.Code == code);
            return CurrentEntity;

        }
        private void GPSCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        GPS newGPS = GPSCollection[newIndex];
                        if (GPSes.Add(newGPS))
                        {
                            CurrentEntity = newGPS;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<GPS> tempListOfRemovedItems = e.OldItems.OfType<GPS>().ToList();
                        GPSes.Delete(tempListOfRemovedItems[0].Code);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<GPS> tempList = e.NewItems.OfType<GPS>().ToList();
                        GPSes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }

        public int Count
        {
            get { return GPSCollection.Count; }
        }

        public void AddRecordToRepo(GPS gps)
        {
            if (gps == null)
                throw new ArgumentNullException("Error: The argument is Null");
            GPSCollection.Add(gps);
        }

        public void UpdateRecordInRepo(GPS gps)
        {
            if (gps.Code == null)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < GPSCollection.Count)
            {
                if (GPSCollection[index].Code == gps.Code)
                {
                    GPSCollection[index] = gps;
                    break;
                }
                index++;
            }
        }

        public void DeleteRecordFromRepo(string code)
        {
            if (code == null)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < GPSCollection.Count)
            {
                if (GPSCollection[index].Code == code)
                {
                    GPSCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
        public EntityValidationResult ValidateGPS(GPS gps, bool isNew, string oldAssignedName, string oldCode)
        {
            EntityValidationResult evr = new EntityValidationResult();

            if (isNew && (gps.DeviceName == null || gps.DeviceName.Length < 5))
            {
                evr.AddMessage("Device name must be at least 5 letters long");
            }

            if (isNew && (gps.Code==null || gps.Code.Length >6))
            {
                evr.AddMessage("Device code must not exceed 6 letters long");
            }

            if (gps.Brand == null || gps.Brand.Length == 0)
            {
                evr.AddMessage("Brand must not be empty");
            }

            if (gps.Model==null || gps.Model.Length == 0)
            {
                evr.AddMessage("Model must not be empty");
            }

            if (gps.Folder == null || gps.Folder.Length == 0)
            {
                evr.AddMessage("Folder must not be empty");
            }

            if (!Directory.Exists($"{gps.Device.Disks[0].Caption}\\{gps.Folder}"))
            {
                evr.AddMessage("GPX folder does not exist");
            }



            if (!isNew && gps.DeviceName.Length > 0
                && oldAssignedName != gps.DeviceName
                && GPSDeviceNameExist(gps.DeviceName))
                evr.AddMessage("Device name already used");

            if (!isNew && gps.Code.Length > 0
                && oldCode != gps.Code
                && GPSCodeExist(gps.Code))
                evr.AddMessage("Device code already used");

            if(isNew && gps.Code.Length>0 && GPSCodeExist(gps.Code))
            {
                evr.AddMessage("Device code already used");   
            }

            if (isNew && gps.DeviceName.Length > 0 && GPSDeviceNameExist(gps.DeviceName))
            {
                evr.AddMessage("Device name already used");
            }

            return evr;
        }
    }
}
