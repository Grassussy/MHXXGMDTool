using System;
using System.Windows.Forms;

namespace MHXXGMDTool
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Bluegrams.Application.PortableSettingsProvider.SettingsFileName = "Settings.xml";
            Bluegrams.Application.PortableSettingsProvider.ApplyProvider(Properties.Settings.Default);
            Application.Run(new Editor());
        }
    }
}
