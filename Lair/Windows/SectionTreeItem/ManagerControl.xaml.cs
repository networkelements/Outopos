using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for ManagerControl.xaml
    /// </summary>
    partial class ManagerControl : UserControl
    {
        private BufferManager _bufferManager;

        private ManagerInfo _managerInfo;

        private ObservableCollection<string> _trustSignatureListViewItemCollection;

        public event UploadEventHandler UploadEvent;

        public ManagerControl(ManagerInfo managerInfo, BufferManager bufferManager)
        {
            _managerInfo = managerInfo;
            _bufferManager = bufferManager;

            _trustSignatureListViewItemCollection = new ObservableCollection<string>(_managerInfo.TrustSignatures);

            InitializeComponent();

            _trustSignatureListView.ItemsSource = _trustSignatureListViewItemCollection;
            _commentTextBox.Text = _managerInfo.Comment;
        }

        public ManagerInfo ManagerInfo
        {
            get
            {
                var managerInfo = new ManagerInfo();

                managerInfo.TrustSignatures.AddRange(_trustSignatureListViewItemCollection);
                managerInfo.Comment = _commentTextBox.Text;

                return managerInfo;
            }
        }

        public bool IsUploadEnabled
        {
            get
            {
                return _uploadButton.IsEnabled;
            }
            set
            {
                _uploadButton.IsEnabled = value;
            }
        }

        protected virtual void OnUploadEvent()
        {
            if (this.UploadEvent != null)
            {
                this.UploadEvent(this);
            }
        }

        #region _trustSignatureListView

        private void _trustSignatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _trustSignatureAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _trustSignatureListViewUpdate()
        {
            _trustSignatureListView_SelectionChanged(this, null);
        }

        private void _trustSignatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _trustSignatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _trustSignatureUpButton.IsEnabled = false;
                    _trustSignatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _trustSignatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _trustSignatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _trustSignatureListViewItemCollection.Count - 1)
                    {
                        _trustSignatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _trustSignatureDownButton.IsEnabled = true;
                    }
                }

                _trustSignatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _trustSignatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _trustSignatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _trustSignatureTextBox.Text = "";
                return;
            }

            var item = _trustSignatureListView.SelectedItem as string;
            if (item == null) return;

            _trustSignatureTextBox.Text = item;
        }

        private void _trustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _trustSignatureListView.SelectedItems;

            _trustSignatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _trustSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _trustSignatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            _trustSignatureListViewPasteMenuItem.IsEnabled = Clipboard.GetText().Split('\r', '\n').Any(n => Signature.HasSignature(n));
        }

        private void _trustSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _trustSignatureDeleteButton_Click(null, null);
        }

        private void _trustSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _trustSignatureListViewCopyMenuItem_Click(null, null);
            _trustSignatureDeleteButton_Click(null, null);
        }

        private void _trustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _trustSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Signature.HasSignature(item)) continue;

                    if (_trustSignatureListViewItemCollection.Contains(item)) continue;
                    _trustSignatureListViewItemCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _trustSignatureTextBox.Text = "";
            _trustSignatureListView.SelectedIndex = _trustSignatureListViewItemCollection.Count - 1;

            _trustSignatureListViewUpdate();
        }

        private void _trustSignatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _trustSignatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _trustSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _trustSignatureListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _trustSignatureListViewUpdate();
        }

        private void _trustSignatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _trustSignatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _trustSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _trustSignatureListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _trustSignatureListViewUpdate();
        }

        private void _trustSignatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_trustSignatureTextBox.Text) || !Signature.HasSignature(_trustSignatureTextBox.Text)) return;

            var item = _trustSignatureTextBox.Text;

            if (_trustSignatureListViewItemCollection.Contains(item)) return;
            _trustSignatureListViewItemCollection.Add(item);

            _trustSignatureTextBox.Text = "";
            _trustSignatureListView.SelectedIndex = _trustSignatureListViewItemCollection.Count - 1;

            _trustSignatureListViewUpdate();
        }

        private void _trustSignatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _trustSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _trustSignatureTextBox.Text = "";

            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _trustSignatureListViewItemCollection.Remove(item);
            }

            _trustSignatureListView.SelectedIndex = selectIndex;
            _trustSignatureListViewUpdate();
        }

        #endregion

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_trustsTabItem.IsSelected)
            {
                _trustSignatureListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_trustsTabItem.IsSelected)
            {
                _trustSignatureListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_trustsTabItem.IsSelected)
            {
                _trustSignatureListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_trustsTabItem.IsSelected)
            {
                _trustSignatureListViewPasteMenuItem_Click(null, null);
            }
        }

        private void _uploadButton_Click(object sender, RoutedEventArgs e)
        {
            this.OnUploadEvent();
        }
    }
}
