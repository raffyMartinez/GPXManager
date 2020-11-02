using System;
using System.Collections.Generic;
using System.Data;
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
using MapWinGIS;

namespace GPXManager.entities.mapping.Views
{
    /// <summary>
    /// Interaction logic for ShapeFileAttributesWindow.xaml
    /// </summary>
    public partial class ShapeFileAttributesWindow : Window
    {
        private static ShapeFileAttributesWindow _instance;
        public ShapeFileAttributesWindow()
        {
            InitializeComponent();
            Closing += OnWindowClosing;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ApplyPlacement();
        }
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;
            this.SavePlacement();
        }

        public static ShapeFileAttributesWindow GetInstance()
        {
            if (_instance == null) _instance = new ShapeFileAttributesWindow();
            return _instance;
        }

        private void OnButtonCLick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public Shapefile ShapeFile { get; set; }

        public void ShowShapeFileAttribute()
        {
            DataTable dt = new DataTable();

            for(int y=0;y< ShapeFile.NumFields;y++)
            {
                Field fld = ShapeFile.Field[y];
                string fieldCaption = fld.Name;
                Type t = typeof(int);
                switch (fld.Type)
                {
                    case FieldType.DOUBLE_FIELD:
                        t = typeof(double);
                        break;
                    case FieldType.STRING_FIELD:
                        t = typeof(string);
                        break;
                    case FieldType.INTEGER_FIELD:
                        t = typeof(int);
                        break;
                    case FieldType.DATE_FIELD:
                        t = typeof(DateTime);
                        break;
                    case FieldType.BOOLEAN_FIELD:
                        t = typeof(bool);
                        break;
                }
                dt.Columns.Add(new DataColumn { Caption = fieldCaption, DataType = t,ColumnName = fieldCaption });
            }

            DataRow row;
            if (ShapeFile.NumSelected == 0)
            {
                for (int x = 0; x < ShapeFile.NumShapes; x++)
                {
                    row = dt.NewRow();
                    for (int z = 0; z < ShapeFile.NumFields; z++)
                    {
                        row[z] = ShapeFile.CellValue[z, x];
                    }
                    dt.Rows.Add(row);
                }
            }
            else
            {
                for (int x=0;x<ShapeFile.NumShapes;x++)
                {
                    if(ShapeFile.ShapeSelected[x])
                    {
                        row = dt.NewRow();
                        for (int z=0; z<ShapeFile.NumFields;z++)
                        {
                            row[z] = ShapeFile.CellValue[z, x];
                        }
                        dt.Rows.Add(row);
                    }
                }
            }

            dataGridAttributes.DataContext = dt;
        }

        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
