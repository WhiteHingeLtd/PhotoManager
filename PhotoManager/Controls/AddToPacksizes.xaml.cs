using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhotoManager.DataGridClassess;
using WHLClasses;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for AddToPacksizes.xaml
    /// </summary>
    public partial class AddToPacksizes : Window
    {
        public AddToPacksizes()
        {
            InitializeComponent();
        }

        public AddToPacksizes(FileInfo RealImage, BitmapImage OriginalImage, SkuCollection Children)
        {
            InitializeComponent();
            //Now we've loaded a shitty quality one, we can show it then load in the good one.
            ImagePreview.Source = OriginalImage;
            this.Show();
            //And now we start a loading job in the background. 100% stolen from thw way the filmstrip does it.
            SourceFileInfo = RealImage;

            //Now we can start loading the skus and shit.
            LoadUI(Children);

            ShowImage();
        }
    

        private FileInfo SourceFileInfo = null;
        private BitmapImage SourceImage = null;
        
        private void ApplyImage(BitmapImage image)
        {
            Console.WriteLine("Applying image");
            ImagePreview.Source = image;
        }


        private Dispatcher Disp = null;

        public void ShowImage()
        {
            Disp = Dispatcher.CurrentDispatcher;

            if (SourceFileInfo.Exists)
            {
                Thread Background = new Thread(new ThreadStart(BackgroundThread));
                Background.IsBackground = true;
                Background.Start();
            }

        }

        private void BackgroundThread()
        {
            Console.WriteLine("Thread Starting");
            Thread.Sleep(100);
            Console.WriteLine("Thread Started");
            BitmapImage img = Methods.MakeImage(300, ref SourceImage, SourceFileInfo);
            SourceImage.Freeze();
            Disp.BeginInvoke(DispatcherPriority.Normal, new Action(() => { ApplyImage(SourceImage); }));


        }

        private void LoadUI(SkuCollection Children)
        {
            //Use the first one for showing the title
            this.Title = "Add image to " + Children[0].ShortSku + " - " + Children[0].Title.Invoice;
            ImageName.Text = SourceFileInfo.Name;
            ImageModified.Text = "Modified " + SourceFileInfo.LastWriteTime.ToString("dd/MM/yyyy HH:mm");
            //Clear the packsziebuttoncontainer
            PackSizeButtonContainer.Children.Clear();
            //Iterate the files
            foreach (WhlSKU child in Children)
            {
                if (child.PackSize != 0)
                {
                    //New button
                    ToggleButton Newb = new ToggleButton();
                    Newb.Template = FindResource("CustomToggleButtons") as ControlTemplate;
                    Newb.ApplyTemplate();
                    //We can tag it with the sku.
                    Newb.Tag = child;
                    //Now we've made it, we can actually fill the buttons with useful information.
                    string unused = "";
                    string PacksizeText = child.PackSize.ToString() + " pack (";
                    string imgcount = child.Images.Count.ToString() + " image";
                    if (!child.NewItem.IsListed) { unused = " (unused)";}
                    if (child.Images.Count == 1){imgcount += ")";}else{imgcount += "s)";}
                    Newb.Content = PacksizeText + imgcount + unused;
                    //Now we need to check if the image is already on it.
                    if (child.Images.Any((image) => { return image.FullImagePath == SourceFileInfo.FullName; }))
                    {
                        //We have it on this one.
                        Newb.IsChecked = true;
                    }
                    else
                    {
                        Newb.IsChecked = false;
                    }
                    PackSizeButtonContainer.Children.Add(Newb);
                }
                
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PacksizeSwitchWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key== Key.Escape) { this.Close();}
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            //We'll have to iterate through them all and apply them accordingly.
        }
    }

}

