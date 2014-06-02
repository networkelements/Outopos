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
using System.Runtime.InteropServices;
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
using Outopos.Properties;
using Library;
using Library.Io;
using Library.Net.Outopos;
using Library.Net.Connections;
using Library.Net.Proxy;
using Library.Net.Upnp;
using Library.Security;
using Library.Collections;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Outopos.Windows
{
    public delegate void DebugLog(string message);

    public enum MainWindowTabType
    {
        Connection,
        Section,
        Mail,
        Log,
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MainWindow : Window
    {
        private BufferManager _bufferManager;
        private OutoposManager _lairManager;
        private AutoBaseNodeSettingManager _autoBaseNodeSettingManager;
        private OverlayNetworkManager _overlayNetworkManager;
        private TransfarLimitManager _transferLimitManager;

        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath;

        private bool _isRun = true;
        private bool _autoStop;

        private System.Timers.Timer _refreshTimer = new System.Timers.Timer();
        private Thread _timerThread;
        private Thread _statusBarTimerThread;

        private volatile bool _diskSpaceNotFoundException;
        private volatile bool _cacheSpaceNotFoundException;

        private string _cacheBlocksPath;

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

                Settings.Instance.Load(_configrationDirectoryPaths["MainWindow"]);

                InitializeComponent();

                _windowState = this.WindowState;

                this.Title = string.Format("Outopos {0}", App.OutoposVersion);

                {
                    var icon = new BitmapImage();

                    icon.BeginInit();
                    icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    icon.EndInit();
                    if (icon.CanFreeze) icon.Freeze();

                    this.Icon = icon;
                }

                System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"));
                _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
                _notifyIcon.Visible = true;

                this.Setting_Init();
                this.Setting_Languages();

                _notifyIcon.Visible = false;
                _notifyIcon.Click += (object sender2, EventArgs e2) =>
                {
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

                _refreshTimer = new System.Timers.Timer();
                _refreshTimer.Elapsed += _refreshTimer_Elapsed;
                _refreshTimer.Interval = 1000;
                _refreshTimer.AutoReset = true;
                _refreshTimer.Start();

                _timerThread = new Thread(this.Timer);
                _timerThread.Priority = ThreadPriority.Highest;
                _timerThread.Name = "MainWindow_TimerThread";
                _timerThread.Start();

                _statusBarTimerThread = new Thread(this.StatusBarTimer);
                _statusBarTimerThread.Priority = ThreadPriority.Highest;
                _statusBarTimerThread.Name = "MainWindow_StatusBarTimerThread";
                _statusBarTimerThread.Start();

                _transferLimitManager.StartEvent += _transferLimitManager_StartEvent;
                _transferLimitManager.StopEvent += _transferLimitManager_StopEvent;

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

        void _transferLimitManager_StartEvent(object sender, EventArgs e)
        {
            if (_autoStop && !Settings.Instance.Global_IsStart)
            {
                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    _startMenuItem_Click(sender, null);
                }));
            }
        }

        void _transferLimitManager_StopEvent(object sender, EventArgs e)
        {
            Log.Information(LanguagesManager.Instance.MainWindow_TransferLimit_Message);

            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                _stopMenuItem_Click(sender, null);
            }));
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

        private void Timer()
        {
            try
            {
                Stopwatch debugStopwatch = new Stopwatch();
                Stopwatch spaceCheckStopwatch = new Stopwatch();
                Stopwatch backupStopwatch = new Stopwatch();
                Stopwatch updateStopwatch = new Stopwatch();
                Stopwatch uriUpdateStopwatch = new Stopwatch();
                Stopwatch gcStopwatch = new Stopwatch();
                debugStopwatch.Start();
                spaceCheckStopwatch.Start();
                backupStopwatch.Start();
                updateStopwatch.Start();
                uriUpdateStopwatch.Start();
                gcStopwatch.Start();

                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (!_isRun) return;

                    if (debugStopwatch.Elapsed.TotalMinutes >= 1)
                    {
                        debugStopwatch.Restart();

                        Debug.WriteLine(string.Format("----- ----- ----- BufferManager Size {0} ----- ----- -----", NetworkConverter.ToSizeString(_bufferManager.Size)));
                    }

                    {
                        if (_diskSpaceNotFoundException || _cacheSpaceNotFoundException)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                _stopMenuItem_Click(null, null);
                            }));
                        }

                        if (_autoBaseNodeSettingManager.State == ManagerState.Stop
                            && (Settings.Instance.Global_IsStart && Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled))
                        {
                            _autoBaseNodeSettingManager.Start();
                        }
                        else if (_autoBaseNodeSettingManager.State == ManagerState.Start
                            && (!Settings.Instance.Global_IsStart || !Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled))
                        {
                            _autoBaseNodeSettingManager.Stop();
                        }

                        if (_overlayNetworkManager.State == ManagerState.Stop
                            && (Settings.Instance.Global_IsStart && Settings.Instance.Global_I2p_SamBridge_IsEnabled))
                        {
                            _overlayNetworkManager.Start();
                        }
                        else if (_overlayNetworkManager.State == ManagerState.Start
                            && (!Settings.Instance.Global_IsStart || !Settings.Instance.Global_I2p_SamBridge_IsEnabled))
                        {
                            _overlayNetworkManager.Stop();
                        }

                        if (_lairManager.State == ManagerState.Stop
                            && Settings.Instance.Global_IsStart)
                        {
                            _lairManager.Start();

                            Log.Information("Start");
                        }
                        else if (_lairManager.State == ManagerState.Start
                            && !Settings.Instance.Global_IsStart)
                        {
                            _lairManager.Stop();

                            Log.Information("Stop");
                        }

                        if (_diskSpaceNotFoundException)
                        {
                            Log.Warning(LanguagesManager.Instance.MainWindow_DiskSpaceNotFound_Message);

                            this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                MessageBox.Show(
                                    this,
                                    LanguagesManager.Instance.MainWindow_DiskSpaceNotFound_Message,
                                    "Warning",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }));

                            _diskSpaceNotFoundException = false;
                        }

                        if (_cacheSpaceNotFoundException)
                        {
                            Log.Warning(LanguagesManager.Instance.MainWindow_CacheSpaceNotFound_Message);

                            this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                MessageBox.Show(
                                    this,
                                    LanguagesManager.Instance.MainWindow_CacheSpaceNotFound_Message,
                                    "Warning",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }));

                            _cacheSpaceNotFoundException = false;
                        }
                    }

                    if (Settings.Instance.Global_IsStart && spaceCheckStopwatch.Elapsed.TotalMinutes >= 1)
                    {
                        spaceCheckStopwatch.Restart();

                        try
                        {
                            DriveInfo drive = new DriveInfo(Directory.GetCurrentDirectory());

                            if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                            {
                                _diskSpaceNotFoundException = true;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }

                        try
                        {
                            if (!string.IsNullOrWhiteSpace(_cacheBlocksPath))
                            {
                                DriveInfo drive = new DriveInfo(Path.GetDirectoryName(Path.GetFullPath(_cacheBlocksPath)));

                                if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                                {
                                    _diskSpaceNotFoundException = true;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }

                    if (backupStopwatch.Elapsed.TotalMinutes >= 30)
                    {
                        backupStopwatch.Restart();

                        try
                        {
                            _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                            _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                            _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                            _lairManager.Save(_configrationDirectoryPaths["OutoposManager"]);
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
                                _checkUpdateMenuItem_Click(null, null);
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

                    if (gcStopwatch.Elapsed.TotalHours >= 1)
                    {
                        gcStopwatch.Restart();

                        try
                        {
                            System.GC.Collect();
                            System.GC.WaitForPendingFinalizers();
                            System.GC.Collect();
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

        private void StatusBarTimer()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (!_isRun) return;

                    var state = _lairManager.State;

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 1), new Action(() =>
                    {
                        try
                        {
                            _sendSpeedTextBlock.Text = NetworkConverter.ToSizeString(_ci.SentByteCountList.ToArray().Sum(n => n) / 3) + "/s";
                            _receiveSpeedTextBlock.Text = NetworkConverter.ToSizeString(_ci.ReceivedByteCountList.ToArray().Sum(n => n) / 3) + "/s";
                        }
                        catch (Exception)
                        {

                        }

                        try
                        {
                            string coreText = null;

                            if (state == ManagerState.Start) coreText = LanguagesManager.Instance.MainWindow_Running;
                            else coreText = LanguagesManager.Instance.MainWindow_Stopping;

                            _stateTextBlock.Text = string.Format(LanguagesManager.Instance.MainWindow_StatesBar, coreText);
                        }
                        catch (Exception)
                        {

                        }
                    }));
                }
            }
            catch (Exception)
            {

            }
        }

        private ConnectionInformation _ci = new ConnectionInformation();
        private bool _refreshTimer_Running;

        void _refreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_refreshTimer_Running) return;
            _refreshTimer_Running = true;

            try
            {
                var sentByteCount = _lairManager.SentByteCount;
                var receivedByteCount = _lairManager.ReceivedByteCount;

                _ci.SentByteCountList[_ci.Count] = sentByteCount - _ci.SentByteCount;
                _ci.SentByteCount = sentByteCount;
                _ci.ReceivedByteCountList[_ci.Count] = receivedByteCount - _ci.ReceivedByteCount;
                _ci.ReceivedByteCount = receivedByteCount;
                _ci.Count++;

                if (_ci.Count >= _ci.SentByteCountList.Count) _ci.Count = 0;
            }
            catch (Exception)
            {

            }
            finally
            {
                _refreshTimer_Running = false;
            }
        }

        private class ConnectionInformation
        {
            private LockedList<long> _sentByteCountList = new LockedList<long>(new long[3]);
            private LockedList<long> _receivedByteCountList = new LockedList<long>(new long[3]);

            public long SentByteCount { get; set; }
            public long ReceivedByteCount { get; set; }
            public int Count { get; set; }

            public LockedList<long> SentByteCountList
            {
                get
                {
                    return _sentByteCountList;
                }
            }

            public LockedList<long> ReceivedByteCountList
            {
                get
                {
                    return _receivedByteCountList;
                }
            }
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

            Log.LogEvent += (object sender, LogEventArgs e) =>
            {
                if (e.Exception != null && e.Exception.GetType().ToString() == "Library.Net.Outopos.SpaceNotFoundException")
                {
                    if (Settings.Instance.Global_IsStart)
                        _cacheSpaceNotFoundException = true;
                }

                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        try
                        {
                            if (_logListBox.Items.Count > 100)
                            {
                                _logListBox.Items.RemoveAt(0);
                            }

                            _logListBox.Items.Add(string.Format("{0} {1}:\t{2}", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.MessageLevel, e.Message));
                            _logListBox.ScrollIntoView(_logListBox.Items[_logListBox.Items.Count - 1]);
                        }
                        catch (Exception)
                        {

                        }
                    }));
                }
                catch (Exception)
                {

                }
            };

            Debug.Listeners.Add(new MyTraceListener((string message) =>
            {
                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        try
                        {
                            if (_logListBox.Items.Count > 100)
                            {
                                _logListBox.Items.RemoveAt(0);
                            }

                            _logListBox.Items.Add(string.Format("{0} Debug:\t{1}", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), message));
                            _logListBox.ScrollIntoView(_logListBox.Items[_logListBox.Items.Count - 1]);
                        }
                        catch (Exception)
                        {

                        }
                    }));
                }
                catch (Exception)
                {

                }
            }));
        }

        private class MyTraceListener : TraceListener
        {
            private DebugLog _debugLog;

            public MyTraceListener(DebugLog debugLog)
            {
                _debugLog = debugLog;
            }

            public override void Write(string message)
            {
                this.WriteLine(message);
            }

            public override void WriteLine(string message)
            {
                _debugLog(message);
            }
        }

        private void Setting_Languages()
        {
            foreach (var item in LanguagesManager.Instance.Languages)
            {
                var menuItem = new LanguageMenuItem() { IsCheckable = true, Value = item };

                menuItem.Click += (object sender, RoutedEventArgs e) =>
                {
                    foreach (var item3 in _languagesMenuItem.Items.Cast<LanguageMenuItem>())
                    {
                        item3.IsChecked = false;
                    }

                    menuItem.IsChecked = true;
                };

                menuItem.Checked += (object sender, RoutedEventArgs e) =>
                {
                    Settings.Instance.Global_UseLanguage = (string)menuItem.Value;
                    LanguagesManager.ChangeLanguage((string)menuItem.Value);
                };

                _languagesMenuItem.Items.Add(menuItem);
            }

            var menuItem2 = _languagesMenuItem.Items.Cast<LanguageMenuItem>().FirstOrDefault(n => n.Value == Settings.Instance.Global_UseLanguage);
            if (menuItem2 != null) menuItem2.IsChecked = true;
        }

        private void Setting_Init()
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.SystemRequired | ExecutionState.Continuous);

            {
                bool initFlag = false;

                if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.path")))
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.path"), new UTF8Encoding(false)))
                    {
                        _cacheBlocksPath = reader.ReadLine();
                    }
                }
                else
                {
                    _cacheBlocksPath = Path.Combine(App.DirectoryPaths["Configuration"], "Cache.blocks");

                    using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Cache.path"), false, new UTF8Encoding(false)))
                    {
                        writer.WriteLine(_cacheBlocksPath);
                    }
                }

                _lairManager = new OutoposManager(_cacheBlocksPath, _bufferManager);
                _lairManager.Load(_configrationDirectoryPaths["OutoposManager"]);

                if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version")))
                {
                    initFlag = true;

                    {
                        byte[] buffer = new byte[64];
                        (new RNGCryptoServiceProvider()).GetBytes(buffer);

                        _lairManager.SetBaseNode(new Node(buffer, null));
                    }

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

                    _lairManager.Filters.Clear();
                    _lairManager.Filters.Add(ipv4ConnectionFilter);
                    _lairManager.Filters.Add(ipv6ConnectionFilter);
                    _lairManager.Filters.Add(tcpConnectionFilter);
                    _lairManager.Filters.Add(torConnectionFilter);
                    _lairManager.Filters.Add(i2pConnectionFilter);

                    // デフォルトでセクションとチャンネルを設定
                    {
                        string chatOption;
                        var chat = OutoposConverter.FromChatString("Chat:AAAAAEAA7EGVwK3U57z4eCziwKn6emKp8Q2N0fiPFDbvcBtA6ul4UUcemclKQGs0QfVDKE9nrcPCAlbuIIEkkQ6-EoLVQgAAAAUBU3RhcnQvk9wn", out chatOption);

                        ChatTreeItem chatTreeItem = new ChatTreeItem(chat);

                        string sectionOption;
                        var section = OutoposConverter.FromSectionString("Section:AAAAAEAAfqnzk3Ro4Xt2xoRCns8xCNmqhhuv_ue2z-QdlEN6pfnyyUUR2A-031K2kCbdsvteDzN1vbICBwbu3p2dahO2ywAAABABQWxsaWFuY2UgTmV0d29ya9mG6sw,Lyrise@7seiSbhOCkls6gPxjJYjptxskzlSulgIe3dSfj1KxnJJ6eejKjuJ3R1Ec8yFuKpr4uNcwF7bFh5OrmxnY25y7A", out sectionOption);

                        SectionTreeItem sectionTreeItem = new SectionTreeItem(section);
                        sectionTreeItem.LeaderSignature = sectionOption;
                        sectionTreeItem.ChatCategorizeTreeItem.ChatTreeItems.Add(chatTreeItem);

                        Settings.Instance.SectionControl_SectionCategorizeTreeItem.SectionTreeItems.Add(sectionTreeItem);
                    }

                    if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                    {
                        Settings.Instance.Global_UseLanguage = "Japanese";
                    }
                    else
                    {
                        Settings.Instance.Global_UseLanguage = "English";
                    }
                }
                else
                {
                    Version version;

                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
                    }

                    if (version < new Version(2, 0, 2))
                    {
                        // デフォルトでセクションとチャンネルを設定
                        {
                            string chatOption;
                            var chat = OutoposConverter.FromChatString("Chat:AAAAAEAA7EGVwK3U57z4eCziwKn6emKp8Q2N0fiPFDbvcBtA6ul4UUcemclKQGs0QfVDKE9nrcPCAlbuIIEkkQ6-EoLVQgAAAAUBU3RhcnQvk9wn", out chatOption);

                            ChatTreeItem chatTreeItem = new ChatTreeItem(chat);

                            string sectionOption;
                            var section = OutoposConverter.FromSectionString("Section:AAAAAEAAfqnzk3Ro4Xt2xoRCns8xCNmqhhuv_ue2z-QdlEN6pfnyyUUR2A-031K2kCbdsvteDzN1vbICBwbu3p2dahO2ywAAABABQWxsaWFuY2UgTmV0d29ya9mG6sw,Lyrise@7seiSbhOCkls6gPxjJYjptxskzlSulgIe3dSfj1KxnJJ6eejKjuJ3R1Ec8yFuKpr4uNcwF7bFh5OrmxnY25y7A", out sectionOption);

                            SectionTreeItem sectionTreeItem = new SectionTreeItem(section);
                            sectionTreeItem.LeaderSignature = sectionOption;
                            sectionTreeItem.ChatCategorizeTreeItem.ChatTreeItems.Add(chatTreeItem);

                            Settings.Instance.SectionControl_SectionCategorizeTreeItem.SectionTreeItems.Add(sectionTreeItem);
                        }
                    }

                    if (version < new Version(2, 0, 10))
                    {
                        var count = Math.Min(_lairManager.ConnectionCountLimit, 25);
                        _lairManager.ConnectionCountLimit = 25;
                    }
                }

                using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version"), false, new UTF8Encoding(false)))
                {
                    writer.WriteLine(App.OutoposVersion.ToString());
                }

