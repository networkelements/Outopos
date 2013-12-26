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
using L = Library.Net.Lair;

namespace Lair.Windows
{
    /// <summary>
    /// SectionMessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SectionMessageEditWindow : Window
    {
        private L.Section _section;
        private SectionMessage _responsMessage;
        private ExchangePublicKey _exchangePublicKey;
        private DigitalSignature _digitalSignature;
        private LairManager _lairManager;
        private SectionMessage _sectionMessage;

        public SectionMessageEditWindow(L.Section section, string content, SectionMessage responsMessage, ExchangePublicKey exchangePublicKey, DigitalSignature digitalSignature, LairManager lairManager)
        {
            _section = section;
            _responsMessage = responsMessage;
            _exchangePublicKey = exchangePublicKey;
            _digitalSignature = digitalSignature;
            _lairManager = lairManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

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

            _commentTextBox_TextChanged(null, null);
        }

        public SectionMessage SectionMessage
        {
            get
            {
                return _sectionMessage;
            }
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

                if (comment.Length > SectionMessage.MaxCommentLength)
                {
                    comment = comment.Substring(0, SectionMessage.MaxCommentLength);
                }

                Anchor anchor = null;

                if (_responsMessage != null)
                {
                    anchor = new Anchor(_responsMessage.Signature, _responsMessage.CreationTime);
                }

                RichTextBoxHelper.SetRichTextBox(_richTextBox, _section, _digitalSignature.ToString(), DateTime.UtcNow, comment, anchor, true);

                _richTextBox.MaxHeight = double.PositiveInfinity;
            }
        }

        private void _commentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_commentTextBox.Text) || _commentTextBox.Text.Length > SectionMessage.MaxCommentLength)
            {
                _okButton.IsEnabled = false;
            }
            else
            {
                _okButton.IsEnabled = true;
            }

            if (_commentTextBox.Text != null)
            {
                _countLabel.Content = string.Format("{0} / {1}", _commentTextBox.Text.Length, SectionMessage.MaxCommentLength);
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            Anchor anchor = null;

            if (_responsMessage != null)
            {
                anchor = new Anchor(_responsMessage.Signature, _responsMessage.CreationTime);
            }

            _sectionMessage = _lairManager.UploadSectionMessage(_section, _commentTextBox.Text, anchor, _exchangePublicKey, _digitalSignature);

            this.Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
