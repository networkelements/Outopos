using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Lair.Properties;
using Library;
using Library.Io;
using Library.Net.Lair;
using Library.Net.Connection;
using Library.Net.Proxy;
using Library.Net.Upnp;
using Library.Security;
using System.ComponentModel;

namespace Lair.Windows
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MainWindow : Window
    {
        private BufferManager _bufferManager;
        private LairManager _lairManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;

        System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath = null;

        public MainWindow()
        {
            _bufferManager = new BufferManager();

            this.Setting_Log();

            _configrationDirectoryPaths.Add("MainWindow", Path.Combine(App.DirectoryPaths["Configuration"], @"Lair/Properties/Settings"));
            _configrationDirectoryPaths.Add("AutoBaseNodeSettingManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Lair/AutoBaseNodeSettingManager"));
            _configrationDirectoryPaths.Add("LairManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Library/Net/Lair/LairManager"));

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

            Log.LogEvent += new LogEventHandler((object sender, LogEventArgs e) =>
            {
                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            _logParagraph.Inlines.Add(string.Format("{0} {1}:\t{2}\r\n", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.MessageLevel, e.Message));

                            if (_logParagraph.Inlines.Count > 100)
                            {
                                _logParagraph.Inlines.Remove(_logParagraph.Inlines.FirstInline);
                            }

                            _logRichTextBox.ScrollToEnd();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
            });

            Debug.Listeners.Add(new MyTraceListener(this));
        }

        private class MyTraceListener : TraceListener
        {
            MainWindow _mainWindow;

            public MyTraceListener(MainWindow mainWindow)
            {
                _mainWindow = mainWindow;
            }

            public override void Write(string message)
            {
                this.WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                try
                {
                    _mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            _mainWindow._logParagraph.Inlines.Add(string.Format("{0} Debug:\t{1}\r\n", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), message));

                            if (_mainWindow._logParagraph.Inlines.Count > 100)
                            {
                                _mainWindow._logParagraph.Inlines.Remove(_mainWindow._logParagraph.Inlines.FirstInline);
                            }

                            _mainWindow._logRichTextBox.ScrollToEnd();
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
            }
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
            NativeMethods.SetThreadExecutionState(ExecutionState.SystemRequired | ExecutionState.Continuous);

            {
                bool initFlag = false;

                _lairManager = new LairManager(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.blocks"), _bufferManager);
                _lairManager.Load(_configrationDirectoryPaths["LairManager"]);

                if (_lairManager.BaseNode == null || _lairManager.BaseNode.Id == null)
                {
                    byte[] buffer = new byte[64];
                    (new RNGCryptoServiceProvider()).GetBytes(buffer);

                    var baseNode = new Node();
                    baseNode.Id = buffer;

                    _lairManager.BaseNode = baseNode;
                }

                if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version")))
                {
                    initFlag = true;

                    Settings.Instance.ChannelControl_Category.Boards.Add(new Board() { Channel = LairConverter.FromChannelString("Channel@AAAAAEAAnsVLtnbRdm22in0qQhQ8Smdqc9Yro90PXOiF3lkIF2RREEbGiQmRmpukx0tPytNON6UiN35jYs3xCSNoxmmnhwAAAAYBQW1vZWJhhggnHQ==") });
                    Settings.Instance.ChannelControl_Category.Boards.Add(new Board() { Channel = LairConverter.FromChannelString("Channel@AAAAAEAACeKcl3zp_ff2hFIhIHWjxxEt1tBcqZvmt4X9a1t3ilY7rUXHbWLPJ6rwu1sLvbHB0KhP81b1J7XvVUV_wi991gAAAAQBTGFpcl1EKAI=") });
                    Settings.Instance.ChannelControl_Category.Boards.Add(new Board() { Channel = LairConverter.FromChannelString("Channel@AAAAAEAAtsMEuKHe-iQje0OeLoHfbSLvKXFo4zv7xP7j2IW1ZK_st0yVK4kO7Hcl9SDnqNTSxAvShddBiOg_2q6tSLsMTwAAAAYBUHVibGljMXNfWw==") });
                    Settings.Instance.ChannelControl_Category.Boards.Add(new Board() { Channel = LairConverter.FromChannelString("Channel@AAAAAEAASaqqritqZRVYlzfkcVhMAT0tm8PTp3Vu_KWBUEloBtnZ_WnRQO5pcEq60gQOktBz5qFxg_Saiqo6lwtDP-bIdgAAAAQBVGVzdITgoiw=") });

                    _lairManager.ConnectionCountLimit = 12;
                    _lairManager.DownloadingConnectionCountLowerLimit = 3;
                    _lairManager.UploadingConnectionCountLowerLimit = 3;

                    Random random = new Random();
                    _lairManager.ListenUris.Clear();
                    _lairManager.ListenUris.Add(string.Format("tcp:{0}:{1}", IPAddress.Any.ToString(), random.Next(1024, 65536)));
                    _lairManager.ListenUris.Add(string.Format("tcp:[{0}]:{1}", IPAddress.IPv6Any.ToString(), random.Next(1024, 65536)));

                    var ipv4ConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.Tcp,
                        UriCondition = new UriCondition()
                        {
                            Value = @"tcp:([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3}).*",
                        },
                    };

                    var ipv6ConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.Tcp,
                        UriCondition = new UriCondition()
                        {
                            Value = @"tcp:\[(\d|:)*\].*",
                        },
                    };

                    var tcpConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.Tcp,
                        UriCondition = new UriCondition()
                        {
                            Value = @"tcp:.*",
                        },
                    };

                    var torConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.Socks5Proxy,
                        ProxyUri = "tcp:127.0.0.1:9050",
                        UriCondition = new UriCondition()
                        {
                            Value = @"tor:.*",
                        },
                    };

                    var i2pConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.None,
                        UriCondition = new UriCondition()
                        {
                            Value = @"i2p:.*",
                        },
                    };

                    _lairManager.Filters.Clear();
                    _lairManager.Filters.Add(ipv4ConnectionFilter);
                    _lairManager.Filters.Add(ipv6ConnectionFilter);
                    _lairManager.Filters.Add(tcpConnectionFilter);
                    _lairManager.Filters.Add(torConnectionFilter);
                    _lairManager.Filters.Add(i2pConnectionFilter);
                }
                else
                {
                    Version version;

                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
                    }
                }

                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version"), false, new UTF8Encoding(false)))
                {
                    writer.WriteLine(App.LairVersion.ToString());
                }

