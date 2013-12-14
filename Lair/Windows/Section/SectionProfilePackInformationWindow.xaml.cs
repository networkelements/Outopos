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
using Lair.Properties;
using Library;
using Library.Net.Lair;
using Library.Security;
using System.ComponentModel;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for SectionProfilePackInformation.xaml
    /// </summary>
    partial class SectionProfileInformationWindow : Window
    {
        private SectionProfilePack _sectionProfilePack;

        private ObservableCollectionEx<string> _trustSignatureCollection = new ObservableCollectionEx<string>();
        private ObservableCollectionEx<Archive> _archiveCollection;
        private ObservableCollectionEx<Chat> _chatCollection;

        public SectionProfileInformationWindow(SectionProfilePack sectionProfilePack)
        {
            _sectionProfilePack = sectionProfilePack;

            InitializeComponent();

            {
                _signatureTextBox.Text = _sectionProfilePack.Header.Certificate.ToString();
                _trustSignatureCollection.AddRange(sectionProfilePack.Content.TrustSignatures);
                _archiveCollection = new ObservableCollectionEx<Archive>(sectionProfilePack.Content.Archives);
                _chatCollection = new ObservableCollectionEx<Chat>(sectionProfilePack.Content.Chats);
            }

            _trustSignatureListView.ItemsSource = _trustSignatureCollection;
            _archiveListView.ItemsSource = _archiveCollection;
            _chatListView.ItemsSource = _chatCollection;

            this.Sort();
        }

        private void Sort()
        {
            _trustSignatureListView.Items.SortDescriptions.Clear();
            _trustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

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

        #region _archiveListView

        private void _archiveListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _archiveListView.SelectedItems;

            _archiveListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _archiveListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _archiveListView.SelectedItems.OfType<Archive>())
            {
                sb.AppendLine(LairConverter.ToArchiveString(item, null));
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
            this.DialogResult = false;
        }
    }
}
