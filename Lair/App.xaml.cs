using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using Library;
using System.IO;
using System.Reflection;

namespace Lair
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static Version LairVersion { get; private set; }
        public static Dictionary<string, string> DirectoryPaths { get; private set; }
        private FileStream _lockStream = null;

        public App()
        {
            App.LairVersion = new Version(0, 0, 0);

            App.DirectoryPaths = new Dictionary<string, string>();
            App.DirectoryPaths["Base"] = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            App.DirectoryPaths["Core"] = Path.Combine(App.DirectoryPaths["Base"], "Core");
            Directory.SetCurrentDirectory(App.DirectoryPaths["Core"]);

            App.DirectoryPaths["Configuration"] = Path.Combine(App.DirectoryPaths["Base"], "Configuration");
            App.DirectoryPaths["Update"] = Path.Combine(App.DirectoryPaths["Base"], "Update");
            App.DirectoryPaths["Log"] = Path.Combine(App.DirectoryPaths["Base"], "Log");
            App.DirectoryPaths["Icons"] = Path.Combine(App.DirectoryPaths["Core"], "Icons");
            App.DirectoryPaths["Languages"] = Path.Combine(App.DirectoryPaths["Core"], "Languages");
            App.DirectoryPaths["Session"] = Path.Combine(App.DirectoryPaths["Configuration"], "Lair", "Session");

            foreach (var item in App.DirectoryPaths.Values)
            {
                try
                {
                    if (!Directory.Exists(item))
                    {
                        Directory.CreateDirectory(item);
                    }
                }
                catch (Exception)
                {

                }
            }

            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(App_UnhandledException);
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception == null)
            {
                return;
            }

            Log.Error(exception);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _lockStream = new FileStream(Path.Combine(App.DirectoryPaths["Configuration"], "Amoeba.lock"), FileMode.Create);
            }
            catch (IOException)
            {
                this.Shutdown();

                return;
            }

            this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_lockStream != null)
            {
                _lockStream.Close();
                _lockStream = null;
            }
        }
    }
}
