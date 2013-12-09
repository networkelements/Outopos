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

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for SectionTreeItemEditWindow.xaml
    /// </summary>
    partial class SectionTreeItemEditWindow : Window
    {
        private SectionTreeItem _sectionTreeItem;
        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private string _uploadSignature;

        public SectionTreeItemEditWindow(SectionTreeItem sectionTreeItem, LairManager lairManager, BufferManager bufferManager)
        {
            _sectionTreeItem = sectionTreeItem;
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            if (digitalSignatureCollection.Count > 0) _signatureComboBox.SelectedIndex = 1;

            lock (_sectionTreeItem.ThisLock)
            {
                _tagTextBox.Text = MessageConverter.ToTagString(_sectionTreeItem.Tag);
                _sectionLeaderSignatureTextBox.Text = _sectionTreeItem.LeaderSignature;
                _uploadSignature = _sectionTreeItem.UploadSignature;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int index = 0; index < Settings.Instance.Global_DigitalSignatureCollection.Count; index++)
            {
                if (Settings.Instance.Global_DigitalSignatureCollection[index].ToString() == _uploadSignature)
                {
                    _signatureComboBox.SelectedIndex = index + 1;

                    break;
                }
            }

            this.Check();

            WindowPosition.Move(this);
        }

        private void Check()
        {
            _okButton.IsEnabled = _signatureComboBox.SelectedIndex != 0 && !string.IsNullOrWhiteSpace(_sectionLeaderSignatureTextBox.Text);
        }

        private void _signatureComboBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            if (digitalSignatureComboBoxItem == null) return;

            Clipboard.SetText(digitalSignatureComboBoxItem.Value.ToString());
        }

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Check();
        }

        private void _sectionLeaderSignatureTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Check();
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            lock (_sectionTreeItem.ThisLock)
            {
                _sectionTreeItem.LeaderSignature = Signature.HasSignature(_sectionLeaderSignatureTextBox.Text) ? _sectionLeaderSignatureTextBox.Text : null;
                _sectionTreeItem.UploadSignature = (digitalSignature == null) ? null : digitalSignature.ToString();
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
