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

using System.Deployment.Application;
using System.Diagnostics;

namespace GPXManager.views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            Loaded += AboutWindow_Loaded;
        }

        private void AboutWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //string exePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\GPXManager.exe";
            //FileVersionInfo exeVersion = FileVersionInfo.GetVersionInfo(exePath);
            //labelAppVerion.Content = $"App version: {exeVersion.ProductVersion}";

            labelAppVerion.Content = "App version: 1.1.1";

            string mapOCXPath = $@"{AppDomain.CurrentDomain.BaseDirectory}\AxInterop.MapWinGIS.dll";
            FileVersionInfo ocxVersionInfo = FileVersionInfo.GetVersionInfo(mapOCXPath);
            labelMapOCXVersion.Content = $"MapWinGIS OCX version: {ocxVersionInfo.ProductVersion}";
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
