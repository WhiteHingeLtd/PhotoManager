using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WHLClasses;
using WHLClasses.MySQL_Old;

namespace PhotoManager.Controls
{
    /// <summary>
    /// Interaction logic for PackSizeControl.xamls
    /// </summary>
    public partial class PackSizeControl : UserControl
    {
        internal WhlSKU ActiveSku = null;
        private MainWindow MainWindowRef = null;
        private bool _IsLoaded = false;
        public PackSizeControl(WhlSKU CurrentItem, MainWindow main)
        {
            InitializeComponent();
            _UiDispatcher = this.Dispatcher;
            MainWindowRef = main;
            ActiveSku = CurrentItem;
            packsizeText.Text = CurrentItem.PackSize.ToString();
            if (!ActiveSku.NewItem.IsListed){packsizeText.Background = Brushes.Red;}
            RedoButton.IsChecked = MainWindowRef.NeededState(CurrentItem.SKU);
            RefreshImages();
            main.ItemGrid.ScrollIntoView(main.ItemGrid.SelectedItem);
            _IsLoaded = true;

        }

        internal void RefreshImages()
        {
            packsizeFilmStripContainer.Children.Clear();
            bool PrimaryFound = false;
            foreach (SKUImage Image in ActiveSku.Images)
            {
                try
                {
                    FileInfo File = new FileInfo(Image.FullImagePath);
                    FilmStripFrame newimg = new FilmStripFrame(File, MainWindowRef, false, Image.isPrimary);
                    newimg.container = this;
                    if (Image.isPrimary)
                    {
                        PrimaryFound = true;
                        packsizeFilmStripContainer.Children.Insert(0,newimg);
                        //If it is the primary, we want it to show at the top.
                    }
                    else
                    {
                        packsizeFilmStripContainer.Children.Add(newimg);
                    }
                }
                catch (Exception a)
                {
                    //Oh no we had a missing image.
                }

            }
            if (PrimaryFound)
            {
                packsizeAlertText.Visibility = Visibility.Collapsed;
            }
            else
            {
                packsizeAlertText.Visibility = Visibility.Visible;
            }
            //Make the porgess bar invisible just to be safe.
            RefreshingProgressBar.Visibility = Visibility.Collapsed;
        }

        private Dispatcher _UiDispatcher = null;

        internal void refreshAndRerender()
        {
            //Make a new thread to refresh on. But first we need to show the refresher.
            RefreshingProgressBar.Visibility = Visibility.Visible;
            //Now we can start the thead
            var TS = new ThreadStart(refreshChildrensData);
            var BgThread = new Thread(TS);
            BgThread.IsBackground = true;
            BgThread.Start();
        }

        internal void refreshChildrensData()
        {
            ActiveSku.UpdateImages();
            _UiDispatcher.Invoke(RefreshImages, DispatcherPriority.Background);
        }

        private void DropZoneRegion_Drop(object sender, DragEventArgs e)
        {   
            //SOEMTHING DROPPED
            if (e.Data.GetDataPresent(typeof(FileInfo)))
            {
                // call the ez funciton.
                AddNewImage(e.Data.GetData(typeof(FileInfo)) as FileInfo, (packsizeFilmStripContainer.Children.Count==0));
            }
            else
            {
                MessageBox.Show("Dropped object of unkown types: " + Environment.NewLine + String.Join(Environment.NewLine, e.Data.GetFormats(false)));
            }

        }


        private void DropZoneRegion_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
        }

        private void AddImage(SKUImage Image)
        {
            FileInfo File = new FileInfo(Image.FullImagePath);
            FilmStripFrame newimg = new FilmStripFrame(File, MainWindowRef, false, Image.isPrimary);
            packsizeFilmStripContainer.Children.Add(newimg);
        }

        internal void AddNewImage(FileInfo sourceFileInfo, bool Primary =false)
        {
            RefreshingProgressBar.Visibility = Visibility.Visible;
            //Add to the database (with primary if applicable)
            if (Primary) { MySQL_Ext.insertupdate("UPDATE whldata.sku_images SET IsPrimary='False' WHERE sku='" + ActiveSku.SKU + "';"); }
            string Key = ActiveSku.SKU + "_" + sourceFileInfo.Name.Replace(sourceFileInfo.Extension, "");
            MySQL_Ext.insertupdate(
                "INSERT INTO whldata.sku_images (filename, path, sku, shortsku, IsPrimary) VALUES ('" + Key + "','" +
                sourceFileInfo.FullName.Replace("\\", "\\\\") + "','" + ActiveSku.SKU + "','" + ActiveSku.ShortSku +
                "','" + Primary + "')");
            //Then finally add a changelog.
            MySQL_Ext.insertupdate("INSERT INTO whldata.sku_changelog (shortsku, payrollId, reason, datetimechanged) VALUES ('" + ActiveSku.ShortSku + "'," + MainWindowRef.Data_User.AuthenticatedUser.PayrollId.ToString() + ",'Added " + sourceFileInfo.Name + " to pack of " + ActiveSku.PackSize.ToString() + "','" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "')");
            //Then RefreshRerender™
            refreshAndRerender();
        }

        private void OpenInFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_IsLoaded) { MainWindowRef.UpdateNeeded(ActiveSku.SKU, RedoButton.IsChecked.Value); }
        }
    }
}
