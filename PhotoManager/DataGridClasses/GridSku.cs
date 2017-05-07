using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WHLClasses;

namespace PhotoManager.DataGridClasses
{
    class GridSku
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

        internal void LoadChildrenAsync()
        {
            //Do it in the background

            children = Mainwindow.Data_Skus.GatherChildren(Data.ShortSku);
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
                GetChildren();
            }
            return children;
        }
        
    }
}
