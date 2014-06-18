using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Outopos.Properties;
using Library;
using Library.Net.Outopos;
using Library.Security;

namespace Outopos.Windows
{
    /// <summary>
    /// Interaction logic for TrustOptionsWindow.xaml
    /// </summary>
    partial class TrustOptionsWindow : Window
    {
        private ObservableCollectionEx<string> _signatureCollection = new ObservableCollectionEx<string>();

        public TrustOptionsWindow()
        {
            InitializeComponent();

            _signatureCollection.AddRange(Settings.Instance.Global_TrustSignatures);

            _signatureListView.ItemsSource = _signatureCollection;

            this.Sort();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        private void Sort()
        {
            _signatureListView.Items.SortDescriptions.Clear();
            _signatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        #region _signatureListView

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Signature.Check(line[0]);
                }

                _signatureListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in _signatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _signatureCollection.Remove(signature);
            }
        }

        private void _signatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewCopyMenuItem_Click(null, null);
            _signatureListViewDeleteMenuItem_Click(null, null);
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var signature in _signatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(signature);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.Check(signature)) continue;

                _signatureCollection.Add(signature);
            }
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Settings.Instance.Global_TrustSignatures.ThisLock)
            {
                Settings.Instance.Global_TrustSignatures.Clear();
                Settings.Instance.Global_TrustSignatures.AddRange(_signatureCollection);
            }

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
