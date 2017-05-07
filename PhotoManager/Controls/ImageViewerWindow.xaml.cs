using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for ImageViewerWindow.xaml
    /// </summary>
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow()
        {
            InitializeComponent();
        }

        public ImageViewerWindow(FileInfo RealImage, BitmapImage OriginalImage)
        {
            InitializeComponent();
            this.Title = RealImage.Name + " - " + (Math.Round(Convert.ToDouble(RealImage.Length / 1024)) as Double?).ToString() + "kB";
            //Now we've loaded a shitty quality one, we can show it then load in the good one.
            ImageControl.Source = OriginalImage;
            this.Show();
            //And now we start a loading job in the background. 100% stolen from thw way the filmstrip does it.
            SourceFileInfo = RealImage;
            ShowImage();
        }

        private FileInfo SourceFileInfo = null;
        private BitmapImage SourceImage = null;

        private BitmapImage MakeImage()
        {
            Console.WriteLine("Making image");
            SourceImage = new BitmapImage();
            SourceImage.BeginInit();
            SourceImage.CacheOption = BitmapCacheOption.OnDemand;
            SourceImage.UriSource = new Uri(SourceFileInfo.FullName);
            SourceImage.EndInit();
            Console.WriteLine("Made image");
            return SourceImage;
        }
        private void ApplyImage(BitmapImage image)
        {
            Console.WriteLine("Applying image");
            ImageControl.Source = image;
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
            BitmapImage img = MakeImage();
            SourceImage.Freeze();
            Disp.BeginInvoke(DispatcherPriority.Normal, new Action(() => { ApplyImage(SourceImage); }));


        }

        private void ShowInFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "/select, \"" + SourceFileInfo.FullName + "\"");
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(SourceFileInfo.FullName);
        }
    }
}
