using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoManager.Controls
{
    internal static class Constants_ish
    {
        internal static string FilmstripSaveDataLocation()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create) + "\\PhotoManager\\FilmStrip.save";
        }
    }
}
