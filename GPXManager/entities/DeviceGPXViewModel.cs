﻿using System;
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
    public class DeviceGPXViewModel
    {

        private bool _success;
        private int _count;
        public ObservableCollection<DeviceGPX> DeviceGPXCollection { get; set; }
        private DeviceGPXRepository DeviceWaypointGPXes{ get; set; }


        public Dictionary<GPS, List<GPXFile>> ArchivedGPXFiles { get; private set; } = new Dictionary<GPS, List<GPXFile>>();
        public DeviceGPXViewModel()
        {
            DeviceWaypointGPXes = new DeviceGPXRepository();
            DeviceGPXCollection = new ObservableCollection<DeviceGPX>(DeviceWaypointGPXes.DeviceGPXes);
            DeviceGPXCollection.CollectionChanged += DeviceWptGPXCollection_CollectionChanged;
            ConvertDeviceGPXInArchiveToGPXFile();
            GPXBackupFolder = "GPXBackup";
        }
        
        /// <summary>
        /// checks if the backup location exists. It not, it will create one.
        /// </summary>
        /// <param name="folderForBackup"></param>
        public void CheckGPXBackupFolder()
        {
            string folderForBackup = Global.Settings.ComputerGPXFolder;
            if (!Directory.Exists($@"{folderForBackup}\{GPXBackupFolder}"))
            {
                Directory.CreateDirectory($@"{folderForBackup}\{GPXBackupFolder}");
            }
        }

        public string GPXBackupFolder { get; private set; }
        public int BackupGPXToDrive()
        {
            string folderForBackup = Global.Settings.ComputerGPXFolder;
            int count = 0;
            DirectoryInfo backupDir;
            DirectoryInfo gpsBackupDir;
            DirectoryInfo gpsBackupDirMonth;
            if(!Directory.Exists($@"{folderForBackup}\{GPXBackupFolder}"))
            {
                backupDir =  Directory.CreateDirectory($@"{folderForBackup}\{GPXBackupFolder}");
            }
            else
            {
                backupDir = new DirectoryInfo(($@"{folderForBackup}\{GPXBackupFolder}"));
            }
            foreach(var gps in Entities.GPSViewModel.GPSCollection.OrderBy(t=>t.DeviceName))
            {

                string gpsDirectory = $@"{backupDir.FullName}\{gps.DeviceName}";
                if (Directory.Exists(gpsDirectory))
                {
                    gpsBackupDir = new DirectoryInfo(gpsDirectory);
                }
                else
                {
                    gpsBackupDir = Directory.CreateDirectory(gpsDirectory);
                }

                if (Entities.DeviceGPXViewModel.ArchivedGPXFiles.Keys.Contains(gps))
                {
                    foreach (var gpxFile in Entities.DeviceGPXViewModel.ArchivedGPXFiles[gps])
                    {
                        var fileTimeStart = gpxFile.DateRangeStart;
                        var monthYear = new DateTime(fileTimeStart.Year, fileTimeStart.Month, 1).ToString("MMM-yyyy");
                        if(Directory.Exists($@"{gpsBackupDir.FullName}\{monthYear}"))
                        {
                            gpsBackupDirMonth = new DirectoryInfo($@"{gpsBackupDir.FullName}\{monthYear}");
                        }
                        else
                        {
                            gpsBackupDirMonth = Directory.CreateDirectory($@"{gpsBackupDir.FullName}\{monthYear}");
                        }

                        string fileToBackup = $@"{gpsBackupDirMonth.FullName}\{gpxFile.FileName}";
                        if (File.Exists(fileToBackup))
                        {
                            if(CreateMD5(gpxFile.XML) != CreateMD5( File.OpenText(fileToBackup).ReadToEnd()))
                            {
                                var pattern = (Path.GetFileNameWithoutExtension(fileToBackup) + "*.gpx");
                                var files = Directory.GetFiles(gpsBackupDirMonth.FullName,pattern );
                                string versionedFile = $@"{Path.ChangeExtension(fileToBackup, null)}_{((int)(DateTime.Now.ToOADate()*1000)).ToString()}.gpx";
                                using (StreamWriter sw = File.CreateText(versionedFile))
                                {
                                    sw.Write(gpxFile.XML);
                                }
                            }

                        }
                        else
                        {
                            using (StreamWriter sw = File.CreateText(fileToBackup))
                            {
                                sw.Write(gpxFile.XML);
                                count++;
                            }
                        }
                    }
                }
            }
            return count;
        }

        public int ImportGPX(string folder, GPS in_gps = null, bool first=false)
        {
            if(first)
            {
                _count = 0;
            }
            GPS gps = null;
            GPS current_gps = null; 
            var files = Directory.GetFiles(folder).Select(s => new FileInfo(s));
            if (files.Any())
            {
                foreach(var file in files)
                {
                    if(file.Extension.ToLower()==".gpx")
                    {
                        var folderName = Path.GetFileName(folder);

                        gps = Entities.GPSViewModel.GetGPSByName(folderName);

                        if (gps!=null)
                        {
                            current_gps = gps;    
                        }
                        else if(gps==null && in_gps!=null)
                        {
                            current_gps = in_gps;
                        } 

                        if (current_gps != null)
                        {
                            GPXFile g = new GPXFile(file);
                            g.GPS = current_gps;
                            g.ComputeStats();
                            DeviceGPX d = new DeviceGPX
                            {
                                Filename = file.Name,
                                GPS = current_gps,
                                GPX = g.XML,
                                GPXType = g.GPXFileType == GPXFileType.Track ? "track" : "waypoint",
                                RowID = NextRecordNumber,
                                MD5 = CreateMD5(g.XML),
                                TimeRangeStart = g.DateRangeStart,
                                TimeRangeEnd = g.DateRangeEnd
                            };

                            string fileProcessed = $@"{current_gps.DeviceName}:{file.FullName}";

                            DeviceGPX saved = GetDeviceGPX(d);
                            if(saved==null)
                            {
                                if(AddRecordToRepo(d))
                                {
                                    _count++;
                                    fileProcessed += "  (ADDED)";
                                }
                            }
                            else
                            {
                                if(saved.MD5!=d.MD5 && d.TimeRangeEnd > saved.TimeRangeEnd)
                                {
                                    UpdateRecordInRepo(d);
                                    fileProcessed += " (MODIFIED ADDED)";
                                }
                                else
                                {
                                    fileProcessed += "  (DUPLICATE)";
                                }
                            }
                            Console.WriteLine(fileProcessed);
                        }
                    }
                }
            }

            foreach(var dir in Directory.GetDirectories(folder))
            {
                ImportGPX(dir, current_gps);
            }
            return _count;
        }
        
        public void RefreshArchivedGPXCollection(GPS gps)
        {
            ConvertDeviceGPXInArchiveToGPXFile(gps); ;
        }

        public void MarkAllNotShownInMap()
        {
            foreach(GPS gps in ArchivedGPXFiles.Keys)
            {
                foreach(GPXFile file in ArchivedGPXFiles[gps])
                {
                    file.ShownInMap = false;
                }
            }
        }
        private void ConvertDeviceGPXInArchiveToGPXFile(GPS gps = null)
        {
            if (gps == null)
            {
                foreach (var item in DeviceGPXCollection)
                {
                    var gpxFile = Entities.GPXFileViewModel.ConvertToGPXFile(item);
                    AddToDictionary(gpxFile.GPS, gpxFile);
                }
            }
            else
            {
                List<GPXFile> gpxFiles = new List<GPXFile>();
                if (ArchivedGPXFiles.Keys.Contains(gps)&& ArchivedGPXFiles[gps].Count > 0)
                {
                   gpxFiles = ArchivedGPXFiles[gps];
                }
                foreach (var item in DeviceGPXCollection.Where(t=>t.GPS.DeviceID==gps.DeviceID))
                {
                    var gpxFile = Entities.GPXFileViewModel.ConvertToGPXFile(item);
                    if (!gpxFiles.Contains(gpxFile))
                    {
                        AddToDictionary(gpxFile.GPS, gpxFile);
                    }
                }
            }
        }

        private void AddToDictionary(GPS gps, GPXFile gpxFile)
        {
            if (!ArchivedGPXFiles.Keys.Contains(gps))
            {
                ArchivedGPXFiles.Add(gps, new List<GPXFile>());
            }
            ArchivedGPXFiles[gps].Add(gpxFile);
        }
        
        public List<WaypointLocalTime>GetWaypoints(GPXFile wayppointFile)
        {
            if(wayppointFile.TrackCount==0 && wayppointFile.NamedWaypointsInLocalTime.Count>0)
            {
                return wayppointFile.NamedWaypointsInLocalTime;
            }
            return null;
        }
        public List<WaypointLocalTime>GetWaypointsMatch(GPXFile trackFile, out List<GPXFile> gpxFiles)
        {
            gpxFiles = new List<GPXFile>();
            var thisList = new List<WaypointLocalTime>();
            foreach(var g in  ArchivedGPXFiles[trackFile.GPS].Where(t=>t.GPXFileType==GPXFileType.Waypoint))
            {
                foreach(var wpt in g.NamedWaypointsInLocalTime)
                {
                    if(wpt.Time >= trackFile.DateRangeStart && wpt.Time <= trackFile.DateRangeEnd)
                    {
                        thisList.Add(wpt);
                        if(!gpxFiles.Contains(g))
                        {
                            gpxFiles.Add(g);
                        }
                    }
                }
            }

            return thisList;
        }

        public DeviceGPX GetDeviceGPX(int id)
        {
            return DeviceGPXCollection.Where(t => t.RowID == id).FirstOrDefault();
        }

        public DeviceGPX GetDeviceGPX (DeviceGPX deviceGPX)
        {
            return DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == deviceGPX.GPS.DeviceID)
                .Where(t => t.Filename == deviceGPX.Filename)
                .FirstOrDefault();
        }
        
        public DeviceGPX GetDeviceGPX(GPS gps, string fileName)
        {
            var g =  DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .Where(t=>t.Filename==fileName)
                .FirstOrDefault();

            return g;
        }

        public DeviceGPX GetDeviceGPX(GPS gps)
        {
            return DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .FirstOrDefault();
        }
        public List<GPS> GetAllGPS()
        {
            return DeviceGPXCollection.GroupBy(t => t.GPS).Select(g => g.First().GPS).OrderBy(t=>t.DeviceName).ToList();
        }
        public DeviceGPX GetDeviceGPX(GPS gps, DateTime month_year)
        {
            return DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .FirstOrDefault();
        }

        public List<DateTime> GetMonthsInArchive(GPS gps)
        {
            return DeviceGPXCollection
                .Where(t => t.GPS.DeviceID == gps.DeviceID)
                .OrderBy(t=>t.TimeRangeStart)
                .GroupBy(t => t.TimeRangeStart.ToString("MMM-yyyy"))
                .Select(g => g.First().TimeRangeStart)
                .ToList();
        }

        public List<DeviceGPX> GetAllDeviceWaypointGPX()
        {
            return DeviceGPXCollection.ToList();
        }


        public List<DeviceGPX> GetAllDeviceWaypointGPX(GPS gps)
        {
            return DeviceGPXCollection.Where(t=>t.GPS.DeviceID==gps.DeviceID).ToList();
        }

        public DeviceGPX CurrentEntity { get; set; }
        private void DeviceWptGPXCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _success = false;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int newIndex = e.NewStartingIndex;
                        DeviceGPX newWPTGPX = DeviceGPXCollection[newIndex];
                        if (DeviceWaypointGPXes.Add(newWPTGPX))
                        {
                            CurrentEntity = newWPTGPX;
                            _success = true;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        List<DeviceGPX> tempListOfRemovedItems = e.OldItems.OfType<DeviceGPX>().ToList();
                        DeviceWaypointGPXes.Delete(tempListOfRemovedItems[0].RowID);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        List<DeviceGPX> tempList = e.NewItems.OfType<DeviceGPX>().ToList();
                        _success =  DeviceWaypointGPXes.Update(tempList[0]);      // As the IDs are unique, only one row will be effected hence first index only
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
            get { return DeviceGPXCollection.Count; }
        }

        public bool AddRecordToRepo(DeviceGPX gpx)
        {
            int oldCount = DeviceGPXCollection.Count;
            if (gpx == null)
                throw new ArgumentNullException("Error: The argument is Null");
            DeviceGPXCollection.Add(gpx);
            return DeviceGPXCollection.Count > oldCount;
        }

        public bool UpdateRecordInRepo(DeviceGPX gpx)
        {
            if (gpx.RowID == 0)
                throw new Exception("Error: ID cannot be null");

            int index = 0;
            while (index < DeviceGPXCollection.Count)
            {
                if (DeviceGPXCollection[index].RowID == gpx.RowID)
                {
                    DeviceGPXCollection[index] = gpx;
                    break;
                }
                index++;
            }
            return _success;
        }

        /// <summary>
        /// Saves gpx files in device to database
        /// </summary>
        /// <param name="device"></param>
        public bool SaveDeviceGPXToRepository(DetectedDevice device)
        {
            bool successSave = false;
            foreach(var file in Entities.GPXFileViewModel.GetGPXFilesFromGPS(device))
            {
                string content;
                using (StreamReader sr = File.OpenText(file.FullName))
                {
                    content = sr.ReadToEnd();
                    var gpxFileName = Path.GetFileName(file.FullName);
                    var dwg = GetDeviceGPX(device.GPS, gpxFileName);
                    GPXFile gpxFile = Entities.GPXFileViewModel.GetFile(device.GPS, gpxFileName);
                    var gpxType = gpxFile.GPXFileType == GPXFileType.Track ? "track" : "waypoint";
                    if (dwg == null)
                    {
                        successSave= AddRecordToRepo(
                            new DeviceGPX
                            {
                                GPS = device.GPS,
                                Filename = gpxFileName,
                                GPX = content,
                                RowID = NextRecordNumber,
                                MD5 = CreateMD5(content),
                                GPXType = gpxType,
                                TimeRangeStart = gpxFile.DateRangeStart,
                                TimeRangeEnd = gpxFile.DateRangeEnd
                            }
                        ) ;
                    }
                    else
                    {
                        var deviceMD5 = CreateMD5(content);
                        if (CreateMD5(dwg.GPX) != deviceMD5)
                        {
                            successSave= UpdateRecordInRepo(new DeviceGPX
                            {
                                GPS = dwg.GPS,
                                GPX = content,
                                Filename = dwg.Filename,
                                RowID = dwg.RowID,
                                MD5 = deviceMD5,
                                GPXType = gpxType,
                                TimeRangeStart = gpxFile.DateRangeStart,
                                TimeRangeEnd = gpxFile.DateRangeEnd
                            });
                        }
                    }
                }
            }
            return successSave;
        }
        public void SaveDeviceGPXToRepository()
        {
            if (Entities.WaypointViewModel.Waypoints.Count > 0)
            {
                foreach (var gps_waypointset in Entities.WaypointViewModel.Waypoints)
                {
                    if (Entities.GPSViewModel.Exists(gps_waypointset.Key))
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
                                    var dwg = GetDeviceGPX(item.GPS, file);
                                    GPXFile gpxFile = Entities.GPXFileViewModel.GetFile(item.GPS, item.FileName);
                                    if (dwg == null)
                                    {
                                        AddRecordToRepo(
                                            new DeviceGPX
                                            {
                                                GPS = item.GPS,
                                                Filename = file,
                                                GPX = content,
                                                RowID = NextRecordNumber,
                                                MD5 = CreateMD5(content),
                                                GPXType = "waypoint",
                                                TimeRangeStart = gpxFile.DateRangeStart,
                                                TimeRangeEnd = gpxFile.DateRangeEnd
                                            }
                                        );
                                    }
                                    else
                                    {
                                        var deviceMD5 = CreateMD5(content);
                                        if (CreateMD5(dwg.GPX) != deviceMD5)
                                        {
                                            UpdateRecordInRepo(new DeviceGPX
                                            {
                                                GPS = dwg.GPS,
                                                GPX = content,
                                                Filename = dwg.Filename,
                                                RowID = dwg.RowID,
                                                MD5 = deviceMD5,
                                                GPXType = "waypoint",
                                                TimeRangeStart = gpxFile.DateRangeStart,
                                                TimeRangeEnd = gpxFile.DateRangeEnd
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if(Entities.TrackViewModel.Tracks.Count>0)
            {
                foreach(var track_set in Entities.TrackViewModel.Tracks)
                {
                    if(Entities.GPSViewModel.Exists(track_set.Key))
                    {
                        foreach (var track in track_set.Value)
                        {
                            if (File.Exists(track.FullFileName))
                            {
                                var file = Path.GetFileName(track.FullFileName);
                                string content;
                                using (StreamReader sr = File.OpenText(track.FullFileName))
                                {
                                    content = sr.ReadToEnd();
                                }
                                var dwg = GetDeviceGPX(track.GPS, file);
                                var gpxFile = Entities.GPXFileViewModel.GetFile(track.GPS, track.FileName);
                                if (dwg == null)
                                {
                                    AddRecordToRepo(
                                        new DeviceGPX
                                        {
                                            GPS = track.GPS,
                                            Filename = file,
                                            GPX = content,
                                            RowID = NextRecordNumber,
                                            MD5 = CreateMD5(content),
                                            GPXType = "track",
                                            TimeRangeStart = gpxFile.DateRangeStart,
                                            TimeRangeEnd = gpxFile.DateRangeEnd
                                        }
                                    );
                                }
                                else
                                {
                                    var deviceMD5 = CreateMD5(content);
                                    if (CreateMD5(dwg.GPX) != deviceMD5)
                                    {
                                        UpdateRecordInRepo(new DeviceGPX
                                        {
                                            GPS = dwg.GPS,
                                            GPX = content,
                                            Filename = dwg.Filename,
                                            RowID = dwg.RowID,
                                            MD5 = deviceMD5,
                                            GPXType = "track",
                                            TimeRangeStart = gpxFile.DateRangeStart,
                                            TimeRangeEnd = gpxFile.DateRangeEnd
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
                if (DeviceGPXCollection.Count == 0)
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
            while (index < DeviceGPXCollection.Count)
            {
                if (DeviceGPXCollection[index].RowID == rowID)
                {
                    DeviceGPXCollection.RemoveAt(index);
                    break;
                }
                index++;
            }
        }
    }
}
