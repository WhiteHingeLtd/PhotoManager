using PhotoManager.DataGridClasses;
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
using WHLClasses;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for RowDetailsControl.xaml
    /// </summary>
    public partial class RowDetailsControl : UserControl
    {
        private MainWindow MainWindowRef = null;
        public GridSku ActiveGridSku = null;
        public RowDetailsControl()
        {
            InitializeComponent();
        }
        public RowDetailsControl(GridSku gs, MainWindow mainwindow)
        {
            InitializeComponent();
            MainWindowRef = mainwindow;
            ActiveGridSku = gs;
            skutb.Text = gs.Data.SKU + " " + gs.Data.Title.Label;
        }
        
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowFilmstrip();
            FillControls();
        }

        internal void Page_Shown()
        {
            //ShowFilmstrip(); //Not anymore. We dont wanna do this when they load a thing, they can copy stuff to the filmstrip if they really want.
        }

        private void ShowFilmstrip()
        {
            //Iterate through images to get a lsit of fileinfos
            List<FileInfo> fileInfos = new List<FileInfo>();
            foreach (WhlSKU child in ActiveGridSku.GetChildren())
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

            MainWindowRef.ClearFilmstrip();
            MainWindowRef.PopulateFilmstrip(fileInfos);
        }

        private void FillControls()
        {
            //Fill in the menial stuff.
            LabelShort.Content = ActiveGridSku.Data.Title.Label;
            //Create the things
            foreach (WhlSKU sku in ActiveGridSku.GetChildren())
            {
                if (sku.NewItem.IsListed || sku.PackSize == 1)
                {
                    packsizeControlContainer.Children.Add(new PackSizeControl(sku,MainWindowRef));
                }
            }
        }
    }
}
