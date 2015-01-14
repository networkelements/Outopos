using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Outopos;
using Outopos.Properties;
using Library;
using Library.Collections;
using Library.Net.Outopos;
using Library.Security;
using A = Library.Net.Amoeba;
using System.Windows.Documents;

namespace Outopos.Windows
{
    /// <summary>
    /// Interaction logic for TrustControl.xaml
    /// </summary>
    partial class TrustControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private OutoposManager _outoposManager;
        private BufferManager _bufferManager;

        private static Random _random = new Random();

        public TrustControl(OutoposManager outoposManager, BufferManager bufferManager)
        {
            _outoposManager = outoposManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            this.Update();
        }

        public void Update()
        {
            _treeView.Items.Clear();

            _trustSignatureListView.Items.Clear();
            _wikiListView.Items.Clear();
            _chatListView.Items.Clear();

            foreach (var leaderSignature in Settings.Instance.Global_TrustSignatures)
            {
                var item = this.GetSignatureTreeViewItem(leaderSignature);
                if (item == null) continue;

                _treeView.Items.Add(new SignatureTreeViewItem(item));
            }
        }

        private SignatureTreeItem GetSignatureTreeViewItem(string leaderSignature)
        {
            List<SignatureTreeItem> workSignatureTreeItems = new List<SignatureTreeItem>();

            HashSet<string> checkedSignatures = new HashSet<string>();

            {
                Profile leaderProfile;
                if (!Settings.Instance.Global_Profiles.TryGetValue(leaderSignature, out leaderProfile)) return null;

                workSignatureTreeItems.Add(new SignatureTreeItem(leaderProfile));
                checkedSignatures.Add(leaderSignature);
            }

            List<SignatureTreeItem> checkedSignatureTreeItems = new List<SignatureTreeItem>();

            for (int i = 0; workSignatureTreeItems.Count != 0 && i < 256; i++)
            {
                var sortList = workSignatureTreeItems.SelectMany(n => n.Profile.TrustSignatures).ToList();
                sortList.Sort((x, y) => x.CompareTo(y));

                checkedSignatureTreeItems.AddRange(workSignatureTreeItems);
                workSignatureTreeItems.Clear();

                foreach (var trustSignature in sortList)
                {
                    if (checkedSignatures.Contains(trustSignature)) continue;

                    Profile tempProfile;
                    if (!Settings.Instance.Global_Profiles.TryGetValue(trustSignature, out tempProfile)) continue;

                    var tempItem = new SignatureTreeItem(tempProfile);
                    workSignatureTreeItems.Add(tempItem);

                    var targetItem = checkedSignatureTreeItems.FirstOrDefault(n => n.Profile.TrustSignatures.Contains(trustSignature));
                    targetItem.Children.Add(tempItem);

                    checkedSignatures.Add(trustSignature);
                }
            }

            return checkedSignatureTreeItems[0];
        }

        #region _treeView

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SignatureTreeViewItem;
            if (selectTreeViewItem == null) return;

            _trustSignatureListView.Items.Clear();
            _trustSignatureListView.Items.AddRange(selectTreeViewItem.Value.Profile.TrustSignatures);

            _wikiListView.Items.Clear();
            _wikiListView.Items.AddRange(selectTreeViewItem.Value.Profile.Wikis);

            _chatListView.Items.Clear();
            _chatListView.Items.AddRange(selectTreeViewItem.Value.Profile.Chats);
        }

        private void _treeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var signatureTreeViewItem = _treeView.SelectedItem as SignatureTreeViewItem;
            if (signatureTreeViewItem == null) return;

            Clipboard.SetText(signatureTreeViewItem.Value.Profile.Certificate.ToString());
        }

        #endregion

        #region _trustSignature

        private void _trustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _trustSignatureListView.SelectedItems;

            _trustSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
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

            _wikiListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _wikiListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _wikiListView.SelectedItems.OfType<Wiki>())
            {
                sb.AppendLine(OutoposConverter.ToWikiString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _chatListView

        private void _chatListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _chatListView.SelectedItems;

            _chatListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _chatListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _chatListView.SelectedItems.OfType<Chat>())
            {
                sb.AppendLine(OutoposConverter.ToChatString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }
    }
}
