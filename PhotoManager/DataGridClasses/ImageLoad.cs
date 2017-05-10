using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WHLClasses;

namespace PhotoManager.DataGridClassess
{
    static class Methods
    {

        // Thanks to Ted from this articale for the code which inspired the following block eamning that I have to load the file into memory then feed it to the BitmapImage because BitmapImage objects lock the source!
        // Source: http://tedshelloworld.blogspot.co.uk/2010/11/bitmaps-in-wpf-disposing-of-bitmaps-in.html
        // This attribution has also been used in Framework which is where the function fo rthis resides for ez file loading.
        internal static BitmapImage MakeImage(int? size, ref BitmapImage SourceImage, FileInfo SourceFileInfo )
        {
            SourceImage = new BitmapImage();
            SourceImage.BeginInit();
            if (size.HasValue)
            {
                SourceImage.DecodePixelWidth = size.Value;
                SourceImage.DecodePixelHeight = size.Value;
            }
            
            SourceImage.CacheOption = BitmapCacheOption.OnDemand;
            SourceImage.StreamSource = WHLClasses.MiscFunctions.Misc.LoadFileStreamToMemory(SourceFileInfo.FullName);
            SourceImage.EndInit();
            return SourceImage;
        }
    }
}
