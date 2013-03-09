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
    /// Interaction logic for LeaderControl.xaml
    /// </summary>
    partial class LeaderControl : UserControl
    {
        private BufferManager _bufferManager;

        private LeaderInfo _leaderInfo;

        private ObservableCollection<string> _creatorSignatureListViewItemCollection;
        private ObservableCollection<string> _managerSignatureListViewItemCollection;

        public event UploadEventHandler UploadEvent;

        public LeaderControl(LeaderInfo leaderInfo, BufferManager bufferManager)
        {
            _leaderInfo = leaderInfo.DeepClone();
            _bufferManager = bufferManager;

            _creatorSignatureListViewItemCollection = new ObservableCollection<string>(_leaderInfo.CreatorSignatures);
            _managerSignatureListViewItemCollection = new ObservableCollection<string>(_leaderInfo.ManagerSignatures);

            InitializeComponent();

            _creatorSignatureListView.ItemsSource = _creatorSignatureListViewItemCollection;
            _managerSignatureListView.ItemsSource = _managerSignatureListViewItemCollection;
            _commentTextBox.Text = _leaderInfo.Comment;
        }

        public LeaderInfo LeaderInfo
        {
            get
            {
                var leaderInfo = new LeaderInfo();

                leaderInfo.CreatorSignatures.AddRange(_creatorSignatureListViewItemCollection);
                leaderInfo.ManagerSignatures.AddRange(_managerSignatureListViewItemCollection);
                leaderInfo.Comment = _commentTextBox.Text;

                return leaderInfo;
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

        #region _creatorSignatureListView

        private void _creatorSignatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _creatorSignatureAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _creatorSignatureListViewUpdate()
        {
            _creatorSignatureListView_SelectionChanged(this, null);
        }

        private void _creatorSignatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _creatorSignatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _creatorSignatureUpButton.IsEnabled = false;
                    _creatorSignatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _creatorSignatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _creatorSignatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _creatorSignatureListViewItemCollection.Count - 1)
                    {
                        _creatorSignatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _creatorSignatureDownButton.IsEnabled = true;
                    }
                }

                _creatorSignatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _creatorSignatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _creatorSignatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _creatorSignatureTextBox.Text = "";
                return;
            }

            var item = _creatorSignatureListView.SelectedItem as string;
            if (item == null) return;

            _creatorSignatureTextBox.Text = item;
        }

        private void _creatorSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _creatorSignatureListView.SelectedItems;

            _creatorSignatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _creatorSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _creatorSignatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            _creatorSignatureListViewPasteMenuItem.IsEnabled = Clipboard.GetText().Split('\r', '\n').Any(n => Signature.HasSignature(n));
        }

        private void _creatorSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _creatorSignatureDeleteButton_Click(null, null);
        }

        private void _creatorSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _creatorSignatureListViewCopyMenuItem_Click(null, null);
            _creatorSignatureDeleteButton_Click(null, null);
        }

        private void _creatorSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _creatorSignatureListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _creatorSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Signature.HasSignature(item)) continue;

                    if (_creatorSignatureListViewItemCollection.Contains(item)) continue;
                    _creatorSignatureListViewItemCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _creatorSignatureTextBox.Text = "";
            _creatorSignatureListView.SelectedIndex = _creatorSignatureListViewItemCollection.Count - 1;

            _creatorSignatureListViewUpdate();
        }

        private void _creatorSignatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creatorSignatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _creatorSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _creatorSignatureListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _creatorSignatureListViewUpdate();
        }

        private void _creatorSignatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creatorSignatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _creatorSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _creatorSignatureListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _creatorSignatureListViewUpdate();
        }

        private void _creatorSignatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_creatorSignatureTextBox.Text) || !Signature.HasSignature(_creatorSignatureTextBox.Text)) return;

            var item = _creatorSignatureTextBox.Text;

            if (_creatorSignatureListViewItemCollection.Contains(item)) return;
            _creatorSignatureListViewItemCollection.Add(item);

            _creatorSignatureTextBox.Text = "";
            _creatorSignatureListView.SelectedIndex = _creatorSignatureListViewItemCollection.Count - 1;

            _creatorSignatureListViewUpdate();
        }

        private void _creatorSignatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _creatorSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _creatorSignatureTextBox.Text = "";

            foreach (var item in _creatorSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _creatorSignatureListViewItemCollection.Remove(item);
            }

            _creatorSignatureListView.SelectedIndex = selectIndex;
            _creatorSignatureListViewUpdate();
        }

        #endregion

        #region _managerSignatureListView

        private void _managerSignatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _managerSignatureAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _managerSignatureListViewUpdate()
        {
            _managerSignatureListView_SelectionChanged(this, null);
        }

        private void _managerSignatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _managerSignatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _managerSignatureUpButton.IsEnabled = false;
                    _managerSignatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _managerSignatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _managerSignatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _managerSignatureListViewItemCollection.Count - 1)
                    {
                        _managerSignatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _managerSignatureDownButton.IsEnabled = true;
                    }
                }

                _managerSignatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _managerSignatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _managerSignatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _managerSignatureTextBox.Text = "";
                return;
            }

            var item = _managerSignatureListView.SelectedItem as string;
            if (item == null) return;

            _managerSignatureTextBox.Text = item;
        }

        private void _managerSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _managerSignatureListView.SelectedItems;

            _managerSignatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _managerSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _managerSignatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            _managerSignatureListViewPasteMenuItem.IsEnabled = Clipboard.GetText().Split('\r', '\n').Any(n => Signature.HasSignature(n));
        }

        private void _managerSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _managerSignatureDeleteButton_Click(null, null);
        }

        private void _managerSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _managerSignatureListViewCopyMenuItem_Click(null, null);
            _managerSignatureDeleteButton_Click(null, null);
        }

        private void _managerSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _managerSignatureListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _managerSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Signature.HasSignature(item)) continue;

                    if (_managerSignatureListViewItemCollection.Contains(item)) continue;
                    _managerSignatureListViewItemCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _managerSignatureTextBox.Text = "";
            _managerSignatureListView.SelectedIndex = _managerSignatureListViewItemCollection.Count - 1;

            _managerSignatureListViewUpdate();
        }

        private void _managerSignatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _managerSignatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _managerSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _managerSignatureListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _managerSignatureListViewUpdate();
        }

        private void _managerSignatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _managerSignatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _managerSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _managerSignatureListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _managerSignatureListViewUpdate();
        }

        private void _managerSignatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_managerSignatureTextBox.Text) || !Signature.HasSignature(_managerSignatureTextBox.Text)) return;

            var item = _managerSignatureTextBox.Text;

            if (_managerSignatureListViewItemCollection.Contains(item)) return;
            _managerSignatureListViewItemCollection.Add(item);

            _managerSignatureTextBox.Text = "";
            _managerSignatureListView.SelectedIndex = _managerSignatureListViewItemCollection.Count - 1;

            _managerSignatureListViewUpdate();
        }

        private void _managerSignatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _managerSignatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _managerSignatureTextBox.Text = "";

            foreach (var item in _managerSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _managerSignatureListViewItemCollection.Remove(item);
            }

            _managerSignatureListView.SelectedIndex = selectIndex;
            _managerSignatureListViewUpdate();
        }

        #endregion

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_creatorsTabItem.IsSelected)
            {
                _creatorSignatureListViewDeleteMenuItem_Click(null, null);
            }
            else if (_managersTabItem.IsSelected)
            {
                _managerSignatureListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_creatorsTabItem.IsSelected)
            {
                _creatorSignatureListViewCopyMenuItem_Click(null, null);
            }
            else if (_managersTabItem.IsSelected)
            {
                _managerSignatureListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_creatorsTabItem.IsSelected)
            {
                _creatorSignatureListViewCutMenuItem_Click(null, null);
            }
            else if (_managersTabItem.IsSelected)
            {
                _managerSignatureListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_creatorsTabItem.IsSelected)
            {
                _creatorSignatureListViewPasteMenuItem_Click(null, null);
            }
            else if (_managersTabItem.IsSelected)
            {
                _managerSignatureListViewPasteMenuItem_Click(null, null);
            }
        }

        private void _uploadButton_Click(object sender, RoutedEventArgs e)
        {
            this.OnUploadEvent();
        }
    }
}
