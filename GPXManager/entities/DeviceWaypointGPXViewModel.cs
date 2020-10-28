using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;

namespace GPXManager.entities
{
    public class DeviceWaypointGPXViewModel
    {
        public ObservableCollection<DeviceWaypointGPX> DeviceWptGPXCollection { get; set; }
        private DeviceWaypointGPXRepository DeviceWaypointGPXes{ get; set; }

        public DeviceWaypointGPXViewModel()
        {
            DeviceWaypointGPXes = new DeviceWaypointGPXRepository();
            DeviceWptGPXCollection = new ObservableCollection<DeviceWaypointGPX>(DeviceWaypointGPXes.DeviceWaypointGPXes);
            DeviceWptGPXCollection.CollectionChanged += DeviceWptGPXCollection_CollectionChanged;
        }

        public DeviceWaypointGPX GetDeviceWaypointGPX(int id)
        {
            return DeviceWptGPXCollection.Where(t => t.RowID == id).FirstOrDefault();
        }

        public DeviceWaypointGPX GetDeviceWaypointGPX(GPS gps, string fileName)
        {
            return DeviceWptGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t=>t.Filename==fileName)
                .FirstOrDefault();
        }

        public List<DeviceWaypointGPX> GetAllDeviceWaypointGPX()
        {
            return DeviceWptGPXCollection.ToList();
        }


        public List<DeviceWaypointGPX> GetAllDeviceWaypointGPX(GPS gps)
        {
            return DeviceWptGPXCollection.Where(t=>t.GPS.DeviceID==gps.DeviceID).ToList();
        }

        public DeviceWaypointGPX CurrentEntity { get; set; }
        private void DeviceWptGPXCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        DeviceWaypointGPX newWPTGPX = DeviceWptGPXCollection[newIndex];
                        if (DeviceWaypointGPXes.Add(newWPTGPX))
                        {
                            CurrentEntity = newWPTGPX;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<DeviceWaypointGPX> tempListOfRemovedItems = e.OldItems.OfType<DeviceWaypointGPX>().ToList();
                        DeviceWaypointGPXes.Delete(tempListOfRemovedItems[0].RowID);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<DeviceWaypointGPX> tempList = e.NewItems.OfType<DeviceWaypointGPX>().ToList();
                        DeviceWaypointGPXes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
                    }
                    break;
            }
        }
        public bool ClearRepository()
        {
            return DeviceWaypointGPXes.ClearTable();
        }

        public int Count
        {
            get { return DeviceWptGPXCollection.Count; }
        }

        public void AddRecordToRepo(DeviceWaypointGPX gpx)
        {
            if (gpx == null)
                throw new ArgumentNullException("Error: The argument is Null");
            DeviceWptGPXCollection.Add(gpx);
        }

        public void UpdateRecordInRepo(DeviceWaypointGPX gpx)
        {
            if (gpx.RowID == 0)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < DeviceWptGPXCollection.Count)
            {
                if (DeviceWptGPXCollection[index].RowID == gpx.RowID)
                {
                    DeviceWptGPXCollection[index] = gpx;
                    break;
                }
                index++;
            }
        }
        public void SaveDeviceGPXToRepository()
        {
            if (Entities.WaypointViewModel.Waypoints.Count > 0)
            {
                foreach (var gps_waypointset in Entities.WaypointViewModel.Waypoints)
                {
                    foreach (var item in gps_waypointset.Value)
                    {
                        if (item.Waypoints.Count > 0)
                        {
                            if (File.Exists(item.FullFileName))
                            {
                                var file = Path.GetFileName(item.FullFileName);

                                string content;
                                using (StreamReader sr = File.OpenText(item.FullFileName))
                                {
                                    content = sr.ReadToEnd();
                                }
                                var dwg = GetDeviceWaypointGPX(item.GPS, file);
                                if (dwg == null)
                                {
                                    AddRecordToRepo(
                                        new DeviceWaypointGPX
                                        {
                                            GPS = item.GPS,
                                            Filename = file,
                                            GPX = content,
                                            RowID = NextRecordNumber,
                                            MD5 = CreateMD5(content)
                                        }
                                    );
                                }
                                else
                                {
                                    var deviceMD5 = CreateMD5(content);
                                    if (CreateMD5(dwg.GPX) != deviceMD5)
                                    {
                                        UpdateRecordInRepo(new DeviceWaypointGPX
                                        {
                                            GPS = dwg.GPS,
                                            GPX = content,
                                            Filename = dwg.Filename,
                                            RowID = dwg.RowID,
                                            MD5 = deviceMD5
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public int NextRecordNumber
        {
            get
            {
                if (DeviceWptGPXCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return DeviceWaypointGPXes.MaxRecordNumber() + 1;
                }
            }
        }
        public void DeleteRecordFromRepo(int rowID)
        {
            if (rowID == 0)
                throw new Exception("Record ID cannot be null");

            int index = 0;
            while (index < DeviceWptGPXCollection.Count)
            {
                if (DeviceWptGPXCollection[index].RowID == rowID)
                {
                    DeviceWptGPXCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
