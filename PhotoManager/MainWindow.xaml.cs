using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
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
        internal GridSku CurrentInstance = null;
        private int Presses = 0;
        internal OpenFileDialog FileDialog= new OpenFileDialog();
        internal SaveFileDialog SaveDialog = new SaveFileDialog();
        internal Dictionary<string, bool> ReDoBools = null;
        internal Dictionary<string, bool> NeedsImgBools = null;
        //Pageing
        private int _CurrentPage = 1;
        private int _PageitemCount = 100;
        private int _PageCount = 0;


        public MainWindow()
        {
            InitializeComponent();
            Photos = (new System.IO.DirectoryInfo("T:\\Photos\\Amazon")).EnumerateFiles("*.jpg").ToList();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.6);
            timer.Tick += TimerTick;
            timer.Start();

            //Set up the filedialog
            FileDialog.Multiselect = true;
            FileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG";

            //And the savedialog
            SaveDialog.DefaultExt = "*.CSV";
            SaveDialog.AddExtension = true;
            SaveDialog.Filter = "CSV Files(*.CSV)|*.CSV";
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
            if (Filmstrip.Children.Count > 0) { DropZoneRegion.BringIntoView(); }
            
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
            worker.ReportProgress(0,"Loading first page of data");

            _PageCount = (int)Math.Ceiling(Convert.ToDouble(Data_SkusMixdown.Count / _PageitemCount));
            MainDispatcher.Invoke(delegate
            {
                string fuck = "of " + (_PageCount+1).ToString();
                Pages_PageCount.Text = fuck;
                LoadPage(0);
            });

            try
            {
                //Load the redo
                worker.ReportProgress(0,"Loading redo states");
                var ReDoData = MySQL.SelectDataDictionary("SELECT * FROM whldata.image_redo");
                ReDoBools = new Dictionary<string, bool>();
                foreach (var redoitem in ReDoData)
                {
                    ReDoBools.Add((string)redoitem["filename"],Convert.ToBoolean(redoitem["redo"]));
                }
            }
            catch (Exception ex)
            {
                //
            }

            try
            {
                worker.ReportProgress(0, "Loading images needed data");
                var NeedsImgData = MySQL.SelectDataDictionary("SELECT * FROM whldata.image_needed");
                NeedsImgBools = new Dictionary<string, bool>();
                foreach (var neededItem in NeedsImgData)
                {
                    NeedsImgBools.Add((string)neededItem["Sku"], Convert.ToBoolean(neededItem["Needed"]));
                }
            }
            catch (Exception ex)
            {
                //
            }


            worker.ReportProgress(0, "Finishing Up");
            _isLoaded = true;
            IgnoreSelection = false;
        }


        internal void LoadPage(int pageNo)
        {
            _CurrentPage = pageNo;
            Pages_CurrentPage.Text = (pageNo+1).ToString();
            try
            {
                List<WhlSKU> searchresults = Data_SkusMixdown.Skip(_CurrentPage * _PageitemCount).Take(_PageitemCount).ToList();
                List<GridSku> gridcontentList = new List<GridSku>();
                foreach (WhlSKU Sku in searchresults)
                {
                    if (!Sku.isBundle)
                    {
                        GridSku NewGS = new GridSku(Sku, this);
                        gridcontentList.Add(NewGS);
                        if (searchresults.Count < 50)
                        {
                            NewGS.LoadChildrenAsync();
                        }

                    }
                }
                ItemGrid.ItemsSource = gridcontentList;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ItemGrid.ItemsSource = new List<GridSku>();
            }
            
            
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
                DropZoneRegion.BringIntoView();
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



        private void ItemGrid_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
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
            (new AddToPacksizes()).Show();
        }

        private void RibbonShowHide_Click(object sender, RoutedEventArgs e)
        {
            if (FilmstripShowHide.IsChecked.Value)
            {
                FilmstripGrid.Height = Double.NaN;
            }
            else
            {
                FilmstripGrid.Height = 27;
            }

        }

        private bool IgnoreSelection = true;
        private void ItemGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)  
        {
            if (!IgnoreSelection && LoadNextPrevToggle.IsChecked.Value)
            {
                IgnoreSelection = true;
                var SelectedIndex = ItemGrid.SelectedIndex;
                ItemGrid.SelectedItems.Clear();         // remove the current selection then readd the one they selected plus the ones either side.
                ItemGrid.SelectedItems.Add(ItemGrid.Items[SelectedIndex]);
                try
                {
                    ItemGrid.SelectedItems.Add(ItemGrid.Items[SelectedIndex-1]);
                }
                catch (ArgumentOutOfRangeException a)
                {
                    //Noithing to do, just at the end.
                }
                try
                {
                    ItemGrid.SelectedItems.Add(ItemGrid.Items[SelectedIndex+1]);
                }
                catch (ArgumentOutOfRangeException a)
                {
                    //Noithing to do, just at the end.
                }
                //Bring it into view because changing the seleciton will have screwed the view up.
                ItemGrid.ScrollIntoView(ItemGrid.SelectedItem);
                IgnoreSelection = false;
            }
        }

        private void DropZoneRegion_Drop(object sender, DragEventArgs e)
        {
            //SOEMTHING DROPPED
            if (e.Data.GetDataPresent(typeof(FileInfo)))
            {
                //thank mr c for this stupid line
                AddFilmstripItem(e.Data.GetData(typeof(FileInfo)) as FileInfo);
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

        private void DropZoneRegion_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FileDialog.ShowDialog();
            foreach (string file in FileDialog.FileNames)
            {
                try
                {
                    AddFilmstripItem(new FileInfo(file));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
                
            }
        }
        
        #region "aux data"

        internal void UpdateRedo(string Filename, bool NewValue)
        {
            //Save ht eupdate to the lcoal dict
            ReDoBools[Filename] = NewValue;
            //Then save it in the database.
            var asd = MySQL.InsertUpdate("REPLACE INTO whldata.image_redo (filename, redo) VALUES ('" +
                               Filename.Replace("\\", "\\\\") + "','" + NewValue.ToString() + "');");
        }

        internal void UpdateNeeded(string Sku, bool NewValue)
        {
            //Save ht eupdate to the lcoal dict
            NeedsImgBools[Sku] = NewValue;
            //Then save it in the database.
            var asd = MySQL.InsertUpdate("REPLACE INTO whldata.image_needed (Sku, Needed) VALUES ('" +
                               Sku + "','" + NewValue.ToString() + "');");
        }


        internal bool RedoState(string filename)
        {
            if (ReDoBools.Keys.Contains(filename))
            {
                return ReDoBools[filename];
            }
            else
            {
                return false;
            }
        }

        internal bool NeededState(string sku)
        {
            if (NeedsImgBools.Keys.Contains(sku))
            {
                return NeedsImgBools[sku];
            }
            else
            {
                return false;
            }
        }

        private void ExportNeededButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the datas
                var ExportData = MySQL.SelectDataDictionary("SELECT Sku FROM whldata.image_needed WHERE Needed='True';");
                //create an empty eport string
                string ExportString = "Sku";
                //And loop through the rows.
                foreach (var row in ExportData)
                {
                    ExportString += Environment.NewLine + row["sku"];
                }
                //Done. NOw save it. 
                SaveDialog.ShowDialog();
                var fs = new System.IO.StreamWriter(SaveDialog.FileName);
                fs.Write(ExportString);
                fs.Close();
                MessageBox.Show("The CSV has been saved ");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show("The file was unable to be saved. This might help: " + exception.Message);
            }


        }

        #endregion

        #region "bonus functionality"

        private void Pages_Previous_Click(object sender, RoutedEventArgs e)
        {
            LoadPage(_CurrentPage - 1);
        }

        private void Pages_Next_Click(object sender, RoutedEventArgs e)
        {
            LoadPage(_CurrentPage + 1);
        }

        private void Pages_CurrentPage_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                LoadPage(Convert.ToInt32(Pages_CurrentPage.Text) - 1);
            }
            catch (Exception ex)
            {

            }
        }


        private void Main_Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Add)
            {
                e.Handled = true;
                try
                {
                    ItemGrid.SelectedIndex = ItemGrid.SelectedIndex + 1;
                    //ItemGrid.ScrollIntoView(ItemGrid.SelectedItem);
                }
                catch (Exception ex)
                {
                    //Do nothing. We tried.png.
                }
            }else if(e.Key == Key.Subtract){
                e.Handled = true;
                try
                {
                    ItemGrid.SelectedIndex = ItemGrid.SelectedIndex - 1;
                    //ItemGrid.ScrollIntoView(ItemGrid.SelectedItem);
                }
                catch (Exception ex)
                {
                    //Do nothing. We tried.png.
                }
                
            }
        }

        private void Pages_CurrentPage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Pages_Next_Click(null,null);
            }else if (e.Delta < 0)
            {
                Pages_Previous_Click(null, null);
            }
        }


        #endregion

        private void ExportRedoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Get the datas
                var ExportData = MySQL.SelectDataDictionary("SELECT filename FROM whldata.image_redo WHERE redo='True';");
                //create an empty export string
                string ExportString = "Image";
                //And loop through the rows.
                foreach (var Row in ExportData)
                {
                    ExportString += Environment.NewLine + Row["filename"];
                }
                //Done. NOw save it. 
                SaveDialog.ShowDialog();
                var fs = new System.IO.StreamWriter(SaveDialog.FileName);
                fs.Write(ExportString);
                fs.Close();
                MessageBox.Show("The CSV has been saved ");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show("The file was unable to be saved. This might help: " + exception.Message);
            }
        }

        private void LoadNeededsButton_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("This will take a few seconds to load. Click OK and wait for the items to load.");
            List<GridSku> gridcontentList = new List<GridSku>();
        

            var results = NeedsImgBools.Where(delegate(KeyValuePair<string, bool> pair) { return pair.Value == true; });
            var shorts = new List<string>();
            foreach (KeyValuePair<string, bool> value in results)
            {
                if (!shorts.Contains(value.Key.Substring(0,7)))
                {
                    WhlSKU sku = Data_SkusMixdown.SearchSKUSReturningSingleSku(value.Key.Substring(0, 7) + "0001");
                    if (!sku.isBundle)
                    {
                        shorts.Add(value.Key.Substring(0, 7));
                        GridSku NewGS = new GridSku(sku, this);
                        gridcontentList.Add(NewGS);
                        if (results.Count() < 50)
                        {
                            NewGS.LoadChildrenAsync();
                        }
                    }
                    
                }
            }
            ItemGrid.ItemsSource = gridcontentList;
        }
    }
}
