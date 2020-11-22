﻿using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System.IO;



namespace GPXManager.entities
{
    public static class ImportGPSData
    {
        public static string ImportMessage { get; internal set; }
        
        public static bool ImportGPX()
        {
            bool success = false;
            VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog
            {
                Description = "Locate folder with GPX files",
                UseDescriptionForTitle = true,
                SelectedPath = Global.Settings.ComputerGPXFolder
            };
            if ((bool)vfbd.ShowDialog() && Directory.Exists(vfbd.SelectedPath))
            {
                int importCount = Entities.DeviceGPXViewModel.ImportGPX(vfbd.SelectedPath, first: true);
                if (importCount > 0)
                {
                    ImportMessage= $"{importCount} GPX files imported to database";
                    success = true;
                }
                else
                {
                    ImportMessage = "No GPX files were imported to the database";
                }
            }
            return success;
        }
        public static bool ImportGPS()
        {
            bool success = false;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Import GPS with XML";
            ofd.Filter = "XML file (*.xml)|*.xml|All file type (*.*)|*.*";
            ofd.DefaultExt = ".xml";
            if ((bool)ofd.ShowDialog() && File.Exists(ofd.FileName))
            {

                int importCount = Entities.GPSViewModel.ImportGPS(ofd.FileName, out string message);
                if (importCount > 0)
                {
                    ImportMessage = $"{importCount} GPS was imported into the database";
                    success= true;
                }
                else
                {
                    if (message != "Valid XML")
                    {
                        ImportMessage = $"{message}\r\n\r\nNo GPS was imported into the database";
                    }
                    else
                    {
                        ImportMessage = "No GPS was imported into the database";
                    }
                }
            }
            return success;
        }
    }
}
