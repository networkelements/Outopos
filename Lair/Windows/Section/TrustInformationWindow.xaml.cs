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

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for TrustInformationWindow.xaml
    /// </summary>
    partial class TrustInformationWindow : Window
    {
        public TrustInformationWindow(SignatureTreeItem signatureTreeItem)
        {
            InitializeComponent();

            _signatureTreeView.Items.Add(new SignatureTreeViewItem(signatureTreeItem));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        #region _signatureTreeView

        private void _signatureTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _signatureTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var signatureTreeViewItem = _signatureTreeView.SelectedItem as SignatureTreeViewItem;
            if (signatureTreeViewItem == null) return;

            Clipboard.SetText(signatureTreeViewItem.Value.SectionProfilePack.Header.Certificate.ToString());
        }

        private void _sectionTreeViewItemProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var signatureTreeViewItem = _signatureTreeView.SelectedItem as SignatureTreeViewItem;
            if (signatureTreeViewItem == null) return;

            SectionProfileWindow sectionProfilePackInformationWindow = new SectionProfileWindow(signatureTreeViewItem.Value.SectionProfilePack);
            sectionProfilePackInformationWindow.Owner = this;
            sectionProfilePackInformationWindow.ShowDialog();
        }

        #endregion
    }
}
