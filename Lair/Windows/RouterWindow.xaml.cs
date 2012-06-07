using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Library;
using Library.Net.Lair;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for RouterWindow.xaml
    /// </summary>
    public partial class RouterWindow : Window
    {
        private SettingsManager _settingsManager;
        private RouterManager _routerManager;

        private UriCollection _listenUris = new UriCollection();

        public RouterWindow(ref SettingsManager settingsManager, ref RouterManager routerManager)
        {
            _settingsManager = settingsManager;
            _routerManager = routerManager;

            InitializeComponent();

            _serverListenUrisListView.ItemsSource = _listenUris;

            using (DeadlockMonitor.Lock(_settingsManager.ThisLock))
            {
                _nameTextBox.Text = _settingsManager.Name;
            }

            using (DeadlockMonitor.Lock(_routerManager.ThisLock))
            {
                _listenUris.AddRange(_routerManager.ListenUris);
                _miscellaneousConnectionCountLimitTextBox.Text = _routerManager.ConnectionCountLimit.ToString();
            }
        }

        #region Server

        private void _serverListenUrisListViewUpdate()
        {
            _serverListenUrisListView_SelectionChanged(this, null);
        }

        private void _serverListenUrisListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _serverListenUrisListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _serverListenUriUpButton.IsEnabled = false;
                    _serverListenUriDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _serverListenUriUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _serverListenUriUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _listenUris.Count - 1)
                    {
                        _serverListenUriDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _serverListenUriDownButton.IsEnabled = true;
                    }
                }

                _serverListenUrisListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _serverListenUrisListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _serverListenUriTextBox.Text = string.Format("tcp:0.0.0.0:{0}", new Random().Next(1024, 65536));
                ((ComboBoxItem)_serverListenUriSchemeComboBox.Items[0]).IsSelected = true;

                return;
            }

            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            _serverListenUriTextBox.Text = item;

            Regex regex = new Regex(@"(.*?):(.*)");
            Match match = regex.Match(item);

            if (match.Success)
            {
                var conboboxItem = _serverListenUriSchemeComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(n => (string)n.Content == match.Groups[1].Value);

                if (conboboxItem != null)
                {
                    conboboxItem.IsSelected = true;
                }
            }
        }

        private void _serverListenUriSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serverListenUriSchemeComboBox_PreviewMouseLeftButtonDown(this, null);
        }

        private void _serverListenUriSchemeComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _serverListenUriSchemeComboBox.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string scheme = (string)((ComboBoxItem)_serverListenUriSchemeComboBox.SelectedItem).Content;
            Regex regex = new Regex(@"(.*?):(.*)");
            Match match = regex.Match(_serverListenUriTextBox.Text);

            if (!match.Success)
            {
                _serverListenUriTextBox.Text = string.Format("{0}:0.0.0.0:{1}", scheme, new Random().Next(1024, 65536));
            }
            else
            {
                _serverListenUriTextBox.Text = string.Format("{0}:{1}", scheme, match.Groups[2].Value);
            }
        }

        private void _serverListenUriUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _listenUris.Remove(item);
            _listenUris.Insert(selectIndex - 1, item);
            _serverListenUrisListView.Items.Refresh();

            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _listenUris.Remove(item);
            _listenUris.Insert(selectIndex + 1, item);
            _serverListenUrisListView.Items.Refresh();

            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            var uri = _serverListenUriTextBox.Text;
            if (_listenUris.Any(n => n == uri)) return;
            _listenUris.Add(uri);

            _serverListenUriTextBox.Text = "";
            _serverListenUrisListView.SelectedIndex = _listenUris.Count - 1;

            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serverListenUriTextBox.Text == "") return;

            int selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            var uri = _serverListenUriTextBox.Text;
            if (_listenUris.Any(n => n == uri)) return;

            var item = _serverListenUrisListView.SelectedItem as string;
            if (item == null) return;

            _listenUris[_listenUris.IndexOf(item)] = _serverListenUriTextBox.Text;

            _serverListenUrisListView.SelectedIndex = selectIndex;
            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListViewUpdate();
        }

        private void _serverListenUriDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _serverListenUrisListView.SelectedIndex;
            if (selectIndex == -1) return;

            _serverListenUriTextBox.Text = "";

            foreach (var item in _serverListenUrisListView.SelectedItems.OfType<string>().ToArray())
            {
                _listenUris.Remove(item);
            }

            _serverListenUrisListView.Items.Refresh();
            _serverListenUrisListView.SelectedIndex = selectIndex;
            _serverListenUriSchemeComboBox_SelectionChanged(this, null);
            _serverListenUrisListViewUpdate();
        }

        #endregion

        #region Miscellaneous

        private static int GetStringToInt(string value)
        {
            StringBuilder builder = new StringBuilder("0");

            foreach (var item in value)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9]"))
                {
                    builder.Append(item.ToString());
                }
            }

            int count = 0;

            try
            {
                count = int.Parse(builder.ToString());
            }
            catch (OverflowException)
            {
                count = int.MaxValue;
            }

            return count;
        }

        private void _miscellaneousStackPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Expander expander = e.Source as Expander;
            if (expander == null) return;

            foreach (var item in _miscellaneousStackPanel.Children.OfType<Expander>())
            {
                if (expander != item) item.IsExpanded = false;
            }
        }

        private void _miscellaneousConnectionCountLimitTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _miscellaneousConnectionCountLimitTextBox.Text = RouterWindow.GetStringToInt(_miscellaneousConnectionCountLimitTextBox.Text).ToString();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            using (DeadlockMonitor.Lock(_settingsManager.ThisLock))
            {
                _settingsManager.Name = _nameTextBox.Text;
            }

            using (DeadlockMonitor.Lock(_routerManager.ThisLock))
            {
                _routerManager.ListenUris.Clear();
                _routerManager.ListenUris.AddRange(_listenUris);
                _routerManager.ConnectionCountLimit = int.Parse(_miscellaneousConnectionCountLimitTextBox.Text);
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
