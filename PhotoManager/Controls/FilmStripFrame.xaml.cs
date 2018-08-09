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
using WHLClasses;
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
        internal bool _CanBePrimary = false;
        internal PackSizeControl container { get; set; }
        internal bool isLoaded = false;
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
                MainGrid.Background = new SolidColorBrush(Color.FromArgb(200,30,130,30));
                MakePrimaryButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainGrid.Background = new SolidColorBrush(Color.FromArgb(200, 207, 207, 207));
                MakePrimaryButton.Visibility = Visibility.Visible;
            }
        }

        public FilmStripFrame(FileInfo IDText, MainWindow mainRef, bool Filmstrip, bool Primary = false)
        {
            InitializeComponent();
            SourceFileInfo = IDText;
            MainWindowRef = mainRef;
            //We use fulklsize to work out if itsa going in the main fiilmstrip or one of the mini ones on the packsize controls.
            IsPrimary = Primary;
            if (Filmstrip)
            {
                //Hide the buttons which we dont want on fimlstrip.
                MakePrimaryButton.Visibility = Visibility.Collapsed;
                CopyToFilmstripButton.Visibility = Visibility.Collapsed;
                RemoveFromFilmstripButton.Visibility = Visibility.Visible;
                _CanBePrimary = false;
            }
            else
            {
                //Hide the ones we sont want on the packsizes grid.
                
                CopyToFilmstripButton.Visibility = Visibility.Visible;
                // MakePrimaryButton.Visibility = Visibility.Visible; No, we did this before.
                RemoveFromFilmstripButton.Visibility = Visibility.Collapsed;
                _CanBePrimary = !Primary;
            }

            RedoButton.IsChecked = MainWindowRef.RedoState(SourceFileInfo.FullName);
            
            ShowImage();


        }

        #region 'dodgy image stuff'
        
        private void ApplyImage(BitmapImage image)
        {
            FilmstripImage.Source = image;
            isLoaded = true;
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
            
            BitmapImage img = Methods.MakeImage(212, ref SourceImage, SourceFileInfo);
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
        private void OpenInFolderButton_Click(object sender, RoutedEventArgs e) //This name is a lie.
        {
            if (isLoaded) { MainWindowRef.UpdateRedo(SourceFileInfo.FullName, RedoButton.IsChecked.Value); }
            
        }

        private void EnlargeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenPhotoViewer();
        }

        private void FilmstripImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                if (_CanBePrimary)
                {
                    MakeThisPrimary();
                }
                else if (IsPrimary)
                {
                    MessageBox.Show("This image is already the primary one for this packsize.");
                }
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
            //Remove this image from the item, render reset etc.
            container.RefreshingProgressBar.Visibility = Visibility.Visible;
            //Remove the entry
            MySQL.InsertUpdate("DELETE FROM whldata.sku_images WHERE sku='" + container.ActiveSku.SKU + "' AND path='" + SourceFileInfo.FullName.Replace("\\", "\\\\") + "'");
            //Then finally add a changelog.
            MySQL.InsertUpdate("INSERT INTO whldata.sku_changelog (shortsku, payrollId, reason, datetimechanged) VALUES ('" + container.ActiveSku.ShortSku + "'," + MainWindowRef.Data_User.AuthenticatedUser.PayrollId.ToString() + ",'Removed " + SourceFileInfo.Name + " from pack of " + container.ActiveSku.PackSize.ToString() + "','" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "')");
            //Reset
            container.refreshAndRerender();


        }

        private void AddToSkuButton_Click(object sender, RoutedEventArgs e)
        {
            var AddPacksizeWindow = new AddToPacksizes(SourceFileInfo, SourceImage, MainWindowRef.CurrentInstance.GetChildren());
        }

        private void FilmstripImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //Mouse is pressed so it must be dragging.
                DragDrop.DoDragDrop(this, SourceFileInfo, DragDropEffects.Copy);
                //We now do the drag ops!

            }
        }

        private void MakePrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            MakeThisPrimary();
        }

        internal void MakeThisPrimary()
        {
            //Show the magic bar.
            container.RefreshingProgressBar.Visibility = Visibility.Visible;

            //First we need to remove the primary from all of them.
            MySQL.InsertUpdate("UPDATE whldata.sku_images SET IsPrimary='False' WHERE sku='" + container.ActiveSku.SKU + "';");
            //Then we set this one to it.
            MySQL.InsertUpdate("UPDATE whldata.sku_images SET IsPrimary='True' WHERE sku='" + container.ActiveSku.SKU + "' AND path='"+SourceFileInfo.FullName.Replace("\\","\\\\")+"'");
            //Then finally add a changelog.
            MySQL.InsertUpdate("INSERT INTO whldata.sku_changelog (shortsku, payrollId, reason, datetimechanged) VALUES ('" + container.ActiveSku.ShortSku +"'," + MainWindowRef.Data_User.AuthenticatedUser.PayrollId.ToString() +",'Marked " + SourceFileInfo.Name + " as the primary image" + " to pack of " + container.ActiveSku.PackSize.ToString() + "','" +DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +"')");
            //Done. Just need to tell the container to refresh.
            container.refreshAndRerender(); //™
        }

    }
}
