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
using System.Security.Cryptography;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for CreatorControl.xaml
    /// </summary>
    partial class CreatorControl : UserControl
    {
        private BufferManager _bufferManager;

        private CreatorInfo _creatorInfo;

        private ObservableCollection<Channel> _channelListViewItemCollection;

        public event UploadEventHandler UploadEvent;

        public CreatorControl(CreatorInfo creatorInfo, BufferManager bufferManager)
        {
            _creatorInfo = creatorInfo.DeepClone();
            _bufferManager = bufferManager;

            _channelListViewItemCollection = new ObservableCollection<Channel>(_creatorInfo.Channels);

            InitializeComponent();

            _channelListView.ItemsSource = _channelListViewItemCollection;
            _commentTextBox.Text = _creatorInfo.Comment;
        }

        public CreatorInfo CreatorInfo
        {
            get
            {
                var creatorInfo = new CreatorInfo();

                creatorInfo.Channels.AddRange(_channelListViewItemCollection);
                creatorInfo.Comment = _commentTextBox.Text;

                return creatorInfo;
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

        #region _channelListView

        private void _channelTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _channelAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _channelListViewUpdate()
        {
            _channelListView_SelectionChanged(this, null);
        }

        private void _channelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _channelListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _channelUpButton.IsEnabled = false;
                    _channelDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _channelUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _channelUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _channelListViewItemCollection.Count - 1)
                    {
                        _channelDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _channelDownButton.IsEnabled = true;
                    }
                }

                _channelListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _channelListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void _channelListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _channelListView.SelectedItems;

            _channelListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _channelListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _channelListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            _channelListViewPasteMenuItem.IsEnabled = (Clipboard.GetChannels().Count() != 0);
        }

        private void _channelListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _channelDeleteButton_Click(null, null);
        }

        private void _channelListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _channelListViewCopyMenuItem_Click(null, null);
            _channelDeleteButton_Click(null, null);
        }

        private void _channelListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetChannels(_channelListView.SelectedItems.OfType<Channel>());
        }

        private void _channelListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetChannels())
            {
                try
                {
                    if (_channelListViewItemCollection.Contains(item)) continue;
                    _channelListViewItemCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _channelTextBox.Text = "";
            _channelListView.SelectedIndex = _channelListViewItemCollection.Count - 1;

            _channelListViewUpdate();
        }

        private void _channelUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _channelListView.SelectedItem as Channel;
            if (item == null) return;

            var selectIndex = _channelListView.SelectedIndex;
            if (selectIndex == -1) return;

            _channelListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _channelListViewUpdate();
        }

        private void _channelDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _channelListView.SelectedItem as Channel;
            if (item == null) return;

            var selectIndex = _channelListView.SelectedIndex;
            if (selectIndex == -1) return;

            _channelListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _channelListViewUpdate();
        }

        private void _channelAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_channelTextBox.Text)) return;

            byte[] buffer = new byte[64];
            (new RNGCryptoServiceProvider()).GetBytes(buffer);

            var item = new Channel(buffer, _channelTextBox.Text);

            if (_channelListViewItemCollection.Contains(item)) return;
            _channelListViewItemCollection.Add(item);

            _channelTextBox.Text = "";
            _channelListView.SelectedIndex = _channelListViewItemCollection.Count - 1;

            _channelListViewUpdate();
        }

        private void _channelDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _channelListView.SelectedIndex;
            if (selectIndex == -1) return;

            _channelTextBox.Text = "";

            foreach (var item in _channelListView.SelectedItems.OfType<Channel>().ToArray())
            {
                _channelListViewItemCollection.Remove(item);
            }

            _channelListView.SelectedIndex = selectIndex;
            _channelListViewUpdate();
        }

        #endregion

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_channelsTabItem.IsSelected)
            {
                _channelListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_channelsTabItem.IsSelected)
            {
                _channelListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_channelsTabItem.IsSelected)
            {
                _channelListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_channelsTabItem.IsSelected)
            {
                _channelListViewPasteMenuItem_Click(null, null);
            }
        }

        private void _uploadButton_Click(object sender, RoutedEventArgs e)
        {
            this.OnUploadEvent();
        }
    }
}
