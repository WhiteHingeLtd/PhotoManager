﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        internal SkuCollection Data_Skus = null;
        internal EmployeeCollection Data_Employees = null;
        internal AuthClass Data_User = null;
        internal SkuCollection Data_SkusMixdown = null;


        public MainWindow()
        {
            InitializeComponent();
            Photos = (new System.IO.DirectoryInfo("T:\\Photos\\Amazon")).EnumerateFiles("*.jpg").ToList();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.6);
            timer.Tick += TimerTick;
            timer.Start();

        }

        public DispatcherTimer timer = null;
        public List<FileInfo> Photos = new List<FileInfo>();
        private GridSku CurrentInstance = null;
        private int Presses = 0;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            Presses += 1;
            FilmStripFrame test = new FilmStripFrame(Photos[Presses],this, true);
            Filmstrip.Children.Add(test);
            test.BringIntoView();
            fstb.Text = Presses.ToString();
        }

        private void TimerTick(object sender, object e)
        {
            if (autocheckbox.IsChecked.Value) { button_Click(null,null);}
        }

        private void Main_Window_Initialized(object sender, EventArgs e)
        {
            Splash newSplash = new Splash{HomeRef = this};
            newSplash.InitializeComponent();
            newSplash.ShowDialog();
        }

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
                if (searchresults.Count < 5){NewGS.LoadChildrenAsync();}
                

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
            }
            catch (Exception a)
            {

            }
        }
    }
}
