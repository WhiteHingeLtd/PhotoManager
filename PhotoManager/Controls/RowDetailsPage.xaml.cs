using System;
using System.Collections.Generic;
using System.IO;
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
using PhotoManager.DataGridClasses;
using WHLClasses;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for RowDetailsPage.xaml
    /// </summary>
    public partial class RowDetailsPage : Page
    {
        private MainWindow MainWindowRef = null;
        public GridSku ActiveGS = null;
        public RowDetailsPage()
        {
            InitializeComponent();
        }
        public RowDetailsPage(GridSku gs, MainWindow mainwindow)
        {
            InitializeComponent();
            MainWindowRef = mainwindow;
            ActiveGS = gs;
            skutb.Text = gs.Data.SKU + " " +gs.Data.Title.Label;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(MainWindowRef.Data_SkusMixdown.SearchSKUS(testbox.Text)[0].Title.Invoice);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Iterate through images to get a lsit of fileinfos
            List<FileInfo> fileInfos = new List<FileInfo>();
            foreach (WhlSKU child in ActiveGS.GetChildren())
            {
                foreach (SKUImage img in child.Images)
                {
                    try
                    {
                        fileInfos.Add(new FileInfo(img.FullImagePath));
                    }
                    catch (Exception a)
                    {
                        //Ah weklkl.
                    }
                }
            }
            
            MainWindowRef.ResetFilmstrip();
            MainWindowRef.PopulateFilmstrip(fileInfos);
        }
    }
}
