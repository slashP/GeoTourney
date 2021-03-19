using System;
using System.IO;
using System.Windows.Forms;
using GeoTourney.Core;

namespace GeoTourney.Config
{
    static class Program
    {
        private const string AppsettingsPath = "appsettings.json";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EnsureAppsettingsExists();

            Application.Run(new Form1());
        }

        private static void EnsureAppsettingsExists()
        {
            if (!File.Exists(AppsettingsPath))
            {
                File.WriteAllText(AppsettingsPath, EmbeddedFileHelper.Content("defaultAppsettings.json"));
            }
        }
    }
}
