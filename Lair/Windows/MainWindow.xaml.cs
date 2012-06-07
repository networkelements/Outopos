using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Library.Net.Lair;
using Library;
using System.Threading;
using Lair.Properties;
using System.Diagnostics;

namespace Lair.Windows
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    internal partial class MainWindow : Window
    {
        private BufferManager _bufferManager;

        private List<TreeViewItem> _treeViewItems = new List<TreeViewItem>();

        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath = null;

        public MainWindow()
        {
            _bufferManager = new BufferManager();

            this.Setting_Log();

            _configrationDirectoryPaths.Add("MainWindow", Path.Combine(App.DirectoryPaths["Configuration"], @"Amoeba/Properties/Settings"));

            Settings.Instance.Load(_configrationDirectoryPaths["MainWindow"]);

            InitializeComponent();

            _windowState = this.WindowState;

            this.Title = string.Format("Lair {0}", App.LairVersion);

            using (FileStream stream = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            this.Setting_Languages();

            System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"));
            _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
            _notifyIcon.Visible = true;

            this.Setting_Init();

            _notifyIcon.Visible = false;
            _notifyIcon.Click += (object sender2, EventArgs e2) =>
            {
                this.Show();
                this.Activate();
                this.WindowState = _windowState;

                _notifyIcon.Visible = false;
            };
        }

        private static string GetMachineInfomation()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            string osName = "";

            if (osInfo.Platform == PlatformID.Win32NT)
            {
                if (osInfo.Version.Major == 4)
                {
                    osName = "Windows NT 4.0";
                }
                else if (osInfo.Version.Major == 5)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows 2000";
                            break;

                        case 1:
                            osName = "Windows XP";
                            break;

                        case 2:
                            osName = "Windows Server 2003";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 6)
                {
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osName = "Windows Vista";
                            break;

                        case 1:
                            osName = "Windows 7";
                            break;
                    }
                }
            }
            else if (osInfo.Platform == PlatformID.WinCE)
            {
                osName = "Windows CE";
            }
            else if (osInfo.Platform == PlatformID.MacOSX)
            {
                osName = "MacOSX";
            }
            else if (osInfo.Platform == PlatformID.Unix)
            {
                osName = "Unix";
            }

            return string.Format(
                "Lair:\t\t{0}\r\n" +
                "OS:\t\t{1} ({2})\r\n" +
                ".NET Framework:\t{3}", App.LairVersion.ToString(3), osName, osInfo.VersionString, Environment.Version);
        }

        private void Setting_Log()
        {
            Directory.CreateDirectory(App.DirectoryPaths["Log"]);
            int logCount = 0;
            bool isHeaderWrite = true;

            if (_logPath == null)
            {
                do
                {
                    if (logCount == 0)
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.txt", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                    }
                    else
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.({1}).txt", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), logCount));
                    }

                    logCount++;
                } while (File.Exists(_logPath));
            }

            Log.LogEvent += new LogEventHandler((object sender, LogEventArgs e) =>
            {
                lock (_logPath)
                {
                    try
                    {
                        if (e.MessageLevel == LogMessageLevel.Error || e.MessageLevel == LogMessageLevel.Warning)
                        {
                            using (var writer = new StreamWriter(_logPath, true, new UTF8Encoding(false)))
                            {
                                if (isHeaderWrite)
                                {
                                    writer.WriteLine(MainWindow.GetMachineInfomation());
                                    isHeaderWrite = false;
                                }

                                writer.WriteLine(string.Format(
                                    "\r\n--------------------------------------------------------------------------------\r\n\r\n" +
                                    "Time:\t\t{0}\r\n" +
                                    "Level:\t\t{1}\r\n" +
                                    "{2}",
                                    DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        private void Setting_Languages()
        {
            foreach (var item in LanguagesManager.Instance.Languages)
            {
                var menuItem = new MenuItem() { IsCheckable = true, Header = item };

                menuItem.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) =>
                {
                    foreach (var item3 in _menuItemLanguages.Items.Cast<MenuItem>())
                    {
                        item3.IsChecked = false;
                    }

                    menuItem.IsChecked = true;
                });

                menuItem.Checked += new RoutedEventHandler((object sender, RoutedEventArgs e) =>
                {
                    Settings.Instance.Global_UseLanguage = (string)menuItem.Header;
                    LanguagesManager.ChangeLanguage((string)menuItem.Header);
                });

                _menuItemLanguages.Items.Add(menuItem);
            }

            var menuItem2 = _menuItemLanguages.Items.Cast<MenuItem>().FirstOrDefault(n => (string)n.Header == Settings.Instance.Global_UseLanguage);
            if (menuItem2 != null) menuItem2.IsChecked = true;
        }

        private void Setting_Init()
        {
            foreach (var path in Directory.GetDirectories(Path.Combine(App.DirectoryPaths["Configuration"], "Lair", "Session"))
                .OrderBy(n => int.Parse(Path.GetFileName(n))))
            {
                string name;
                RouterManager routerManager = new RouterManager(_bufferManager);
                SessionManager sessionManager = new SessionManager(_bufferManager);

                using (FileStream stream = new FileStream(Path.Combine(path, "Name.txt"), FileMode.Open))
                using (StreamReader reader = new StreamReader(stream))
                {
                    name = reader.ReadLine();
                }

                routerManager.Load(Path.Combine(path, "ServerManager"));
                sessionManager.Load(Path.Combine(path, "SessionManager"));

                _treeViewItems.Add(new SessionTreeViewItem()
                {
                    Name = name,
                    ServerManager = routerManager,
                    SessionManager = sessionManager,
                });
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            TopRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).Y;
            };

            LeftRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).X;
            };
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();

                _notifyIcon.Visible = true;
            }
            else
            {
                _windowState = this.WindowState;
            }
        }

        private void _menuItemSignatureSetting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _menuItemVersionInformation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewServerAddContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager settingsManager = new SettingsManager();
            settingsManager.AutoStart = true;
            settingsManager.Name = "Server";

            RouterManager routerManager = new RouterManager(_bufferManager);
            Random random = new Random();
            routerManager.ListenUris.Clear();
            routerManager.ListenUris.Add(string.Format("tcp:{0}:{1}", IPAddress.Any.ToString(), random.Next(1024, 65536)));
            routerManager.ListenUris.Add(string.Format("tcp:[{0}]:{1}", IPAddress.IPv6Any.ToString(), random.Next(1024, 65536)));

            RouterWindow window = new RouterWindow(ref settingsManager, ref routerManager);
            window.Owner = this;
            window.ShowDialog();

            _treeViewItems.Add(new RouterTreeViewItem()
            {
                SettingsManager = settingsManager,
                RouterManager = routerManager,
            });
        }

        public class SessionTreeViewItem : TreeViewItem
        {
            private SessionManager _sessionManager;
            private SettingsManager _settingsManager;

            public SessionTreeViewItem()
                : base()
            {
                base.IsExpanded = true;
            }

            public void Update()
            {
                base.Header = new TextBlock()
                {
                    Text = this.Name,
                };

                List<ChannelTreeViewItem> list = new List<ChannelTreeViewItem>();

                foreach (var item in this.SettingsManager.Channels)
                {
                    list.Add(new ChannelTreeViewItem()
                    {
                        Name = item
                    });
                }

                foreach (var item in this.Items.OfType<ChannelTreeViewItem>().ToArray())
                {
                    if (!list.Any(n => n.Name == item.Name))
                    {
                        this.Items.Remove(item);
                    }
                }

                foreach (var item in list)
                {
                    if (!this.Items.OfType<ChannelTreeViewItem>().Any(n => n.Name == item.Name))
                    {
                        this.Items.Add(item);
                    }
                }
            }

            public SessionManager SessionManager
            {
                get
                {
                    return _sessionManager;
                }
                set
                {
                    _sessionManager = value;

                    this.Update();
                }
            }

            public SettingsManager SettingsManager
            {
                get
                {
                    return _settingsManager;
                }
                set
                {
                    _settingsManager = value;

                    this.Update();
                }
            }

            new public string Name
            {
                get
                {
                    return _settingsManager.Name;
                }
                set
                {
                    _settingsManager.Name = value;

                    this.Update();
                }
            }
        }

        public class RouterTreeViewItem : TreeViewItem
        {
            private RouterManager _routerManager;
            private SettingsManager _settingsManager;

            public RouterTreeViewItem()
                : base()
            {
                base.IsExpanded = true;
            }

            public void Update()
            {
                base.Header = new TextBlock()
                {
                    Text = this.Name,
                };

                List<ChannelTreeViewItem> list = new List<ChannelTreeViewItem>();

                foreach (var item in this.SettingsManager.Channels)
                {
                    list.Add(new ChannelTreeViewItem()
                    {
                        Name = item
                    });
                }

                foreach (var item in this.Items.OfType<ChannelTreeViewItem>().ToArray())
                {
                    if (!list.Any(n => n.Name == item.Name))
                    {
                        this.Items.Remove(item);
                    }
                }

                foreach (var item in list)
                {
                    if (!this.Items.OfType<ChannelTreeViewItem>().Any(n => n.Name == item.Name))
                    {
                        this.Items.Add(item);
                    }
                }
            }

            public RouterManager RouterManager
            {
                get
                {
                    return _routerManager;
                }
                set
                {
                    _routerManager = value;

                    this.Update();
                }
            }

            public SettingsManager SettingsManager
            {
                get
                {
                    return _settingsManager;
                }
                set
                {
                    _settingsManager = value;

                    this.Update();
                }
            }

            new public string Name
            {
                get
                {
                    return _settingsManager.Name;
                }
                set
                {
                    _settingsManager.Name = value;

                    this.Update();
                }
            }
        }

        public class ChannelTreeViewItem : TreeViewItem
        {
            private bool _hit;
            private string _name;

            public ChannelTreeViewItem()
                : base()
            {
                base.IsExpanded = true;
            }

            public bool Hit
            {
                get
                {
                    return _hit;
                }
                set
                {
                    _hit = value;

                    this.Update();
                }
            }

            new public string Name
            {
                get
                {
                    return _name;
                }
                set
                {
                    _name = value;

                    this.Update();
                }
            }

            public void Update()
            {
                if (_hit)
                {
                    base.Header = new TextBlock()
                    {
                        Text = _name,
                        Opacity = 0,
                    };
                }
                else
                {
                    base.Header = new TextBlock()
                    {
                        Text = _name,
                        Opacity = 0.3,
                    };
                }
            }
        }
    }
}
