using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace GeoTourney.Windows
{
    static class Program
    {
        private const string AppsettingsPath = "appsettings.json";
        static Browser? browser;
        internal static Page? page;
        internal static IConfiguration? config;

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

            var mainForm = new Form1();

            void threadExceptionHandler(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                Console.WriteLine(e);
                Application.ExitThread();
            }

            async void startupHandler(object? s, EventArgs e)
            {
                // WindowsFormsSynchronizationContext is already set here
                Application.Idle -= startupHandler;

                try
                {
                    await StartUp();
                }
                catch (Exception)
                {
                }
            };

            Application.ThreadException += threadExceptionHandler;
            Application.Idle += startupHandler;
            try
            {
                Application.Run(mainForm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Application.Idle -= startupHandler;
                Application.ThreadException -= threadExceptionHandler;
                try
                {
                    page?.CloseAsync().Wait();
                    browser?.CloseAsync().Wait();
                }
                catch (Exception) { }
            }
        }

        static async Task StartUp()
        {
            await BrowserSetup.Initiate();
            browser = await Puppeteer.LaunchAsync(BrowserSetup.LaunchOptions);
            page = await browser.NewPageAsync();
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(AppsettingsPath, reloadOnChange: true, optional: false).Build();
            await Startup.InitiateAsync(config, page);
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
