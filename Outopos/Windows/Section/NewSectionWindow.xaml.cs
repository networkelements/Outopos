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
using Outopos.Properties;
using Library;
using Library.Net.Outopos;
using Library.Security;
using l = Library.Net.Outopos;

namespace Outopos.Windows
{
    /// <summary>
    /// Interaction logic for NewSectionWindow.xaml
    /// </summary>
    partial class NewSectionWindow : Window
    {
        private string _leaderSignature;
        private string _sectionName;

        public NewSectionWindow()
        {
            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _sectionNameTextBox.MaxLength = l.Section.MaxNameLength;

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            _signatureComboBox.SelectedIndex = 0;

            _sectionNameTextBox_TextChanged(null, null);
        }

        public string LeaderSignature
        {
            get
            {
                return _leaderSignature;
            }
            set
            {
                _leaderSignature = value;
            }
        }

        public string SectionName
        {
            get
            {
                return _sectionName;
            }
            set
            {
                _sectionName = value;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;

            WindowPosition.Move(this);
        }

        private void _signatureComboBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            if (digitalSignatureComboBoxItem == null) return;

            Clipboard.SetText(digitalSignatureComboBoxItem.Value.ToString());
        }

        private void _sectionNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_sectionNameTextBox.Text);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _leaderSignature = digitalSignature.ToString();
            _sectionName = _sectionNameTextBox.Text;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
