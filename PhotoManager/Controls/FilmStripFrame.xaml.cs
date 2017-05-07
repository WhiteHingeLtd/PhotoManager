using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for FilmStripFrame.xaml
    /// </summary>
    public partial class FilmStripFrame : UserControl
    {
        private BitmapImage SourceImage = null;
        private FileInfo SourceFileInfo = null;
        public FilmStripFrame(FileInfo IDText)
        {
            InitializeComponent();
            //Set the text of the thing for now.
            FilmstripID.Text = IDText.Name + " - " + (Math.Round(Convert.ToDouble(IDText.Length / 1024)) as Double?).ToString() + "kB";
            SourceFileInfo = IDText;
            GO();
        }
        private BitmapImage MakeImage()
        {
            SourceImage = new BitmapImage();
            SourceImage.BeginInit();
            SourceImage.DecodePixelWidth = 134;
            SourceImage.DecodePixelHeight = 134;
            //SourceImage.CacheOption = BitmapCacheOption.OnDemand;
            SourceImage.UriSource = new Uri(SourceFileInfo.FullName);
            SourceImage.EndInit();
            return SourceImage;
        }
        private void ApplyImage(BitmapImage image)
        {
            FilmstripImage.Source = image;
            Storyboard sb = this.FindResource("FadeImageIn") as Storyboard;
            sb.Begin();
        }

        //NO TOUCH ABOVE HERE

        private Dispatcher Disp = null;
        
        public void GO()
        {
            Disp = Dispatcher.CurrentDispatcher;


            Thread Background = new Thread(new ThreadStart(BackgroundThread));
            Background.Start();

            //worker.DoWork += dowork;
            //worker.RunWorkerCompleted += complete;
            //worker.RunWorkerAsync();

        }

        private void dowork(object sender, DoWorkEventArgs e)
        {
            BitmapImage img = MakeImage();

            SourceImage.Freeze();

            Disp.BeginInvoke(DispatcherPriority.Background, new Action(() => { ApplyImage(SourceImage); }));
        }

        private void complete(object sender, RunWorkerCompletedEventArgs e)
        {
            //ApplyImage(SourceImage);
        }


        private void BackgroundThread()
        {

            BitmapImage img = MakeImage();

            SourceImage.Freeze();

            Disp.BeginInvoke(DispatcherPriority.Normal, new Action(() => { ApplyImage(SourceImage); }));
        }

    }
}
