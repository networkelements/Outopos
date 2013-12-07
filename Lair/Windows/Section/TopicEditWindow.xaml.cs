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
using System.Threading;
using Library;
using System.Windows.Threading;

namespace Lair.Windows
{
    /// <summary>
    /// TopicEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class TopicEditWindow : Window
    {
        private Chat _chat;
        private DigitalSignature _digitalSignature;
        private LairManager _lairManager;

        private Thread _checkThread;

        private volatile bool _refresh = true;

        public TopicEditWindow(Chat chat, string content, DigitalSignature digitalSignature, LairManager lairManager)
        {
            _chat = chat;
            _digitalSignature = digitalSignature;
            _lairManager = lairManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            this.Title = string.Format(LanguagesManager.Instance.TopicEditWindow_Title, chat.Name);

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

            _checkThread = new Thread(new ThreadStart(this.Check));
            _checkThread.Priority = ThreadPriority.Highest;
            _checkThread.IsBackground = true;
            _checkThread.Name = "TopicEditWindow_CheckThread";
            _checkThread.Start();
        }

        private void Check()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (!_refresh) continue;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        _refresh = false;

                        string comment = "";

                        if (!string.IsNullOrWhiteSpace(_commentTextBox.Text))
                        {
                            comment = _commentTextBox.Text;
                            comment = comment.Replace("\r\n", "\n");
                            comment = comment.Replace("\r", "\n");
                        }

                        if (comment.Length > ChatTopicContent.MaxCommentLength)
                        {
                            _okButton.IsEnabled = false;
                        }
                        else
                        {
                            _okButton.IsEnabled = true;
                        }

                        if (_commentTextBox.Text != null)
                        {
                            _countLabel.Content = string.Format("{0} / {1}", _commentTextBox.Text.Length, ChatTopicContent.MaxCommentLength);
                        }
                    }));
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
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

                string comment = "";

                if (!string.IsNullOrWhiteSpace(_commentTextBox.Text))
                {
                    comment = _commentTextBox.Text;
                    comment = comment.Replace("\r\n", "\n");
                    comment = comment.Replace("\r", "\n");
                }

                if (comment.Length > ChatTopicContent.MaxCommentLength)
                {
                    comment = comment.Substring(0, ChatTopicContent.MaxCommentLength);
                }

                RichTextBoxHelper.TopicToRichTextBox(_richTextBox, _chat, DateTime.UtcNow, _digitalSignature.ToString(), comment);
            }
        }

        private void _commentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _refresh = true;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            if (_refresh) return;

            string comment = null;

            if (!string.IsNullOrWhiteSpace(_commentTextBox.Text))
            {
                comment = _commentTextBox.Text;
                comment = comment.Replace("\r\n", "\n");
                comment = comment.Replace("\r", "\n");
            }

            _lairManager.UploadChatTopic(_chat, comment, _digitalSignature);

            this.Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
