using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Outopos.Properties;
using Library.Net;
using Library.Net.Outopos;
using Library.Security;
using System.Collections;
using Library;

namespace Outopos.Windows
{
    delegate void SignatureAddEventHandler(object sender, string signature);

    /// <summary>
    /// SignatureListWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SignatureListWindow : Window
    {
        private OutoposManager _lairManager;
        private ObservableCollectionEx<string> _signatureCollection;

        public event SignatureAddEventHandler SignatureAddEvent;

        public SignatureListWindow(IEnumerable<string> signatures, OutoposManager lairManager)
        {
            _lairManager = lairManager;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _signatureCollection = new ObservableCollectionEx<string>(signatures);
            _listView.ItemsSource = _signatureCollection;

            CollectionViewSource.GetDefaultView(_listView.ItemsSource).Filter = (object o) =>
            {
                string searchText = _searchTextBox.Text;
                if (string.IsNullOrWhiteSpace(searchText)) return true;

                var item = o as string;
                if (item == null) return false;

                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                var text = (item ?? "").ToLower();

                foreach (var word in words)
                {
                    if (!text.Contains(word))
                    {
                        return false;
                    }
                }

                return true;
            };

            this.Sort();
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        protected virtual void OnSignatureAddEvent(string signature)
        {
            if (this.SignatureAddEvent != null)
            {
                this.SignatureAddEvent(this, signature);
            }
        }

        #region _listView

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _addButton.IsEnabled = (_listView.SelectedIndex != -1);
        }

        private void _listView_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_listView.GetCurrentIndex(e.GetPosition) < 0) return;

            foreach (var signature in _listView.SelectedItems.Cast<string>().ToArray())
            {
                _signatureCollection.Remove(signature);

                this.OnSignatureAddEvent(signature);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewAddMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var signature in _listView.SelectedItems.Cast<string>())
            {
                sb.AppendLine(signature);
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _listViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _addButton_Click(null, null);
        }

        #endregion

        private void _searchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CollectionViewSource.GetDefaultView(_listView.ItemsSource).Refresh();

                e.Handled = true;
            }
        }

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.SignatureListWindow_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.SignatureListWindow_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.SignatureListWindow_LastHeaderClicked = headerClicked;
                Settings.Instance.SignatureListWindow_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.SignatureListWindow_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.SignatureListWindow_LastHeaderClicked, Settings.Instance.SignatureListWindow_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.SignatureListWindow_Value)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription(null, direction));
            }
        }

        #endregion

        private void _addButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in _listView.SelectedItems.Cast<string>().ToArray())
            {
                _signatureCollection.Remove(signature);
                this.OnSignatureAddEvent(signature);
            }
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
