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
        public ShapeFileAttributesWindow()
        {
            InitializeComponent();
        }

        private void OnButtonCLick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void ShowShapeFileAttribute(Shapefile sf)
        {
            DataTable dt = new DataTable();

            for(int y=0;y<sf.NumFields;y++)
            {
                Field fld = sf.Field[y];
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
            for(int x=0;x<sf.NumShapes;x++)
            {
                var row = dt.NewRow();
                for(int z=0;z<sf.NumFields;z++)
                {
                    row[z] = sf.CellValue[z, x];
                }
                dt.Rows.Add(row);
            }

            dataGridAttributes.DataContext = dt;
        }
    }
}
