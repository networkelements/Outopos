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
using Library.Net.Nest;
using Library;

namespace Nest.Windows
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    internal partial class MainWindow : Window
    {
        private BufferManager _bufferManager;

        private List< SessionTreeViewItem> _sessionTreeViewItems = new List<SessionTreeViewItem>();

        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private Dictionary<string, string> _configrationDirectoryPaths = new Dictionary<string, string>();
        private string _logPath = null;
        private FileStream _lockStream = null;

        private bool _disposed = false;

        public MainWindow()
        {
            try
            {
                _lockStream = new FileStream(Path.Combine(App.DirectoryPaths["Configuration"], "Nest.lock"), FileMode.Create);
            }
            catch (IOException)
            {
                this.Dispose();
            }

            _bufferManager = new BufferManager();

            InitializeComponent();

            foreach (var path in Directory.GetDirectories(Path.Combine(App.DirectoryPaths["Configuration"], "Nest", "Session"))
                .OrderBy(n => int.Parse(n)))
            {
                string name;
                ServerManager serverManager = new ServerManager(_bufferManager);
                SessionManager sessionManager = new SessionManager(_bufferManager);

                using (FileStream stream = new FileStream(Path.Combine(path, "Name.txt"), FileMode.Open))
                using (StreamReader reader = new StreamReader(stream))
                {
                    name = reader.ReadLine();
                }

                serverManager.Load(Path.Combine(path, "ServerManager"));
                sessionManager.Load(Path.Combine(path, "SessionManager"));

                _sessionTreeViewItems.Add(new SessionTreeViewItem()
                {
                    Name = name,
                    ServerManager = serverManager,
                    SessionManager = sessionManager,
                });
            }

            _treeView.ItemsSource = _sessionTreeViewItems;
        }

        ~MainWindow()
        {
            this.Dispose(false);
        }

        #region IDisposable メンバ

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            this.Dispose();
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this.Close();

                    if (_lockStream != null)
                    {
                        _lockStream.Close();
                        _lockStream = null;
                    }
                }

                _disposed = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
        }

        private void Settings_ServerLoad()
        {
        }

        private void _treeViewServerAddContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        public class SessionTreeViewItem : TreeViewItem
        {
            private string _name;
            private ServerManager _serverManager;
            private SessionManager _sessionManager;

            public SessionTreeViewItem()
                : base()
            {
                base.IsExpanded = true;
            }

            public void Update()
            {
                base.Header = new TextBlock()
                {
                    Text = _name
                };

                List<ChannelTreeViewItem> list = new List<ChannelTreeViewItem>();

                foreach (var item in this.SessionManager.Channels)
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

            public ServerManager ServerManager
            {
                get
                {
                    return _serverManager;
                }
                set
                {
                    _serverManager = value;

                    this.Update();
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
