using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WHLClasses;

namespace PhotoManager.DataGridClasses
{
    public class GridSku
    {
        
        public string Sku { get;  set;}
        public string Title { get; set; }

        public WhlSKU Data { get; set; }
        public MainWindow Mainwindow { get; set; }

        private SkuCollection children { get; set; }


        public GridSku(WhlSKU sku, MainWindow mainwindow)
        {
            //Keep the mainwindowref
            this.Mainwindow = mainwindow;
            this.Sku = sku.SKU;
            this.Title = sku.Title.Invoice;
            this.Data = sku;
        }

        public GridSku()
        {
            
        }

        private ThreadStart childrenLoader = null;
        private Thread childLoadThread = null;

        internal void LoadChildrenAsync()
        {
            childrenLoader = new ThreadStart(LoadChildren);
            childLoadThread = new Thread(childrenLoader);
            childLoadThread.IsBackground = true;
            childLoadThread.Start();
        }

        internal void LoadChildren()
        {
            //Do it in the foregroud
            children = Mainwindow.Data_Skus.GatherChildren(Data.ShortSku);
        }

        public SkuCollection GetChildren()
        {
            if (children == null)
            {
                LoadChildren();
            }
            return children;
        }
        
    }
}