#if DEBUG
                if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Debug_NodeId.txt")))
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Debug_NodeId.txt"), new UTF8Encoding(false)))
                    {
                        byte[] buffer = new byte[64];

                        byte b = byte.Parse(reader.ReadLine());

                        for (int i = 0; i < 64; i++)
                        {
                            buffer[i] = b;
                        }

                        var baseNode = _lairManager.BaseNode;

                        _lairManager.SetBaseNode(new Node(buffer, baseNode.Uris));
                    }
                }
#endif

                _autoBaseNodeSettingManager = new AutoBaseNodeSettingManager(_lairManager);
                _autoBaseNodeSettingManager.Load(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);

                _overlayNetworkManager = new OverlayNetworkManager(_lairManager, _bufferManager);
                _overlayNetworkManager.Load(_configrationDirectoryPaths["OverlayNetworkManager"]);

                _transferLimitManager = new TransfarLimitManager(_lairManager);
                _transferLimitManager.Load(_configrationDirectoryPaths["TransfarLimitManager"]);
                _transferLimitManager.Start();

                if (initFlag)
                {
                    _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                    _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _lairManager.Save(_configrationDirectoryPaths["OutoposManager"]);
                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
                }

                {
                    var amoebaPath = Path.Combine(App.DirectoryPaths["Configuration"], "Outopos");
                    var libraryPath = Path.Combine(App.DirectoryPaths["Configuration"], "Library");

                    try
                    {
                        if (Directory.Exists(amoebaPath))
                        {
                            if (Directory.Exists(amoebaPath + ".old"))
                                Directory.Delete(amoebaPath + ".old", true);

                            MainWindow.CopyDirectory(amoebaPath, amoebaPath + ".old");
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

                {
                    if (string.IsNullOrWhiteSpace(Settings.Instance.Global_Amoeba_Path)
                        || !File.Exists(Settings.Instance.Global_Amoeba_Path))
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

        private object _updateLockObject = new object();
        private Version _updateCancelVersion;

        private void CheckUpdate(bool isLogFlag)
        {
            lock (_updateLockObject)
            {
                try
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
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
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
                                var path = string.Format(@"{0}\{1}", App.DirectoryPaths["Work"], System.Web.HttpUtility.UrlDecode(Path.GetFileName(line2)));
                                this.GetFile(line2, path);

                                var signPath = string.Format(@"{0}\{1}", App.DirectoryPaths["Work"], System.Web.HttpUtility.UrlDecode(Path.GetFileName(line3)));
                                this.GetFile(line3, signPath);

                                using (var stream = new FileStream(path, FileMode.Open))
                                using (var signStream = new FileStream(signPath, FileMode.Open))
                                {
                                    Certificate certificate = DigitalSignatureConverter.FromCertificateStream(signStream);

                                    if (Settings.Instance.Global_Update_Signature != certificate.ToString()) throw new Exception("Update DigitalSignature #1");
                                    if (!DigitalSignature.VerifyFileCertificate(certificate, stream)) throw new Exception("Update DigitalSignature #2");
                                }

                                if (File.Exists(path))
                                {
                                    File.Move(path, Path.Combine(App.DirectoryPaths["Update"], Path.GetFileName(path)));
                                }

                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
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
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
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
                    rq.ReadWriteTimeout = 1000 * 60;
                    rq.Timeout = 1000 * 60;
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

                            while (0 < (length = inStream.Read(buffer, 0, buffer.Length)))
                            {
                                outStream.Write(buffer, 0, length);
                                size += length;
                            }
                        }

                        if (size != rs.ContentLength)
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

            ConnectionControl connectionControl = new ConnectionControl(_lairManager);
            connectionControl.Height = Double.NaN;
            connectionControl.Width = Double.NaN;
            _connectionTabItem.Content = connectionControl;

            SectionControl sectionControl = new SectionControl(_lairManager, _bufferManager);
            sectionControl.Height = Double.NaN;
            sectionControl.Width = Double.NaN;
            _sectionTabItem.Content = sectionControl;

            if (Settings.Instance.Global_IsStart)
            {
                _startMenuItem_Click(null, null);
            }

            if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck
               || Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
            {
                _checkUpdateMenuItem_Click(null, null);
            }

            WindowPosition.Move(this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show(
                this,
                LanguagesManager.Instance.MainWindow_Close_Message,
                "Close",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);
            _notifyIcon.Visible = false;

            _isRun = false;

            var thread = new Thread(() =>
            {
                try
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                    _timerThread.Join();
                    _timerThread = null;

                    _statusBarTimerThread.Join();
                    _statusBarTimerThread = null;

                    _transferLimitManager.Stop();
                    _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                    _transferLimitManager.Dispose();

                    _autoBaseNodeSettingManager.Stop();
                    _autoBaseNodeSettingManager.Save(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);
                    _autoBaseNodeSettingManager.Dispose();

                    _overlayNetworkManager.Stop();
                    _overlayNetworkManager.Save(_configrationDirectoryPaths["OverlayNetworkManager"]);
                    _overlayNetworkManager.Dispose();

                    _lairManager.Stop();
                    _lairManager.Save(_configrationDirectoryPaths["OutoposManager"]);
                    _lairManager.Dispose();

                    Settings.Instance.Save(_configrationDirectoryPaths["MainWindow"]);
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

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != _tabControl) return;

            if (_tabControl.SelectedItem == _connectionTabItem)
            {
                this.SelectedTab = MainWindowTabType.Connection;
            }
            else if (_tabControl.SelectedItem == _sectionTabItem)
            {
                this.SelectedTab = MainWindowTabType.Section;
            }
            else if (_tabControl.SelectedItem == _logTabItem)
            {
                this.SelectedTab = MainWindowTabType.Log;
            }
            else
            {
                this.SelectedTab = 0;
            }

            this.Title = string.Format("Outopos {0}", App.OutoposVersion);
        }

        private void _coreMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _updateBaseNodeMenuItem.IsEnabled = Settings.Instance.Global_IsStart && _updateBaseNodeMenuItem_IsEnabled
                && (Settings.Instance.Global_AutoBaseNodeSetting_IsEnabled || Settings.Instance.Global_I2p_SamBridge_IsEnabled);
        }

        private void _startMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _startMenuItem.IsEnabled = false;
            _stopMenuItem.IsEnabled = true;

            Settings.Instance.Global_IsStart = true;
        }

        private void _stopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null) _autoStop = (sender.GetType() == typeof(TransfarLimitManager));

            _startMenuItem.IsEnabled = true;
            _stopMenuItem.IsEnabled = false;

            Settings.Instance.Global_IsStart = false;
        }

        volatile bool _updateBaseNodeMenuItem_IsEnabled = true;

        private void _updateBaseNodeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_updateBaseNodeMenuItem_IsEnabled) return;
            _updateBaseNodeMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
#if DEBUG
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
#endif

                    _autoBaseNodeSettingManager.Update();
                    _overlayNetworkManager.Restart();

#if DEBUG
                    sw.Stop();
                    Debug.WriteLine(sw.Elapsed.ToString());
#endif
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    _updateBaseNodeMenuItem_IsEnabled = true;
                }
            });
        }

        private void _coreOptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CoreOptionsWindow window = new CoreOptionsWindow(
                _lairManager,
                _autoBaseNodeSettingManager,
                _overlayNetworkManager,
                _transferLimitManager,
                _bufferManager);

            window.Owner = this;
            window.ShowDialog();
        }

        private void _viewOptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ViewOptionsWindow window = new ViewOptionsWindow(_bufferManager);
            window.Owner = this;
            window.ShowDialog();
        }

        private void _helpMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _checkUpdateMenuItem.IsEnabled = _checkUpdateMenuItem_IsEnabled;
        }

        private void _manualSiteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://lyrise.web.fc2.com/index.html");
        }

        private void _developerSiteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Alliance-Network");
        }

        volatile bool _checkUpdateMenuItem_IsEnabled = true;

        private void _checkUpdateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_checkUpdateMenuItem_IsEnabled) return;
            _checkUpdateMenuItem_IsEnabled = false;

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    this.CheckUpdate(sender != null);
                }
                catch (Exception)
                {

                }
                finally
                {
                    _checkUpdateMenuItem_IsEnabled = true;
                }
            });
        }

        private void _versionInformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            VersionInformationWindow window = new VersionInformationWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void _logListBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var line in _logListBox.SelectedItems.Cast<string>())
            {
                sb.AppendLine(line);
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\n', '\r'));
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_logTabItem.IsSelected)
            {
                _logListBoxCopyMenuItem_Click(null, null);
            }
        }
    }
}