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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Connections;
using Library.Net.Outopos;
using Library.Net.Proxy;
using Library.Net.Upnp;
using Library.Security;
using Outopos.Properties;
using A = Library.Net.Amoeba;
using O = Library.Net.Outopos;

namespace Outopos.Windows
{
    delegate void DebugLog(string message);

    enum MainWindowTabType
    {
        World,
        Wiki,
        Chat,
        Mail,
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MainWindow : Window
    {
        private BufferManager _bufferManager;
        private OutoposManager _outoposManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;
        private OverlayNetworkManager _overlayNetworkManager;
        private CatharsisManager _catharsisManager;

        private Random _random = new Random();

        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath;

        private volatile bool _closed = false;

        private Thread _timerThread;
        private Thread _statusBarThread;
        private Thread _trafficMonitorThread;

        private volatile MainWindowTabType _selectedTab;

        [FlagsAttribute]
        enum ExecutionState : uint
        {
            Null = 0,
            SystemRequired = 1,
            DisplayRequired = 2,
            Continuous = 0x80000000,
        }

        static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);
        }

        public MainWindow()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _bufferManager = BufferManager.Instance;

                this.Setting_Log();

                _configrationDirectoryPaths.Add("MainWindow", Path.Combine(App.DirectoryPaths["Configuration"], @"Outopos/Properties/Settings"));
                _configrationDirectoryPaths.Add("OutoposManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Library/Net/Outopos/OutoposManager"));
                _configrationDirectoryPaths.Add("AutoBaseNodeSettingManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Outopos/AutoBaseNodeSettingManager"));
                _configrationDirectoryPaths.Add("OverlayNetworkManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Outopos/OverlayNetworkManager"));
                _configrationDirectoryPaths.Add("TransfarLimitManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Outopos/TransfarLimitManager"));
                _configrationDirectoryPaths.Add("CatharsisManager", Path.Combine(App.DirectoryPaths["Configuration"], @"Outopos/CatharsisManager"));

                Settings.Instance.Load(_configrationDirectoryPaths["MainWindow"]);

                InitializeComponent();

                this.Title = string.Format("Outopos {0}", App.OutoposVersion);

                {
                    var icon = new BitmapImage();

                    icon.BeginInit();
                    icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    icon.EndInit();
                    if (icon.CanFreeze) icon.Freeze();

                    this.Icon = icon;
                }

                // World
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tabs\World.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    _worldRadioButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
                }

                // Wiki
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tabs\Wiki.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    _wikiRadioButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
                }

                // Chat
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tabs\Chat.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    _chatRadioButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
                }

                // Mail
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tabs\Mail.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    _mailRadioButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
                }

                // Profile
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tabs\Profile.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    _profileButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
                }

                // Options
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tabs\Options.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    _optionsButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
                }

                System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"));
                _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
                _notifyIcon.Visible = true;

                this.Setting_Init();

                _notifyIcon.Visible = false;
                _notifyIcon.Click += (object sender2, EventArgs e2) =>
                {
                    if (_closed) return;

                    try
                    {
                        this.Show();
                        this.Activate();
                        this.WindowState = _windowState;

                        _notifyIcon.Visible = false;
                    }
                    catch (Exception)
                    {

                    }
                };

                _timerThread = new Thread(this.TimerThread);
                _timerThread.Priority = ThreadPriority.Lowest;
                _timerThread.Name = "MainWindow_TimerThread";
                _timerThread.Start();

                _statusBarThread = new Thread(this.StatusBarThread);
                _statusBarThread.Priority = ThreadPriority.Highest;
                _statusBarThread.Name = "MainWindow_StatusBarThread";
                _statusBarThread.Start();

                _trafficMonitorThread = new Thread(this.TrafficMonitorThread);
                _trafficMonitorThread.Priority = ThreadPriority.Highest;
                _trafficMonitorThread.Name = "MainWindow_TrafficMonitorThread";
                _trafficMonitorThread.Start();

                Debug.WriteLineIf(System.Runtime.GCSettings.IsServerGC, "GCSettings.IsServerGC");

                sw.Stop();
                Debug.WriteLine("StartUp {0}", sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Log.Error(e);

                throw;
            }
        }

        public MainWindowTabType SelectedTab
        {
            get
            {
                return _selectedTab;
            }
            private set
            {
                _selectedTab = value;
            }
        }

        public static void CopyDirectory(string sourceDirectoryPath, string destDirectoryPath)
        {
            if (!Directory.Exists(destDirectoryPath))
            {
                Directory.CreateDirectory(destDirectoryPath);
                File.SetAttributes(destDirectoryPath, File.GetAttributes(sourceDirectoryPath));
            }

            foreach (string file in Directory.GetFiles(sourceDirectoryPath))
            {
                File.Copy(file, Path.Combine(destDirectoryPath, Path.GetFileName(file)), true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDirectoryPath))
            {
                CopyDirectory(dir, Path.Combine(destDirectoryPath, Path.GetFileName(dir)));
            }
        }

        private void SetConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Red)
            {
                _stateImage.Source = StatesIconManager.Red;
            }
            else if (state == ConnectionState.Yello)
            {
                _stateImage.Source = StatesIconManager.Yello;
            }
            else if (state == ConnectionState.Green)
            {
                _stateImage.Source = StatesIconManager.Green;
            }
        }

        private static class StatesIconManager
        {
            static StatesIconManager()
            {
                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"States\Red.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    StatesIconManager.Red = bitmap;
                }

                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"States\Yello.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    StatesIconManager.Yello = bitmap;
                }

                {
                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"States\Green.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();

                    StatesIconManager.Green = bitmap;
                }
            }

            public static ImageSource Red { get; private set; }
            public static ImageSource Yello { get; private set; }
            public static ImageSource Green { get; private set; }
        }

        private void TimerThread()
        {
            try
            {
                Stopwatch spaceCheckStopwatch = new Stopwatch();
                Stopwatch backupStopwatch = new Stopwatch();
                Stopwatch updateStopwatch = new Stopwatch();
                Stopwatch uriUpdateStopwatch = new Stopwatch();
                Stopwatch compactionStopwatch = new Stopwatch();
                Stopwatch garbageCollectStopwatch = new Stopwatch();

                spaceCheckStopwatch.Start();
                backupStopwatch.Start();
                updateStopwatch.Start();
                uriUpdateStopwatch.Start();
                compactionStopwatch.Start();
                garbageCollectStopwatch.Start();

                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (_closed) return;

                    {
                        if (_autoBaseNodeSettingManager.State == ManagerState.Stop
                            && Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled)
                        {
                            _autoBaseNodeSettingManager.Start();
                        }
                        else if (_autoBaseNodeSettingManager.State == ManagerState.Start
                            && !Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled)
                        {
                            _autoBaseNodeSettingManager.Stop();
                        }

                        if (_overlayNetworkManager.State == ManagerState.Stop
                            && Settings.Instance.Global_I2p_SamBridge_IsEnabled)
                        {
                            _overlayNetworkManager.Start();
                        }
                        else if (_overlayNetworkManager.State == ManagerState.Start
                            && !Settings.Instance.Global_I2p_SamBridge_IsEnabled)
                        {
                            _overlayNetworkManager.Stop();
                        }

                        if (_outoposManager.State == ManagerState.Stop)
                        {
                            _outoposManager.Start();

                            Log.Information("Start");
                        }
                    }

                    if (backupStopwatch.Elapsed.TotalMinutes >= 30)
                    {
                        backupStopwatch.Restart();

                        try
                        {
                            _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                            _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                            _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                            _outoposManager.Save(_configrationDirectoryPaths["OutoposManager"]);
                            Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (updateStopwatch.Elapsed.TotalDays >= 1)
                    {
                        updateStopwatch.Restart();

                        try
                        {
                            if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
                               || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
                            {
                                this.CheckUpdate(false);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (uriUpdateStopwatch.Elapsed.TotalHours >= 1)
                    {
                        uriUpdateStopwatch.Restart();

                        try
                        {
                            _autoBaseNodeSettingManager.Update();
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (garbageCollectStopwatch.Elapsed.TotalSeconds >= 60)
                    {
                        garbageCollectStopwatch.Restart();

                        try
                        {
                            this.GarbageCollect();
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void GarbageCollect()
        {
            // LargeObjectHeapCompactionModeの設定を試みる。(.net 4.5.1以上で可能)
            try
            {
                var type = typeof(System.Runtime.GCSettings);
                var property = type.GetProperty("LargeObjectHeapCompactionMode", BindingFlags.Static | BindingFlags.Public);

                if (null != property)
                {
                    var Setter = property.GetSetMethod();
                    Setter.Invoke(null, new object[] { /* GCLargeObjectHeapCompactionMode.CompactOnce */ 2 });

                    Debug.WriteLine("Set GCLargeObjectHeapCompactionMode.CompactOnce");
                }
            }
            catch (Exception)
            {

            }

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception)
            {

            }
        }

        private void StatusBarThread()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (_closed) return;

                    var state = _outoposManager.State;
                    this.Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 1), new Action(() =>
                    {
                        try
                        {
                            decimal sentAverageTraffic;

                            lock (_sentInfomation.ThisLock)
                            {
                                sentAverageTraffic = _sentInfomation.AverageTrafficList.Sum() / _sentInfomation.AverageTrafficList.Length;
                            }

                            decimal receivedAverageTraffic;

                            lock (_receivedInfomation.ThisLock)
                            {
                                receivedAverageTraffic = _receivedInfomation.AverageTrafficList.Sum() / _receivedInfomation.AverageTrafficList.Length;
                            }

                            _sendingSpeedTextBlock.Text = NetworkConverter.ToSizeString(sentAverageTraffic) + "/s";
                            _receivingSpeedTextBlock.Text = NetworkConverter.ToSizeString(receivedAverageTraffic) + "/s";
                        }
                        catch (Exception)
                        {

                        }

                        try
                        {
                            if (_outoposManager.State == ManagerState.Stop)
                            {
                                this.SetConnectionState(ConnectionState.Red);
                            }
                            else if (_outoposManager.State == ManagerState.Start)
                            {
                                var surroundingNodeCount = (int)_outoposManager.Information["SurroundingNodeCount"];

                                if (surroundingNodeCount <= 0)
                                {
                                    this.SetConnectionState(ConnectionState.Red);
                                }
                                else if (surroundingNodeCount <= 3)
                                {
                                    this.SetConnectionState(ConnectionState.Yello);
                                }
                                else
                                {
                                    this.SetConnectionState(ConnectionState.Green);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private TrafficInformation _sentInfomation = new TrafficInformation();
        private TrafficInformation _receivedInfomation = new TrafficInformation();

        private void TrafficMonitorThread()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                while (!_closed)
                {
                    Thread.Sleep(((int)Math.Max(2, 1000 - sw.ElapsedMilliseconds)) / 2);
                    if (sw.ElapsedMilliseconds < 1000) continue;

                    var receivedByteCount = _outoposManager.ReceivedByteCount;
                    var sentByteCount = _outoposManager.SentByteCount;

                    lock (_sentInfomation.ThisLock)
                    {
                        _sentInfomation.AverageTrafficList[_sentInfomation.Round++]
                            = ((decimal)(sentByteCount - _sentInfomation.PreviousTraffic)) * 1000 / sw.ElapsedMilliseconds;
                        _sentInfomation.PreviousTraffic = sentByteCount;

                        if (_sentInfomation.Round >= _sentInfomation.AverageTrafficList.Length)
                        {
                            _sentInfomation.Round = 0;
                        }
                    }

                    lock (_receivedInfomation.ThisLock)
                    {
                        _receivedInfomation.AverageTrafficList[_receivedInfomation.Round++]
                            = ((decimal)(receivedByteCount - _receivedInfomation.PreviousTraffic)) * 1000 / sw.ElapsedMilliseconds;
                        _receivedInfomation.PreviousTraffic = receivedByteCount;

                        if (_receivedInfomation.Round >= _receivedInfomation.AverageTrafficList.Length)
                        {
                            _receivedInfomation.Round = 0;
                        }
                    }

                    sw.Restart();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private class TrafficInformation : IThisLock
        {
            private decimal[] _averageTrafficList = new decimal[3];

            private readonly object _thisLock = new object();

            public long PreviousTraffic { get; set; }

            public int Round { get; set; }

            public decimal[] AverageTrafficList
            {
                get
                {
                    return _averageTrafficList;
                }
            }

            #region IThisLock

            public object ThisLock
            {
                get
                {
                    return _thisLock;
                }
            }

            #endregion
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

                        case 2:
                            osName = "Windows 8";
                            break;

                        case 3:
                            osName = "Windows 8.1";
                            break;
                    }
                }
                else if (osInfo.Version.Major == 10)
                {
                    osName = "Windows 10";
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
                "Outopos:\t\t{0}\r\n" +
                "OS:\t\t{1} ({2})\r\n" +
                ".NET Framework:\t{3}", App.OutoposVersion.ToString(3), osName, osInfo.VersionString, Environment.Version);
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
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                    }
                    else
                    {
                        _logPath = Path.Combine(App.DirectoryPaths["Log"], string.Format("{0}.({1}).txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.DateTimeFormatInfo.InvariantInfo), logCount));
                    }

                    logCount++;
                } while (File.Exists(_logPath));
            }

            Log.LogEvent += (object sender, LogEventArgs e) =>
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
                                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            };
        }

        private void Setting_Init()
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.SystemRequired | ExecutionState.Continuous);

            {
                bool initFlag = false;

                _outoposManager = new OutoposManager(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.bitmap"), App.Cache.Path, _bufferManager);
                _outoposManager.Load(_configrationDirectoryPaths["OutoposManager"]);

                if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version")))
                {
                    initFlag = true;

                    // デフォルトのTrustSignaturesの設定。
                    {
                        Settings.Instance.Global_TrustSignatures.Add("Lyrise@OTAhpWvmegu50LT-p5dZ16og7U6bdpO4z5TInZxGsCs");
                    }

                    // デフォルトのChatタグを設定。
                    {
                        var amoebaTag = OutoposConverter.FromChatString("Chat:AAAAAAAGQW1vZWJhAQAAACCrzcPHuDlkIdAKPyrMvdoRizFo3IOOSlWhQhPTKBIOiWRXEcU");
                        var outoposTag = OutoposConverter.FromChatString("Chat:AAAAAAAHT3V0b3BvcwEAAAAgr9B65c-3yJS95GHleeXi3TekYOtScR4VzJRpz7AoQ294gS26");
                        var testTag = OutoposConverter.FromChatString("Chat:AAAAAAAEVGVzdAEAAAAgye1mG24NVcdu5Vb2UZXwrnT_kwhXUNWONT0W0m5IAyplJHYb");

                        Settings.Instance.ChatControl_ChatCategorizeTreeItem.ChatTreeItems.Add(new ChatTreeItem(amoebaTag));
                        Settings.Instance.ChatControl_ChatCategorizeTreeItem.ChatTreeItems.Add(new ChatTreeItem(outoposTag));
                        Settings.Instance.ChatControl_ChatCategorizeTreeItem.ChatTreeItems.Add(new ChatTreeItem(testTag));
                    }

                    {
                        byte[] buffer = new byte[32];

                        using (var random = RandomNumberGenerator.Create())
                        {
                            random.GetBytes(buffer);
                        }

                        _outoposManager.SetBaseNode(new Node(buffer, null));
                    }

                    _outoposManager.ListenUris.Clear();
                    _outoposManager.ListenUris.Add(string.Format("tcp:{0}:{1}", IPAddress.Any.ToString(), _random.Next(1024, ushort.MaxValue + 1)));
                    _outoposManager.ListenUris.Add(string.Format("tcp:[{0}]:{1}", IPAddress.IPv6Any.ToString(), _random.Next(1024, ushort.MaxValue + 1)));

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
                        ProxyUri = "tcp:127.0.0.1:29050",
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

                    _outoposManager.Filters.Clear();
                    _outoposManager.Filters.Add(ipv4ConnectionFilter);
                    _outoposManager.Filters.Add(ipv6ConnectionFilter);
                    _outoposManager.Filters.Add(tcpConnectionFilter);
                    _outoposManager.Filters.Add(torConnectionFilter);
                    _outoposManager.Filters.Add(i2pConnectionFilter);

                    if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                    {
                        Settings.Instance.Global_UseLanguage = "Japanese";
                    }
                    else
                    {
                        Settings.Instance.Global_UseLanguage = "English";
                    }

                    // ProfileItem
                    {
                        var digitalSignature = new DigitalSignature("Anonymous", DigitalSignatureAlgorithm.Rsa2048_Sha256);
                        Settings.Instance.Global_DigitalSignatureCollection.Add(digitalSignature);

                        var profileItem = new ProfileItem();
                        Settings.Instance.Global_ProfileItem = profileItem;

                        {
                            profileItem.UploadSignature = digitalSignature.ToString();
                            profileItem.Exchange = new Exchange(ExchangeAlgorithm.Rsa2048);

                            // Upload
                            {
                                _outoposManager.UploadProfile(profileItem.Cost,
                                    profileItem.Exchange.GetExchangePublicKey(),
                                    profileItem.TrustSignatures,
                                    profileItem.DeleteSignatures,
                                    profileItem.Wikis,
                                    profileItem.Chats,
                                    digitalSignature);
                            }
                        }
                    }

                    // Nodes.txtにあるノード情報を追加する。
                    if (File.Exists(Path.Combine(App.DirectoryPaths["Settings"], "Nodes.txt")))
                    {
                        var list = new List<Node>();

                        using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Settings"], "Nodes.txt"), new UTF8Encoding(false)))
                        {
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                list.Add(OutoposConverter.FromNodeString(line));
                            }
                        }

                        _outoposManager.SetOtherNodes(list);
                    }
                }
                else
                {
                    Version version;

                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
                    }

                    if (version <= new Version(1, 0, 0))
                    {
                        // デフォルトのTrustSignaturesの設定。
                        {
                            Settings.Instance.Global_TrustSignatures.Add("Lyrise@OTAhpWvmegu50LT-p5dZ16og7U6bdpO4z5TInZxGsCs");
                        }
                    }
                }

                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version"), false, new UTF8Encoding(false)))
                {
                    writer.WriteLine(App.OutoposVersion.ToString());
                }

                _autoBaseNodeSettingManager = new AutoBaseNodeSettingManager(_outoposManager);
                _autoBaseNodeSettingManager.Load(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);

                _overlayNetworkManager = new OverlayNetworkManager(_outoposManager, _bufferManager);
                _overlayNetworkManager.Load(_configrationDirectoryPaths["OverlayNetworkManager"]);

                _catharsisManager = new CatharsisManager(_outoposManager, _bufferManager);
                _catharsisManager.Load(_configrationDirectoryPaths["CatharsisManager"]);

                if (initFlag)
                {
                    _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                    _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _outoposManager.Save(_configrationDirectoryPaths["OutoposManager"]);
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                }

                {
                    var outoposPath = Path.Combine(App.DirectoryPaths["Configuration"], "Outopos");
                    var libraryPath = Path.Combine(App.DirectoryPaths["Configuration"], "Library");

                    try
                    {
                        if (Directory.Exists(outoposPath))
                        {
                            if (Directory.Exists(outoposPath + ".old"))
                                Directory.Delete(outoposPath + ".old", true);

                            MainWindow.CopyDirectory(outoposPath, outoposPath + ".old");
                        }

                        if (Directory.Exists(libraryPath))
                        {
                            if (Directory.Exists(libraryPath + ".old"))
                                Directory.Delete(libraryPath + ".old", true);

                            MainWindow.CopyDirectory(libraryPath, libraryPath + ".old");
                        }
                    }
                    catch (Exception e2)
                    {
                        Log.Warning(e2);
                    }
                }
            }
        }

        private WebProxy GetProxy()
        {
            var proxyUri = Settings.Instance.Global_Update_ProxyUri;

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

                return new WebProxy(proxyHost, proxyPort);
            }

            return null;
        }

        private volatile bool _checkUpdate_IsRunning = false;

        private object _updateLockObject = new object();
        private Version _updateCancelVersion;

        private void CheckUpdate(bool isLogFlag)
        {
            if (_checkUpdate_IsRunning) return;
            _checkUpdate_IsRunning = true;

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    lock (_updateLockObject)
                    {
                        var url = Settings.Instance.Global_Update_Url;
                        string line1;
                        string line2;
                        string line3;

                        for (int i = 0; ; i++)
                        {
                            try
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
                                rq.Proxy = this.GetProxy();

                                using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                                using (Stream stream = rs.GetResponseStream())
                                using (StreamReader r = new StreamReader(stream))
                                {
                                    line1 = r.ReadLine();
                                    line2 = r.ReadLine();
                                    line3 = r.ReadLine();
                                }

                                break;
                            }
                            catch (Exception e)
                            {
                                if (i < 10)
                                {
                                    continue;
                                }
                                else
                                {
                                    Log.Error(e);

                                    return;
                                }
                            }
                        }

                        Regex regex = new Regex(@"Outopos ((\d*)\.(\d*)\.(\d*)).*\.zip");
                        var match = regex.Match(line1);

                        if (match.Success)
                        {
                            var targetVersion = new Version(match.Groups[1].Value);

                            if (targetVersion <= App.OutoposVersion)
                            {
                                if (isLogFlag)
                                {
                                    Log.Information(string.Format("Check Update: {0}", LanguagesManager.Instance.MainWindow_LatestVersion_Message));
                                }
                            }
                            else
                            {
                                if (!isLogFlag && targetVersion == _updateCancelVersion) return;

                                {
                                    foreach (var path in Directory.GetFiles(App.DirectoryPaths["Update"]))
                                    {
                                        string name = Path.GetFileName(path);

                                        if (name.StartsWith("Outopos"))
                                        {
                                            var match2 = regex.Match(name);

                                            if (match2.Success)
                                            {
                                                var tempVersion = new Version(match2.Groups[1].Value);

                                                if (targetVersion <= tempVersion) return;
                                            }
                                        }
                                    }
                                }

                                Log.Information(string.Format("Check Update: {0}", line1));

                                bool flag = true;

                                if (Settings.Instance.Global_Update_Option != UpdateOption.AutoUpdate)
                                {
                                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                    {
                                        if (MessageBox.Show(
                                            this,
                                            string.Format(LanguagesManager.Instance.MainWindow_CheckUpdate_Message, line1),
                                            "Update",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Information) == MessageBoxResult.Cancel)
                                        {
                                            flag = false;
                                        }
                                    }));
                                }

                                if (flag)
                                {
                                    var path = string.Format(@"{0}\{1}", App.DirectoryPaths["Work"], line1);
                                    this.GetFile(line2, path);

                                    var signaturePath = string.Format(@"{0}\{1}", App.DirectoryPaths["Work"], System.Web.HttpUtility.UrlDecode(Path.GetFileName(line3)));
                                    this.GetFile(line3, signaturePath);

                                    using (var stream = new FileStream(path, FileMode.Open))
                                    using (var signStream = new FileStream(signaturePath, FileMode.Open))
                                    {
                                        Certificate certificate = CertificateConverter.FromCertificateStream(signStream);

                                        if (Settings.Instance.Global_Update_Signature != certificate.ToString()) throw new Exception("Update DigitalSignature #1");
                                        if (!DigitalSignature.VerifyFileCertificate(certificate, stream.Name, stream)) throw new Exception("Update DigitalSignature #2");
                                    }

                                    if (File.Exists(path))
                                    {
                                        File.Move(path, Path.Combine(App.DirectoryPaths["Update"], Path.GetFileName(path)));
                                    }

                                    this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                    {
                                        MessageBox.Show(
                                            this,
                                            LanguagesManager.Instance.MainWindow_Restart_Message,
                                            "Update",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Information);
                                    }));
                                }
                                else
                                {
                                    _updateCancelVersion = targetVersion;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                finally
                {
                    _checkUpdate_IsRunning = false;
                }
            });
        }

        private void GetFile(string url, string path)
        {
            for (int i = 0; ; i++)
            {
                try
                {
                    HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create(url);
                    rq.Method = "GET";
                    rq.ContentType = "text/html; charset=UTF-8";
                    rq.UserAgent = "";
                    rq.ReadWriteTimeout = 1000 * 60 * 5;
                    rq.Timeout = 1000 * 60 * 5;
                    rq.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    rq.KeepAlive = true;
                    rq.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
                    rq.Proxy = this.GetProxy();

                    using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                    {
                        long size = 0;

                        using (Stream inStream = rs.GetResponseStream())
                        using (FileStream outStream = new FileStream(path, FileMode.Create))
                        {
                            byte[] buffer = new byte[1024 * 4];

                            int length = 0;

                            while ((length = inStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outStream.Write(buffer, 0, length);
                                size += length;
                            }
                        }

                        if (rs.ContentLength != -1 && size != rs.ContentLength)
                        {
                            try
                            {
                                File.Delete(path);
                            }
                            catch (Exception)
                            {

                            }

                            continue;
                        }
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (i < 10)
                    {
                        continue;
                    }
                    else
                    {
                        Log.Error(e);

                        return;
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);

            _windowState = this.WindowState;

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            TopRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).Y;
            };

            LeftRelativeDoubleConverter.GetDoubleEvent = (object state) =>
            {
                return this.PointToScreen(new Point(0, 0)).X;
            };

            WorldControl worldControl = new WorldControl(_outoposManager, _bufferManager);
            worldControl.Height = Double.NaN;
            worldControl.Width = Double.NaN;
            _worldContentControl.Content = worldControl;

            ChatControl chatControl = new ChatControl(_outoposManager, _bufferManager);
            chatControl.Height = Double.NaN;
            chatControl.Width = Double.NaN;
            _chatContentControl.Content = chatControl;

            if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
               || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
            {
                this.CheckUpdate(false);
            }

            this.SetConnectionState(ConnectionState.Red);

            this.GarbageCollect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_closed) return;

            if (MessageBox.Show(
                this,
                LanguagesManager.Instance.MainWindow_Close_Message,
                "Close",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.No)
            {
                e.Cancel = true;

                return;
            }

            _closed = true;

            e.Cancel = true;

            var thread = new Thread(() =>
            {
                try
                {
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        this.WindowState = System.Windows.WindowState.Minimized;
                    }));

                    _timerThread.Join();
                    _timerThread = null;

                    _statusBarThread.Join();
                    _statusBarThread = null;

                    _trafficMonitorThread.Join();
                    _trafficMonitorThread = null;

                    _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                    _catharsisManager.Dispose();

                    _autoBaseNodeSettingManager.Stop();
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _autoBaseNodeSettingManager.Dispose();

                    _overlayNetworkManager.Stop();
                    _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                    _overlayNetworkManager.Dispose();

                    _outoposManager.Stop();
                    _outoposManager.Save(_configrationDirectoryPaths["OutoposManager"]);
                    _outoposManager.Dispose();

                    NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);
                    _notifyIcon.Visible = false;

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        this.Close();
                    }));
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });
            thread.Priority = ThreadPriority.Highest;
            thread.Name = "MainWindow_CloseThread";
            thread.Start();
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

        private void _worldRadioButton_Click(object sender, RoutedEventArgs e)
        {
            var worldControl = _worldContentControl.Content as WorldControl;
            if (worldControl == null) return;

            worldControl.Update();
        }

        private void _profileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow window = new ProfileWindow(
                Settings.Instance.Global_ProfileItem,
                _outoposManager,
                _bufferManager);

            window.Owner = this;
            window.ShowDialog();
        }

        private void _optionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow window = new OptionsWindow(
                _outoposManager,
                _autoBaseNodeSettingManager,
                _overlayNetworkManager,
                _bufferManager);

            window.Owner = this;
            window.ShowDialog();
        }
    }
}
