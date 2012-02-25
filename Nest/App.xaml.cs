using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using Library;
using System.IO;

namespace Nest
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static Dictionary<string, string> DirectoryPaths
        {
            get;

            private set;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            App.DirectoryPaths = new Dictionary<string, string>();
            App.DirectoryPaths["Base"] = @"..\";
            App.DirectoryPaths["Core"] = Directory.GetCurrentDirectory();
            App.DirectoryPaths["Configuration"] = Path.Combine(App.DirectoryPaths["Base"], "Configuration");
            App.DirectoryPaths["Update"] = Path.Combine(App.DirectoryPaths["Base"], "Update");
            App.DirectoryPaths["Log"] = Path.Combine(App.DirectoryPaths["Base"], "Log");
            App.DirectoryPaths["Icons"] = Path.Combine(App.DirectoryPaths["Core"], "Icons");
            App.DirectoryPaths["Languages"] = Path.Combine(App.DirectoryPaths["Core"], "Languages");
            App.DirectoryPaths["Temp"] = Path.Combine(App.DirectoryPaths["Core"], "Temp");
            App.DirectoryPaths["Session"] = Path.Combine(App.DirectoryPaths["Configuration"], "Nest", "Session");

            foreach (var item in App.DirectoryPaths.Values)
            {
                if (!Directory.Exists(item))
                {
                    Directory.CreateDirectory(item);
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
    }
}
