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
using Outopos.Properties;
using Library.Net;
using Library.Net.Outopos;
using Library.Security;
using Library.Collections;

namespace Outopos.Windows
{
    /// <summary>
    /// ChatMessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ChatMessageEditWindow : Window
    {
        private Chat _chat;
        private List<Anchor> _anchors = new List<Anchor>();
        private DigitalSignature _digitalSignature;

        private bool _isTrust;

        private OutoposManager _outoposManager;

        public ChatMessageEditWindow(Chat chat, string comment, IEnumerable<Anchor> anchors, DigitalSignature digitalSignature, bool isTrust, OutoposManager outoposManager)
        {
            _chat = chat;
            if (anchors != null) _anchors.AddRange(anchors);
            _digitalSignature = digitalSignature;

            _isTrust = isTrust;

            _outoposManager = outoposManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _commentTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            _commentTextBox.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _commentTextBox.Text = comment;

            _commentTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            _commentTextBox.FontSize = Settings.Instance.Global_Fonts_MessageFontSize;

            _commentTextBox_TextChanged(null, null);
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
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
                }
                else
                {
                    string comment = _commentTextBox.Text;

                    if (comment.Length > ChatMessage.MaxCommentLength)
                    {
                        comment = comment.Substring(0, ChatMessage.MaxCommentLength);
                    }

                    RichTextBoxHelper.SetRichTextBox(_richTextBox, _chat, _digitalSignature.ToString(), DateTime.UtcNow, comment, _anchors, _isTrust);
                }
            }
        }

        private void _commentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_commentTextBox.Text) || _commentTextBox.Text.Length > ChatMessage.MaxCommentLength)
            {
                _okButton.IsEnabled = false;
            }
            else
            {
                _okButton.IsEnabled = true;
            }

            if (_commentTextBox.Text != null)
            {
                _countLabel.Content = string.Format("{0} / {1}", _commentTextBox.Text.Length, ChatMessage.MaxCommentLength);
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            int limit;

            if (!_isTrust) limit = (Trust.GetLimit() + 1);
            else limit = 0;

            _outoposManager.UploadChatMessage(_chat, _commentTextBox.Text, _anchors, limit, new TimeSpan(0, 30, 0), _digitalSignature);

            this.Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
