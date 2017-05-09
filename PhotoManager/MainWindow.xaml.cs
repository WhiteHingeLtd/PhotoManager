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
using System.Windows.Media;
using System.Windows.Threading;
using PhotoManager.Controls;
using PhotoManager.DataGridClasses;
using WHLClasses;
using WHLClasses.Authentication;

namespace PhotoManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region "Vars and init"
        internal GenericDataController loader = new GenericDataController();
        internal SkuCollection Data_Skus = null;
        internal EmployeeCollection Data_Employees = null;
        internal AuthClass Data_User = null;
        internal SkuCollection Data_SkusMixdown = null;
        internal FileSystemWatcher FolderWatcher = null; 
        public DispatcherTimer timer = null;
        internal Dispatcher MainDispatcher = null;
        public List<FileInfo> Photos = new List<FileInfo>();
        private GridSku CurrentInstance = null;
        private int Presses = 0;
        public MainWindow()
        {
            InitializeComponent();
            Photos = (new System.IO.DirectoryInfo("T:\\Photos\\Amazon")).EnumerateFiles("*.jpg").ToList();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.6);
            timer.Tick += TimerTick;
            timer.Start();
            
        }

        #endregion

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Presses += 1;
            AddFilmstripItem(Photos[Presses]);
            fstb.Text = Presses.ToString();
        }

        #region "Film strip FileSystemWatecher stuff

        private void initialateFolderWatcher(string Folder = "T:\\Photos\\Amazon\\", bool Events = true)
        {
            try
            {
                FolderWatcher.Changed -= filechangeFolderWatcher;
                FolderWatcher.Created -= filechangeFolderWatcher;
                FolderWatcher.Renamed -= filerenameFolderWatcher;

            }
            catch (Exception e)
            {
            }
            FolderWatcher = new FileSystemWatcher();
            FolderWatcher.Filter = "*.jpg";
            FolderWatcher.Path = Folder;
            
            FolderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
            FolderWatcher.Changed += filechangeFolderWatcher;
            FolderWatcher.Renamed += filerenameFolderWatcher;
            FolderWatcher.Error += delegate(object sender, ErrorEventArgs args)
            {
                MessageBox.Show(args.GetException().Message);
            };

            FolderWatcher.EnableRaisingEvents = Events;


        }

        private void updateFolderWatcherVars()
        {
            try
            {
                initialateFolderWatcher(folderWatcherSourceFolder.Text, folderWatcherEnabled.IsChecked.Value);
                folderWatcherSourceFolder.Background = Brushes.White;
            }
            catch
            {
                try { folderWatcherSourceFolder.Background = Brushes.PaleVioletRed; } catch { }
                
                //FolderWatcher.EnableRaisingEvents = false;

            }
                
        }

        private void filechangeFolderWatcher(object Sender, FileSystemEventArgs e)
        {
            //Instead of blindly adding controls, we should check if thye've been added and shuffle them back to the start. Or more accurately, remake them.
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                //Get the FileInfo for it and then feed it to the add function.
                FileInfo NewItem = new FileInfo(e.FullPath);
                //Gotta invoke on the main thread
                ParameterizedThreadStart asd = new ParameterizedThreadStart(PostponeAddingToFilmstrip);
                Thread asdThread = new Thread(asd);
                asdThread.Start(NewItem);
            }
        }

        /// <summary>
        /// This does some weird thread marshalling around. It originally was used to wait for Photoshop to finish writiing the file but we managed to get the worker to stop acting like such a retard. Now it just tells the dispatcher to do it when its not busy.
        /// </summary>
        /// <param name="NewItem"></param>
        private void PostponeAddingToFilmstrip(object NewItem)
        {
            //System.Threading.Thread.Sleep(2000);
            MainDispatcher.Invoke(new Action(() => AddFilmstripItem(NewItem as FileInfo)),DispatcherPriority.Background);
        }

        private void filerenameFolderWatcher(object Sender, RenamedEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
            {
                FileInfo NewItem = new FileInfo(e.FullPath);
                //Gotta invoke on the main thread
                ParameterizedThreadStart asd = new ParameterizedThreadStart(PostponeAddingToFilmstrip);
                Thread asdThread = new Thread(asd);
                asdThread.Start(NewItem);
            }
        }

        private void TimerTick(object sender, object e)
        {
            if (autocheckbox.IsChecked.Value) { button_Click(null,null);}
        }

        private void Main_Window_Initialized(object sender, EventArgs e)
        {
            Splash newSplash = new Splash{HomeRef = this};
            newSplash.InitializeComponent();
            MainDispatcher = this.Dispatcher;
            this.IsEnabled = false;
            this.Show();
            newSplash.ShowDialog();
            this.IsEnabled = true;
            if (Filmstrip.Children.Count > 0) { (Filmstrip.Children[Filmstrip.Children.Count - 1] as FrameworkElement).BringIntoView(); }
            
        }

        private void folderVarsChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) { updateFolderWatcherVars(); }
        }

        private bool _isLoaded = false;
        #endregion

        #region "SplashStartup"

        internal void SplashStartupTasks(BackgroundWorker worker)
        {
            worker.ReportProgress(0, "Loading Filmstrip data");
            try
            {
                _FilmstripSaveList =
                    loader.LoadAnything(Constants_ish.FilmstripSaveDataLocation()).Value as List<FileInfo>;
                foreach (FileInfo file in _FilmstripSaveList)
                {
                    if (file.Exists)
                    {
                        Dispatcher.Invoke(delegate { AddFilmstripItem(file); }, DispatcherPriority.Background);
                    }
                }
            }
            catch (IOException ioe)
            {
                _FilmstripSaveList = new List<FileInfo>();
            }
            catch (Exception ex)
            {
                Console.Write("UNHANDLED!!" + ex.ToString());
            }
            
            worker.ReportProgress(0,"Loading Photo Detector");
            initialateFolderWatcher();
            _isLoaded = true;
        }



