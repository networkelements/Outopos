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
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using Outopos.Properties;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Outopos;
using Library.Net.Connections;
using Library.Net.Proxy;
using Library.Net.Upnp;
using Library.Security;

namespace Outopos.Windows
{
    delegate void DebugLog(string message);

    enum MainWindowTabType
    {
        Connection,
        Section,
        Chat,
        Wiki,
        Mail,
        Log,
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
        private TransfarLimitManager _transferLimitManager;
        private CatharsisManager _catharsisManager;

        private Random _random = new Random();

        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath;

        private volatile bool _closed = false;
        private bool _autoStop;

        private Thread _timerThread;
        private Thread _statusBarThread;
        private Thread _trafficMonitorThread;

        private volatile bool _diskSpaceNotFoundException;
        private volatile bool _cacheSpaceNotFoundException;

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

                System.Drawing.Icon myIcon = new System.Drawing.Icon(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"));
                _notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
                _notifyIcon.Visible = true;

                this.Setting_Init();
                this.Setting_Languages();

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

                _transferLimitManager.StartEvent += _transferLimitManager_StartEvent;
                _transferLimitManager.StopEvent += _transferLimitManager_StopEvent;

#if !DEBUG
                _logRowDefinition.Height = new GridLength(0);
#endif

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

                        if (_outoposManager.State == ManagerState.Stop
                            && Settings.Instance.Global_IsStart)
                        {
                            _outoposManager.Start();

                            Log.Information("Start");
                        }
                        else if (_outoposManager.State == ManagerState.Start
                            && !Settings.Instance.Global_IsStart)
                        {
                            _outoposManager.Stop();

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
                            if (!string.IsNullOrWhiteSpace(App.Cache.Path))
                            {
                                DriveInfo drive = new DriveInfo(Path.GetDirectoryName(Path.GetFullPath(App.Cache.Path)));

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
                            _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                            _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
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

                    if (compactionStopwatch.Elapsed.TotalMinutes >= 3)
                    {
                        compactionStopwatch.Restart();

                        try
                        {
                            this.Compaction();
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

        private void Compaction()
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
        }

        private void GarbageCollect()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
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

                            _sendSpeedTextBlock.Text = NetworkConverter.ToSizeString(sentAverageTraffic) + "/s";
                            _receiveSpeedTextBlock.Text = NetworkConverter.ToSizeString(receivedAverageTraffic) + "/s";
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
                        if (_logCheckBox.IsChecked.Value)
                        {
                            try
                            {
                                if (_logListBox.Items.Count > 100)
                                {
                                    _logListBox.Items.RemoveAt(0);
                                }

                                _logListBox.Items.Add(string.Format("{0} {1}:\t{2}", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), e.MessageLevel, e.Message));
                                _logListBox.GoBottom();
                            }
                            catch (Exception)
                            {

                            }
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
                        if (_debugCheckBox.IsChecked.Value)
                        {
                            try
                            {
                                if (_logListBox.Items.Count > 100)
                                {
                                    _logListBox.Items.RemoveAt(0);
                                }

                                _logListBox.Items.Add(string.Format("{0} Debug:\t{1}", DateTime.Now.ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo), message));
                                _logListBox.GoBottom();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }));
                }
                catch (Exception)
                {

                }
            }));
        }

        private void _logListBox_Loaded(object sender, RoutedEventArgs e)
        {
            _logListBox.GoBottom();
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
            foreach (var language in LanguagesManager.Instance.Languages)
            {
                var menuItem = new LanguageMenuItem() { IsCheckable = true, Value = language };

                menuItem.Click += (object sender, RoutedEventArgs e) =>
                {
                    foreach (var item in _languagesMenuItem.Items.Cast<LanguageMenuItem>())
                    {
                        item.IsChecked = false;
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

            {
                var menuItem = _languagesMenuItem.Items.Cast<LanguageMenuItem>().FirstOrDefault(n => n.Value == Settings.Instance.Global_UseLanguage);
                if (menuItem != null) menuItem.IsChecked = true;
            }
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

                    {
                        byte[] buffer = new byte[64];

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
                        ProxyUri = "tcp:127.0.0.1:19050",
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
                }
                else
                {
                    Version version;

                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Outopos.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
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

                        var baseNode = _outoposManager.BaseNode;

                        _outoposManager.SetBaseNode(new Node(buffer, baseNode.Uris));
                    }
                }
#endif

                _autoBaseNodeSettingManager = new AutoBaseNodeSettingManager(_outoposManager);
                _autoBaseNodeSettingManager.Load(_configrationDirectoryPaths["AutoBaseNodeSettingManager"]);

                _overlayNetworkManager = new OverlayNetworkManager(_outoposManager, _bufferManager);
                _overlayNetworkManager.Load(_configrationDirectoryPaths["OverlayNetworkManager"]);

                _transferLimitManager = new TransfarLimitManager(_outoposManager);
                _transferLimitManager.Load(_configrationDirectoryPaths["TransfarLimitManager"]);
                _transferLimitManager.Start();

                _catharsisManager = new CatharsisManager(_outoposManager, _bufferManager);
                _catharsisManager.Load(_configrationDirectoryPaths["CatharsisManager"]);

                if (initFlag)
                {
                    _catharsisManager.Save(_configrationDirectoryPaths["CatharsisManager"]);
                    _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
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

        private object _updateLockObject = new object();
        private Version _updateCancelVersion;

        private void CheckUpdate(bool isLogFlag)
        {

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

            ConnectionControl connectionControl = new ConnectionControl(_outoposManager, _bufferManager);
            connectionControl.Height = Double.NaN;
            connectionControl.Width = Double.NaN;
            _connectionTabItem.Content = connectionControl;

            SectionControl sectionControl = new SectionControl(_outoposManager, _bufferManager);
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

                    _transferLimitManager.Stop();
                    _transferLimitManager.Save(_configrationDirectoryPaths["TransfarLimitManager"]);
                    _transferLimitManager.Dispose();

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

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != _tabControl) return;

            if (_tabControl.SelectedItem == _connectionTabItem)
            {
                this.SelectedTab = MainWindowTabType.Connection;
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
                _outoposManager,
                _autoBaseNodeSettingManager,
                _overlayNetworkManager,
                _transferLimitManager,
                _bufferManager);

            window.Owner = this;
            window.ShowDialog();
        }

        private void _cacheMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _checkBlocksMenuItem.IsEnabled = _checkBlocksMenuItem_IsEnabled;
        }

        volatile bool _checkBlocksMenuItem_IsEnabled = true;

        private void _checkBlocksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_checkBlocksMenuItem_IsEnabled) return;
            _checkBlocksMenuItem_IsEnabled = false;

            var window = new ProgressWindow(true);
            window.Owner = this;
            window.Title = string.Format(LanguagesManager.Instance.ProgressWindow_Title, LanguagesManager.Instance.MainWindow_CheckBlocks_Message);
            window.Message = string.Format(LanguagesManager.Instance.MainWindow_CheckBlocks_State, 0, 0, 0);
            window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Cancel;

            ThreadPool.QueueUserWorkItem((object wstate) =>
            {
                bool flag = false;

                window.Closed += (object sender2, EventArgs e2) =>
                {
                    flag = true;
                };

                _outoposManager.ChecBlocks((object sender2, int badBlockCount, int checkedBlockCount, int blockCount, out bool isStop) =>
                {
                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        try
                        {
                            window.Value = 100 * ((double)checkedBlockCount / (double)blockCount);
                            window.Message = string.Format(LanguagesManager.Instance.MainWindow_CheckBlocks_State, badBlockCount, checkedBlockCount, blockCount);
                        }
                        catch (Exception)
                        {

                        }
                    }));

                    isStop = flag;
                });

                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                    try
                    {
                        window.ButtonMessage = LanguagesManager.Instance.ProgressWindow_Ok;
                    }
                    catch (Exception)
                    {

                    }
                }));
            });

            window.Closed += (object sender2, EventArgs e2) =>
            {
                _checkBlocksMenuItem_IsEnabled = true;
            };

            window.Owner = this;
            window.Show();
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