#if DEBUG
                if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Debug_NodeId.txt")))
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Debug_NodeId.txt"), new UTF8Encoding(false)))
                    {
                        var baseNode = new Node();
                        byte[] buffer = new byte[64];

                        byte b = byte.Parse(reader.ReadLine());

                        for (int i = 0; i < 64; i++)
                        {
                            buffer[i] = b;
                        }

                        baseNode.Id = buffer;
                        baseNode.Uris.AddRange(_lairManager.BaseNode.Uris);

                        _lairManager.BaseNode = baseNode;
                    }
                }
#endif

                if (string.IsNullOrWhiteSpace(Settings.Instance.Global_Amoeba_Path))
                {
                    foreach (var p in Process.GetProcesses())
                    {
                        try
                        {
                            var path = p.MainModule.FileName;

                            if (Path.GetFileName(path) == "Amoeba.exe")
                            {
                                Settings.Instance.Global_Amoeba_Path = path;
                                
                                break;
                            }
                        }
                        catch (Win32Exception)
                        {

                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                _autoBaseNodeSettingManager = new AutoBaseNodeSettingManager(_lairManager);
                _autoBaseNodeSettingManager.Load(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);

                if (initFlag)
                {
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _lairManager.Save(_configrationDirectoryPaths["LairManager"]);
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                }
            }
        }

        private object _updateLockObject = new object();

        private void UpdateCheck(bool isShow)
        {
            lock (_updateLockObject)
            {
                try
                {
                    Version updateVersion = new Version();

                    if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.update")))
                    {
                        using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.update"), new UTF8Encoding(false)))
                        {
                            updateVersion = new Version(reader.ReadLine());
                        }
                    }

                    var url = Settings.Instance.Global_Update_Url;
                    var proxyUri = Settings.Instance.Global_Update_ProxyUri;
                    var signature = Settings.Instance.Global_Update_Signature;
                    WebProxy proxy = null;
                    string line1;
                    string line2;

                    {
                        HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create(url);
                        rq.Method = "GET";
                        rq.ContentType = "text/html; charset=UTF-8";
                        rq.UserAgent = "";
                        rq.ReadWriteTimeout = 1000 * 60;
                        rq.Timeout = 1000 * 60;
                        rq.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                        rq.KeepAlive = true;
                        rq.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");

                        if (!string.IsNullOrWhiteSpace(proxyUri))
                        {
                            string proxyScheme = null;
                            string proxyHost = null;
                            int proxyPort = -1;

                            {
                                Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                                var match = regex.Match(proxyUri);

                                if (match.Success)
                                {
                                    proxyScheme = match.Groups[1].Value;
                                    proxyHost = match.Groups[2].Value;
                                    proxyPort = int.Parse(match.Groups[3].Value);
                                }
                                else
                                {
                                    Regex regex2 = new Regex(@"(.*?):(.*)");
                                    var match2 = regex2.Match(proxyUri);

                                    if (match2.Success)
                                    {
                                        proxyScheme = match2.Groups[1].Value;
                                        proxyHost = match2.Groups[2].Value;
                                        proxyPort = 80;
                                    }
                                }
                            }

                            proxy = new WebProxy(proxyHost, proxyPort);
                            rq.Proxy = proxy;
                        }
                     
                        using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                        using (Stream stream = rs.GetResponseStream())
                        using (StreamReader r = new StreamReader(stream))
                        {
                            line1 = r.ReadLine();
                            line2 = r.ReadLine();
                        }
                    }

                    Regex regex3 = new Regex(@"Lair ((\d*)\.(\d*)\.(\d*)).*");
                    var match3 = regex3.Match(line1);

                    if (match3.Success)
                    {
                        var tempVersion = new Version(match3.Groups[1].Value);

                        if (tempVersion <= App.LairVersion)
                        {
                            if (!isShow) return;

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                MessageBox.Show(
                                    this,
                                    LanguagesManager.Instance.MainWindow_LatestVersion_Message,
                                    "Update",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }), null);
                        }
                        else
                        {
                            if (!isShow && tempVersion <= updateVersion) return;

                            bool flag = true;

                            if (Settings.Instance.Global_Update_Option != UpdateOption.AutoUpdate)
                            {
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                {
                                    if (MessageBox.Show(
                                        this,
                                        string.Format(LanguagesManager.Instance.MainWindow_UpdateCheck_Message, line1),
                                        "Update",
                                        MessageBoxButton.OKCancel,
                                        MessageBoxImage.Information) == MessageBoxResult.Cancel)
                                    {
                                        flag = false;
                                    }
                                }), null);
                            }

                            using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.update"), false, new UTF8Encoding(false)))
                            {
                                writer.WriteLine(tempVersion.ToString());
                            }

                            if (flag)
                            {
                                HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create(line2);
                                rq.Method = "GET";
                                rq.ContentType = "text/html; charset=UTF-8";
                                rq.UserAgent = "";
                                rq.ReadWriteTimeout = 1000 * 60;
                                rq.Timeout = 1000 * 60;
                                rq.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                                rq.KeepAlive = true;
                                rq.Proxy = proxy;
                                rq.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");

                                using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                                {
                                    string fileName = null;

                                    if (rs.Headers.AllKeys.Contains("Content-Disposition"))
                                    {
                                        string dispos = rs.Headers["Content-Disposition"];

                                        if (!String.IsNullOrEmpty(dispos))
                                        {
                                            Regex re = new Regex(@"
                                            filename\s*=\s*
                                            (?:
                                              ""(?<filename>[^""]*)""
                                              |
                                              (?<filename>[^;]*)
                                            )
                                            ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

                                            Match m = re.Match(dispos);

                                            if (m.Success)
                                            {
                                                fileName = m.Groups["filename"].Value;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fileName = Path.GetFileName(line2);
                                    }

                                    using (Stream inStream = rs.GetResponseStream())
                                    using (FileStream outStream = new FileStream(string.Format(@"{0}\{1}", App.DirectoryPaths["Update"], fileName), FileMode.Create))
                                    {
                                        byte[] buffer = new byte[1024 * 4];

                                        int length = 0;

                                        while (0 < (length = inStream.Read(buffer, 0, buffer.Length)))
                                        {
                                            outStream.Write(buffer, 0, length);
                                        }
                                    }

                                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                    {
                                        MessageBox.Show(
                                            this,
                                            LanguagesManager.Instance.MainWindow_Restart_Message,
                                            "Update",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Information);
                                    }), null);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private void ConnectionsInformationShow(object state)
        {
            long sentByteCount = 0;
            long receivedByteCount = 0;
            List<long> sentByteCountList = new List<long>(new long[3]);
            List<long> receivedByteCountList = new List<long>(new long[3]);
            int count = 0;

            for (; ; )
            {
                Thread.Sleep(1000);

                try
                {
                    sentByteCountList[count] = _lairManager.SentByteCount - sentByteCount;
                    sentByteCount = _lairManager.SentByteCount;
                    receivedByteCountList[count] = _lairManager.ReceivedByteCount - receivedByteCount;
                    receivedByteCount = _lairManager.ReceivedByteCount;
                    count++;
                    if (count >= sentByteCountList.Count) count = 0;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            _sendSpeedTextBlock.Text = NetworkConverter.ToSizeString(sentByteCountList.Sum(n => n) / 3) + "/s";
                            _receiveSpeedTextBlock.Text = NetworkConverter.ToSizeString(receivedByteCountList.Sum(n => n) / 3) + "/s";
                        }
                        catch (Exception)
                        {

                        }
                    }), null);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        try
                        {
                            if (_lairManager.State == ManagerState.Start)
                            {
                                _stateTextBlock.Text = "Start";
                            }
                            else
                            {
                                _stateTextBlock.Text = "Stop";
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }), null);
                }
                catch (Exception)
                {

                }
            }
        }

        private void Timer(object state)
        {
            Stopwatch spaceCheckStopwatch = new Stopwatch();
            Stopwatch backupStopwatch = new Stopwatch();
            Stopwatch updateStopwatch = new Stopwatch();
            spaceCheckStopwatch.Start();
            backupStopwatch.Start();

            for (; ; )
            {
                Thread.Sleep(1000);

                if (spaceCheckStopwatch.Elapsed > new TimeSpan(0, 1, 0))
                {
                    spaceCheckStopwatch.Restart();

                    try
                    {
                        DriveInfo drive = new DriveInfo(Directory.GetCurrentDirectory());

                        if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                        {
                            if (_lairManager.State == ManagerState.Start)
                            {
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                {
                                    _menuItemStop_Click(null, null);
                                }), null);

                                Log.Warning(LanguagesManager.Instance.MainWindow_SpaceNotFound);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                if (backupStopwatch.Elapsed > new TimeSpan(0, 5, 0))
                {
                    backupStopwatch.Restart();

                    try
                    {
                        _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                        _lairManager.Save(_configrationDirectoryPaths["LairManager"]);
                        Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                    }
                    catch (Exception)
                    {

                    }
                }


                if (!updateStopwatch.IsRunning && updateStopwatch.Elapsed > new TimeSpan(3, 0, 0, 0))
                {
                    updateStopwatch.Restart();

                    try
                    {
                        if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
                           || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
                        {
                            _menuItemUpdateCheck_Click(null, null);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
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

            ConnectionControl _connectionControl = new ConnectionControl(_lairManager);
            _connectionControl.Height = Double.NaN;
            _connectionControl.Width = Double.NaN;
            _connectionTabItem.Content = _connectionControl;

            ChannelControl _channelControl = new ChannelControl(this, _lairManager, _bufferManager);
            _channelControl.Height = Double.NaN;
            _channelControl.Width = Double.NaN;
            _channelTabItem.Content = _channelControl;

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ConnectionsInformationShow), this);
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.Timer), this);

            if (Settings.Instance.Global_IsStart)
            {
                _menuItemStart_Click(null, null);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
            {
                Thread.Sleep(1000 * 60);

                try
                {
                    if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
                       || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
                    {
                        _menuItemUpdateCheck_Click(null, null);
                    }
                }
                catch (Exception)
                {

                }
            }));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);

            _notifyIcon.Visible = false;

            _autoBaseNodeSettingManager.Stop();
            _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
            _autoBaseNodeSettingManager.Dispose();

            _lairManager.Stop();
            _lairManager.Save(_configrationDirectoryPaths["LairManager"]);
            _lairManager.Dispose();

            Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
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

            _logRichTextBox.ScrollToEnd();
        }

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabItem = _tabControl.SelectedItem as TabItem;

            if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Connection)
            {
                App.SelectTab = "Connection";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Channel)
            {
                App.SelectTab = "Channel";
            }
            else if ((string)tabItem.Header == LanguagesManager.Instance.MainWindow_Log)
            {
                App.SelectTab = "Log";
            }
            else
            {
                App.SelectTab = "";
            }

            _logRichTextBox.ScrollToEnd();
            this.Title = string.Format("Lair {0}", App.LairVersion);
        }

        private void _menuItemStart_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
            {
                if (Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled)
                {
                    _autoBaseNodeSettingManager.Start();
                }

                _lairManager.Start();
            }));

            _menuItemStart.IsEnabled = false;
            _menuItemStop.IsEnabled = true;

            Settings.Instance.Global_IsStart = true;
            Log.Information("Start");
        }

        private void _menuItemStop_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
            {
                _autoBaseNodeSettingManager.Stop();
                _lairManager.Stop();
            }));

            _menuItemStart.IsEnabled = true;
            _menuItemStop.IsEnabled = false;

            Settings.Instance.Global_IsStart = false;
            Log.Information("Stop");
        }

        private void _menuItemConnectionSetting_Click(object sender, RoutedEventArgs e)
        {
            ConnectionWindow window = new ConnectionWindow(_lairManager, _autoBaseNodeSettingManager, _bufferManager);
            window.Owner = this;
            window.ShowDialog();
        }

        private void _menuItemUserInterfaceSetting_Click(object sender, RoutedEventArgs e)
        {
            UserInterfaceWindow window = new UserInterfaceWindow(_bufferManager);
            window.Owner = this;
            window.ShowDialog();
        }

        private void _menuItemUpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                this.UpdateCheck(sender != null);
            }));
        }

        private void _menuItemDeveloperSiteCheck_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://lyrise.i2p.to/projects/trac/");
        }

        private void _menuItemManualSiteCheck_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://lyrise.web.fc2.com/");
        }

        private void _menuItemVersionInformation_Click(object sender, RoutedEventArgs e)
        {
            VersionInformationWindow window = new VersionInformationWindow();
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