#endregion

#region "Filmstrip additions and stuff"

        private List<FileInfo> _FilmstripSaveList = null;

        internal void ClearFilmstrip()
        {
            Filmstrip.Children.Clear();
        }

        internal void PopulateFilmstrip(List<FileInfo> Files)
        {
            List<FileInfo> NewFiles = new List<FileInfo>();
            foreach (FileInfo File in Files)
            {
                if (!NewFiles.Contains(File))
                {
                    NewFiles.Add(File);
                    AddFilmstripItem(File);
                }
            }
        }

        internal void AddFilmstripItem(FileInfo file)
        {
            try
            {
                FilmStripFrame newimg = new FilmStripFrame(file, this, true, false);
                Filmstrip.Children.Add(newimg);
                newimg.BringIntoView();
                if (_isLoaded)
                {
                    MainDispatcher.Invoke(SaveFilmstrip, DispatcherPriority.ApplicationIdle);
                }
            }
            catch (Exception a)
            {

            }
        }

        internal void RemoveControlFromFilmstrip(FilmStripFrame control)
        {
            Filmstrip.Children.Remove(control); //ez.
            if (_isLoaded)
            {
                SaveFilmstrip();
            }
        }

        internal void SaveFilmstrip()
        {
            //Iterate through them all and extract their fileinfos.
            var FileInfos = new List<FileInfo>();
            foreach (FilmStripFrame frame in Filmstrip.Children)
            {
                FileInfos.Add(frame.SourceFileInfo);
            }

            //Get the last (up to) 50
            int SkipCount = Math.Max(0, (FileInfos.Count) - 50);
            List<FileInfo> last50 = FileInfos.Skip(SkipCount).ToList();
            //SAVE
            Console.WriteLine("Saving filmstrip data...");
            loader.SaveDataToFile(Constants_ish.FilmstripSaveDataLocation(),last50,false);
            Console.Write("   Saved!");
        }

#endregion

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            //Search the skucoll for stuff that matcges,
            SkuCollection searchresults = Data_SkusMixdown.SearchSKUS(SearchBox.Text);
            //Crete the new list to show in the grid.
            List<GridSku> gridcontentList = new List<GridSku>();
            foreach (WhlSKU Sku in searchresults)
            {
                GridSku NewGS = new GridSku(Sku, this);
                gridcontentList.Add(NewGS);
                if (searchresults.Count < 50){NewGS.LoadChildrenAsync();}
                

            }
            //Set the source
            ItemGrid.ItemsSource = gridcontentList;
        }



        private void ItemGrid_RowDetailsVisibilityChanged(object sender, System.Windows.Controls.DataGridRowDetailsEventArgs e)
        {
            //Basically when a row is selected
            if ((CurrentInstance != e.Row.Item as GridSku))
            {
                Grid HostGrid = e.DetailsElement as Grid;
                GridSku GSInst = e.Row.Item as GridSku;
                CurrentInstance = GSInst;
                if (HostGrid.Children.Count == 0)
                {
                    
                    HostGrid.Children.Add(new RowDetailsControl(GSInst, this));


                }
                else
                {
                    (HostGrid.Children[0] as RowDetailsControl).Page_Shown();
                }
                
            }
            
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Constants_ish.FilmstripSaveDataLocation());
        }
    }
}
