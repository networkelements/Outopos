using System;
using System.Collections.Generic;
using System.IO;
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
using Library.Net;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// MessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MessageEditWindow : Window
    {
        private Message _message;
        private LairManager _lairManager;

        public MessageEditWindow(ref Message message, LairManager lairManager)
        {
            _message = message;
            _lairManager = lairManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _commentTextBox.Text = _message.Content;

            _signatureComboBox.ItemsSource = digitalSignatureCollection;

            var index = Settings.Instance.Global_DigitalSignatureCollection.IndexOf(Settings.Instance.Global_UploadDigitalSignature);
            _signatureComboBox.SelectedIndex = index + 1;
        }

        private void _commentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_commentTextBox.Text);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            Settings.Instance.Global_UploadDigitalSignature = digitalSignature;

            lock (_message.ThisLock)
            {
                _message.Content = comment;
                _message.CreationTime = DateTime.UtcNow;

                if (digitalSignature == null)
                {
                    _message.CreateCertificate(null);
                }
                else
                {
                    _message.CreateCertificate(digitalSignature);
                }
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

    class DigitalSignatureComboBoxItem : ComboBoxItem
    {
        private DigitalSignature _value;

        public DigitalSignatureComboBoxItem()
        {

        }

        public DigitalSignatureComboBoxItem(DigitalSignature digitalSignature)
        {
            this.Value = digitalSignature;
        }

        public void Update()
        {
            this.Content = MessageConverter.ToSignatureString(this.Value);
        }

        public DigitalSignature Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }
    }
}
