using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Library.Net.Lair;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for TrustSignaturesPreviewWindow.xaml
    /// </summary>
    partial class TrustSignaturesPreviewWindow : Window
    {
        SignatureTreeViewItem _treeViewItem;

        public TrustSignaturesPreviewWindow(SignatureTreeItem signatureTreeItem)
        {
            InitializeComponent();

            if (signatureTreeItem != null)
            {
                _treeViewItem = new SignatureTreeViewItem(signatureTreeItem);
                _signatureTreeView.Items.Add(_treeViewItem);

                try
                {
                    _treeViewItem.IsSelected = true;
                }
                catch (Exception)
                {

                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        #region _signatureTreeView

        private void _signatureTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _signatureTreeView.SelectedItem as SignatureTreeViewItem;
            if (selectTreeViewItem == null) return;

            _trustSignatureListView.Items.Clear();
            _trustSignatureListView.Items.AddRange(selectTreeViewItem.Value.SectionProfile.TrustSignatures);

            _wikiListView.Items.Clear();
            _wikiListView.Items.AddRange(selectTreeViewItem.Value.SectionProfile.Wikis);

            _chatListView.Items.Clear();
            _chatListView.Items.AddRange(selectTreeViewItem.Value.SectionProfile.Chats);

            _commentTextBox.Text = selectTreeViewItem.Value.SectionProfile.Comment;
        }

        private void _signatureTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _signatureTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var signatureTreeViewItem = _signatureTreeView.SelectedItem as SignatureTreeViewItem;
            if (signatureTreeViewItem == null) return;

            Clipboard.SetText(signatureTreeViewItem.Value.SectionProfile.Signature);
        }

        #endregion

        #region _trustSignature

        private void _trustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _trustSignatureListView.SelectedItems;

            _trustSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _trustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _wikiListView

        private void _wikiListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _wikiListView.SelectedItems;

            _wikiListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _wikiListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _wikiListView.SelectedItems.OfType<Wiki>())
            {
                sb.AppendLine(LairConverter.ToWikiString(item, null));
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _chatListView

        private void _chatListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _chatListView.SelectedItems;

            _chatListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _chatListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _chatListView.SelectedItems.OfType<Chat>())
            {
                sb.AppendLine(LairConverter.ToChatString(item, null));
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
