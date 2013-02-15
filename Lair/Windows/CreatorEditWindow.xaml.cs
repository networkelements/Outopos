using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using System.Collections.ObjectModel;

namespace Lair.Windows
{
    /// <summary>
    /// CreatorEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class CreatorEditWindow : Window
    {
        private BufferManager _bufferManager;

        private ObservableCollection<string> _signatureListViewItemCollection;
        private ObservableCollection<CreatorEditBoardTreeViewItem> _treeViewItemCollection = new ObservableCollection<CreatorEditBoardTreeViewItem>();

        private Creator _creator;

        private Section _section;
        private List<Board> _boards = new List<Board>();
        private List<string> _filters = new List<string>();
        private string _comment;
        private Certificate _certificate;
        private List<DigitalSignature> _digitalSignatures = new List<DigitalSignature>();

        public CreatorEditWindow(Section section, IEnumerable<Board> boards, IEnumerable<string> filters, string comment, Certificate certificate, IEnumerable<DigitalSignature> digitalSignatures, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _section = section;
            if (boards != null) _boards.AddRange(boards);
            if (filters != null) _filters.AddRange(filters);
            _comment = comment;
            _certificate = certificate;
            _digitalSignatures.AddRange(digitalSignatures);

            _signatureListViewItemCollection = new ObservableCollection<string>(_filters);

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(_digitalSignatures.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _signatureComboBox.ItemsSource = digitalSignatureCollection;

            if (_certificate != null)
            {
                int index = 0;
                for (; _digitalSignatures.Count < index
                    && _digitalSignatures[index].ToString() != _certificate.ToString(); index++) ;

                _signatureComboBox.SelectedIndex = index + 1;
            }

            _signatureComboBox_SelectionChanged(null, null);

            _signatureListView.ItemsSource = _signatureListViewItemCollection;
            _treeView.ItemsSource = _treeViewItemCollection;

            foreach (var item in _boards)
            {
                _treeViewItemCollection.Add(new CreatorEditBoardTreeViewItem(item.Channel, item.Content));
            }

            _commentTextBox.Text = _comment;
        }

        private void Update()
        {
            var list = _treeViewItemCollection.ToList();

            list.Sort((CreatorEditBoardTreeViewItem x, CreatorEditBoardTreeViewItem y) =>
            {
                int c = MessageConverter.ToChannelString(x.Channel).CompareTo(MessageConverter.ToChannelString(y.Channel));
                if (c != 0) return c;

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _treeViewItemCollection.IndexOf(list[i]);

                if (i != o) _treeViewItemCollection.Move(o, i);
            }
        }

        public Creator Creator
        {
            get
            {
                return _creator;
            }
        }

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _okButton.IsEnabled = (_signatureComboBox.SelectedIndex > 0);
        }

        private void _signatureComboBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectComboBoxItem = (DigitalSignatureComboBoxItem)_signatureComboBox.SelectedItem;

            Clipboard.SetText(selectComboBoxItem.Value.ToString());
        }

        #region Signature

        private void _signatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _signatureAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _signatureListViewUpdate()
        {
            _signatureListView_SelectionChanged(this, null);
        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _signatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _signatureUpButton.IsEnabled = false;
                    _signatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _signatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _signatureListViewItemCollection.Count - 1)
                    {
                        _signatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureDownButton.IsEnabled = true;
                    }
                }

                _signatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _signatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _signatureTextBox.Text = "";
                return;
            }

            var item = _signatureListView.SelectedItem as string;
            if (item == null) return;

            _signatureTextBox.Text = item;
        }

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            _signatureListViewPasteMenuItem.IsEnabled = Clipboard.GetText().Split('\r', '\n').Any(n => Signature.HasSignature(n));
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewCopyMenuItem_Click(null, null);
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Signature.HasSignature(item)) continue;

                    if (_signatureListViewItemCollection.Contains(item)) continue;
                    _signatureListViewItemCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;

            _signatureListViewUpdate();
        }

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_signatureTextBox.Text) || !Signature.HasSignature(_signatureTextBox.Text)) return;

            var item = _signatureTextBox.Text;

            if (_signatureListViewItemCollection.Contains(item)) return;
            _signatureListViewItemCollection.Add(item);

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;

            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureTextBox.Text = "";

            foreach (var item in _signatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _signatureListViewItemCollection.Remove(item);
            }

            _signatureListView.SelectedIndex = selectIndex;
            _signatureListViewUpdate();
        }

        #endregion

        #region Channel

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var channels = Clipboard.GetChannels();

            _treeViewPasteMenuItem.IsEnabled = (channels.Count() > 0);
        }

        private void _treeViewNewChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewChannelWindow window = new NewChannelWindow();
            window.Owner = this;
            window.ShowDialog();

            if (window.DialogResult == true)
            {
                _treeViewItemCollection.Add(new CreatorEditBoardTreeViewItem(window.Channel, null));
                this.Update();
            }
        }

        private void _treeViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetChannels(_treeViewItemCollection.Select(n => n.Channel));
        }

        private void _treeViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var channel in _treeViewItemCollection.Select(n => n.Channel))
            {
                sb.AppendLine(LairConverter.ToChannelString(channel));
                sb.AppendLine(MessageConverter.ToInfoMessage(channel));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var channels = new HashSet<Channel>(Clipboard.GetChannels());
                channels.ExceptWith(_treeViewItemCollection.Select(n => n.Channel));

                if (channels.Count == 0) return;

                foreach (var channel in channels)
                {
                    _treeViewItemCollection.Add(new CreatorEditBoardTreeViewItem(channel, null));
                }

                this.Update();
            }
            catch (Exception)
            {

            }
        }

        private void _treeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CreatorEditBoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(this, LanguagesManager.Instance.MainWindow_Delete_Message, "Library", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _treeViewItemCollection.Remove(selectTreeViewItem);

            this.Update();
        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CreatorEditBoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChannels(new Channel[] { selectTreeViewItem.Channel });
        }

        private void _treeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CreatorEditBoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();
            sb.AppendLine(LairConverter.ToChannelString(selectTreeViewItem.Channel));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Channel));
            sb.AppendLine();

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            var boards = new BoardCollection();

            foreach (var item in _treeViewItemCollection)
            {
                boards.Add(new Board(item.Channel, item.Comment));
            }

            _creator = new Creator(_section, comment, boards, new SignatureCollection(_signatureListViewItemCollection), digitalSignature);
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewDeleteMenuItem_Click(null, null);
            }
            else if (_channelsTabItem.IsSelected)
            {
                _treeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewCopyMenuItem_Click(null, null);
            }
            else if (_channelsTabItem.IsSelected)
            {
                _treeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewPasteMenuItem_Click(null, null);
            }
            else if (_channelsTabItem.IsSelected)
            {
                _treeViewPasteMenuItem_Click(null, null);
            }
        }
    }

    class CreatorEditBoardTreeViewItem : TreeViewItem
    {
        private Channel _channel;
        private string _comment;

        public CreatorEditBoardTreeViewItem(Channel channel, string comment)
            : base()
        {
            this.Channel = channel;
            this.Comment = comment;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            base.Header = MessageConverter.ToChannelString(this.Channel);
        }

        public Channel Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;

                this.Update();
            }
        }

        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
            }
        }
    }
}
