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
    /// Interaction logic for PackSizeControl.xamls
    /// </summary>
    public partial class PackSizeControl : UserControl
    {
        private WhlSKU ActiveSku = null;
        private MainWindow MainWindowRef = null;
        public PackSizeControl(WhlSKU CurrentItem, MainWindow main)
        {
            InitializeComponent();
            MainWindowRef = main;
            ActiveSku = CurrentItem;
            packsizeText.Text = CurrentItem.PackSize.ToString();
            RefreshImages();
            
        }

        internal void RefreshImages()
        {
            foreach (SKUImage Image in ActiveSku.Images)
            {
                try
                {
                    FileInfo File = new FileInfo(Image.FullImagePath);
                    FilmStripFrame newimg = new FilmStripFrame(File, MainWindowRef, false, Image.isPrimary);
                    packsizeFilmStripContainer.Children.Add(newimg);
                    newimg.BringIntoView();
                }
                catch (Exception a)
                {
                    //Oh no we had a missing image.
                }

            }
        }

    }
}
