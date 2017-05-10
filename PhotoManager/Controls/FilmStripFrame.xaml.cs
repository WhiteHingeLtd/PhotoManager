using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Policy;
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
using PhotoManager.DataGridClassess;
using WHLClasses.MiscFunctions;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for FilmStripFrame.xaml
    /// </summary>
    public partial class FilmStripFrame : UserControl
    {
        private BitmapImage SourceImage = null;
        internal FileInfo SourceFileInfo = null;
        internal bool _isprimary = false;
        private MainWindow MainWindowRef = null;
        public bool IsPrimary
        {
            get { return _isprimary; }
            set
            {
                _isprimary = value;
                UpdatePrimaryUI();
            }
        }

        private void UpdatePrimaryUI()
        {
            if (_isprimary)
            {
                FilmstripID.Background = new SolidColorBrush(Color.FromArgb(178, 178, 129, 24));
            }
            else
            {
                FilmstripID.Background = new SolidColorBrush(Color.FromArgb(178,255,255,255));
            }
        }

        public FilmStripFrame(FileInfo IDText, MainWindow mainRef, bool FullSize, bool Primary = false)
        {
            InitializeComponent();
            SourceFileInfo = IDText;
            MainWindowRef = mainRef;
            //We use fulklsize to work out if itsa going in the main fiilmstrip or one of the mini ones on the packsize controls.
            if (FullSize)
            {
                FilmstripID.Text = IDText.Name;
                //Hide the buttons which we dont want on fimlstrip.
                MakePrimaryButton.Visibility = Visibility.Collapsed;
                CopyToFilmstripButton.Visibility = Visibility.Collapsed;
                PacksizesButton.Visibility = Visibility.Collapsed;
                AddToSkuButton.Visibility = Visibility.Visible;
                RemoveFromFilmstripButton.Visibility = Visibility.Visible;
            }
            else
            {
                //Hide the ones we sont want on the packsizes grid.
                FilmstripID.Text = IDText.Name;
                MainGrid.Background = Brushes.White;
                AddToSkuButton.Visibility = Visibility.Collapsed;
                PacksizesButton.Visibility = Visibility.Visible;
                CopyToFilmstripButton.Visibility = Visibility.Visible;
                MakePrimaryButton.Visibility = Visibility.Visible;
                RemoveFromFilmstripButton.Visibility = Visibility.Collapsed;
            }
            IsPrimary = Primary;
            
            ShowImage();


        }

        #region 'dodgy image stuff'
        
        private void ApplyImage(BitmapImage image)
        {
            FilmstripImage.Source = image;
            AnimateImageIn();
        }

        private void AnimateImageIn()
        {
            Storyboard sb = this.FindResource("FadeImageIn") as Storyboard;
            sb.Begin();
        }

        //NO TOUCH ABOVE HERE

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
            else { AnimateImageIn(); }
            
        }
        

        private void BackgroundThread()
        {
            
            BitmapImage img = Methods.MakeImage(134, ref SourceImage, SourceFileInfo);
            SourceImage.Freeze();
            Disp.BeginInvoke(DispatcherPriority.Normal, new Action(() => { ApplyImage(SourceImage); }));

            
        }

        #endregion

        #region "removal"

        
        private void RemoveFromContainer(Action<FilmStripFrame> Callback)
        {
            Storyboard Sb = this.FindResource("AnimateRemoval") as Storyboard;
            Sb.Begin();
            _RemovalCallback = Callback;

            Thread asdThread = new Thread(ProcessRemovalCallback);
            asdThread.Start();
        }

        private Action<FilmStripFrame> _RemovalCallback = null;

        private void ProcessRemovalCallback()
        {
            Thread.Sleep(700); //Sleep for 1000ms becuae animation takes 700ms then we remove it when its done and render has had time to do it.
            Disp.Invoke(() => { _RemovalCallback.Invoke(this); },DispatcherPriority.Normal);
        }

        #endregion
        private void OpenInFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "/select, \"" + SourceFileInfo.FullName + "\"");
        }

        private void EnlargeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenPhotoViewer();
        }

        private void FilmstripImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OpenPhotoViewer();
            }
        }

        private void OpenPhotoViewer()
        {
            new ImageViewerWindow(SourceFileInfo, SourceImage);
        }

        private void CopyToFilmstripButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowRef.AddFilmstripItem(SourceFileInfo);
        }

        private void RemoveFromFilmstripButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveFromContainer(MainWindowRef.RemoveControlFromFilmstrip);
        }

        private void PacksizesButton_Click(object sender, RoutedEventArgs e)
        {
            //Show up the packsizes window modally
            var AddPacksizeWindow = new AddToPacksizes(SourceFileInfo,SourceImage,MainWindowRef.CurrentInstance.GetChildren());
        }

        private void AddToSkuButton_Click(object sender, RoutedEventArgs e)
        {
            var AddPacksizeWindow = new AddToPacksizes(SourceFileInfo, SourceImage, MainWindowRef.CurrentInstance.GetChildren());
        }
    }
}
