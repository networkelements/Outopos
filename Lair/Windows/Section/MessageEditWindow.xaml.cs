using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using Library.Collections;

namespace Lair.Windows
{
    /// <summary>
    /// MessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MessageEditWindow : Window
    {
        private Channel _channel;
        private List<Message> _responsMessages = new List<Message>();
        private DigitalSignature _digitalSignature;
        private LairManager _lairManager;

        public MessageEditWindow(Channel channel, string content, IEnumerable<Message> responsMessages, DigitalSignature digitalSignature, LairManager lairManager)
        {
            _channel = channel;
            if (responsMessages != null) _responsMessages.AddRange(responsMessages);
            _digitalSignature = digitalSignature;
            _lairManager = lairManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            this.Title = string.Format(LanguagesManager.Instance.MessageEditWindow_Title, channel.Name);

            _commentTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            _commentTextBox.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _commentTextBox.Text = content;

            _commentTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            _commentTextBox.FontSize = Settings.Instance.Global_Fonts_MessageFontSize;

            //_commentTextBox.CaretIndex = _commentTextBox.Text.Length;
            //_commentTextBox.ScrollToEnd();

            _commentTextBox_TextChanged(null, null);
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void RichTextBoxEx_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tabControl.SelectedItem == _previewTabItem)
            {
                if (string.IsNullOrWhiteSpace(_commentTextBox.Text))
                {
                    _richTextBox.Document = new FlowDocument();

                    return;
                }

                string comment = _commentTextBox.Text;

                if (comment.Length > Message.MaxContentLength)
                {
                    comment = comment.Substring(0, Message.MaxContentLength);
                }

                var m = new Message(_channel, comment, _responsMessages.Select(n => new Key(n.GetHash(HashAlgorithm.Sha512), HashAlgorithm.Sha512)), _digitalSignature);

                RichTextBoxHelper.SetRichTextBox(_richTextBox, m);

                _richTextBox.MaxHeight = double.PositiveInfinity;
            }
        }

        private void _commentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_commentTextBox.Text) || _commentTextBox.Text.Length > Message.MaxContentLength)
            {
                _okButton.IsEnabled = false;
            }
            else
            {
                _okButton.IsEnabled = true;
            }

            if (_commentTextBox.Text != null)
            {
                _countLabel.Content = string.Format("{0} / {1}", _commentTextBox.Text.Length, Message.MaxContentLength);
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(_channel, _commentTextBox.Text, _responsMessages.Select(n => new Key(n.GetHash(HashAlgorithm.Sha512), HashAlgorithm.Sha512)), _digitalSignature);

            {
                LockedHashSet<Message> messages;

                if (!Settings.Instance.Global_LockedMessageItems.TryGetValue(_channel, out messages))
                {
                    messages = new LockedHashSet<Message>();
                    Settings.Instance.Global_LockedMessageItems[_channel] = messages;
                }

                messages.Add(message);
            }

            _lairManager.Upload(message);

            this.Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
