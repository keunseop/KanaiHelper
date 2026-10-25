using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace KanaiHelper
{
    public static class Program
    {
        private const string MutexName = "Global\\KanaiHelper_v1_Mutex";

        [STAThread]
        public static void Main(string[] args)
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    IntPtr existingWnd = NativeMethods.FindWindow(null, "KanaiHelper v1.0");
                    if (existingWnd != IntPtr.Zero)
                    {
                        // Restore and bring to front
                        NativeMethods.ShowWindow(existingWnd, 9); // SW_RESTORE
                        NativeMethods.SetForegroundWindow(existingWnd);
                    }
                    return;
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string iniPath = Path.Combine(baseDir, "Hotkeys.ini");

                AppConfig config = new AppConfig(iniPath);
                MacroEngine engine = new MacroEngine(config);
                GlobalHook hook = new GlobalHook(config, engine);

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;

                MainWindow window = new MainWindow(config, engine, hook);
                app.Run(window);
            }
        }
    }
}


