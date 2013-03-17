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

namespace Lair.Windows
{
    /// <summary>
    /// TopicPreviewWindow.xaml の相互作用ロジック
    /// </summary>
    partial class TopicPreviewWindow : Window
    {
        public TopicPreviewWindow(Topic topic)
        {
            InitializeComponent();

            if (topic != null && topic.Content != null) RichTextBoxHelper.SetRichTextBox(_richTextBox, topic);
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
