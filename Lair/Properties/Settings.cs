using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Library;

namespace Lair.Properties
{
    internal class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static Settings _defaultInstance = new Settings();
        private object _thisLock = new object();

        public Settings()
            : base(new List<Library.Configuration.ISettingsContext>()
            {
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UseLanguage", Value = "English" },
                
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Normal },
                      
                new Library.Configuration.SettingsContext<double>() { Name = "RouterWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "RouterWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "RouterWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "RouterWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "RouterWindow_WindowState", Value = WindowState.Normal },               
            })
        {

        }

        public override void Load(string directoryPath)
        {
            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                try
                {
                    base.Load(directoryPath);
                }
                catch (Exception)
                {
                }
            }
        }

        public override void Save(string directoryPath)
        {
            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                base.Save(directoryPath);
            }
        }

        public static Settings Instance
        {
            get
            {
                return _defaultInstance;
            }
        }
        
        #region Property

        public string Global_UseLanguage
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["Global_UseLanguage"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UseLanguage"] = value;
                }
            }
        }


        public double MainWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MainWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Top"] = value;
                }
            }
        }

        public double MainWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MainWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Left"] = value;
                }
            }
        }

        public double MainWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MainWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Height"] = value;
                }
            }
        }

        public double MainWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MainWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Width"] = value;
                }
            }
        }

        public WindowState MainWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["MainWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_WindowState"] = value;
                }
            }
        }


        public double RouterWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["RouterWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["RouterWindow_Top"] = value;
                }
            }
        }

        public double RouterWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["RouterWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["RouterWindow_Left"] = value;
                }
            }
        }

        public double RouterWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["RouterWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["RouterWindow_Height"] = value;
                }
            }
        }

        public double RouterWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["RouterWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["RouterWindow_Width"] = value;
                }
            }
        }

        public WindowState RouterWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["RouterWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["RouterWindow_WindowState"] = value;
                }
            }
        }

        #endregion

        #region IThisLock メンバ

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }
}
