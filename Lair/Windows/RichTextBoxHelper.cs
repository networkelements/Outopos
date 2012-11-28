using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Lair.Properties;
using Library;
using Library.Net.Lair;

namespace Lair.Windows
{
    delegate void LinkClickEventHandler(object sender, string link);
    delegate void SeedClickEventHandler(object sender, Library.Net.Amoeba.Seed seed);
    delegate void ChannelClickEventHandler(object sender, Channel channel);
    delegate double GetMaxHeightEventHandler(object sender);

    class RichTextBoxHelper : DependencyObject
    {
        public static event LinkClickEventHandler LinkClickEvent;
        public static event SeedClickEventHandler SeedClickEvent;
        public static event ChannelClickEventHandler ChannelClickEvent;
        public static GetMaxHeightEventHandler GetMaxHeightEvent;

        private static Regex _urlRegex = new Regex(@"^(?<start>.*?)(?<url>http(s)?://(\S)+)(?<end>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _titleRegex = new Regex(@"^ - (.*) - (\S*) (\S*) UTC$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Color _d = Color.FromRgb(0xDF, 0xc0, 0xDF);
        private static Color _h = Color.FromRgb(0xDF, 0xDF, 0xDF);

        public static string GetMessageToString(Message message)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (message.Certificate == null)
            {
                stringBuilder.AppendLine(string.Format(" - Anonymous - {0} UTC",
                    message.CreationTime.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            }
            else
            {
                stringBuilder.AppendLine(string.Format(" - {0} - {1} UTC",
                    MessageConverter.ToSignatureString(message.Certificate),
                    message.CreationTime.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            }

            stringBuilder.AppendLine();

            stringBuilder.Append(message.Content);

            return stringBuilder.ToString().TrimEnd('\r', '\n');
        }

        public static string GetMessageToShowString(Message message)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (message.Certificate == null)
            {
                stringBuilder.AppendLine(string.Format(" - Anonymous - {0}",
                message.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            }
            else
            {
                stringBuilder.AppendLine(string.Format(" - {0} - {1}",
                    MessageConverter.ToSignatureString(message.Certificate),
                    message.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            }

            stringBuilder.AppendLine();

            foreach (var line in message.Content.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                var text = line;

                while (text.StartsWith("> "))
                {
                    text = text.Substring(2, text.Length - 2);
                }

                try
                {
                    var match = _titleRegex.Match(text);

                    if (match.Success)
                    {
                        try
                        {
                            var dateText = match.Groups[2] + " " + match.Groups[3];
                            var creationTime = DateTime.ParseExact(dateText, "yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToLocalTime();

                            var item = string.Format(" - {0} - {1}",
                                 match.Groups[1].Value,
                                 creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo));

                            stringBuilder.AppendLine(item);
                        }
                        catch (Exception)
                        {
                            stringBuilder.AppendLine(text);
                        }
                    }
                    else
                    {
                        stringBuilder.AppendLine(text);
                    }

                    if (text.StartsWith("Seed@"))
                    {
                        var seed = Library.Net.Amoeba.AmoebaConverter.FromSeedString(text);
                        if (seed == null) throw new Exception();
                        if (!seed.VerifyCertificate()) throw new Exception();

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(seed));
                        stringBuilder.AppendLine();
                    }
                    else if (text.StartsWith("Channel@"))
                    {
                        var channel = Library.Net.Lair.LairConverter.FromChannelString(text);
                        if (channel == null) throw new Exception();

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(channel));
                        stringBuilder.AppendLine();
                    }
                }
                catch (Exception)
                {
                    stringBuilder.AppendLine(text);
                    stringBuilder.AppendLine();
                }
            }

            var ttt = stringBuilder.ToString().TrimEnd('\r', '\n');

            return stringBuilder.ToString().TrimEnd('\r', '\n');
        }

        public static Message GetDocumentMessage(DependencyObject obj)
        {
            return (Message)obj.GetValue(DocumentMessageProperty);
        }

        public static void SetDocumentMessage(DependencyObject obj, Message value)
        {
            obj.SetValue(DocumentMessageProperty, value);
        }

        public static readonly DependencyProperty DocumentMessageProperty = DependencyProperty.RegisterAttached("DocumentMessage", typeof(Message), typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata()
            {
                AffectsMeasure = true,
                AffectsArrange = true,
                AffectsParentMeasure = true,
                AffectsParentArrange = true,
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                PropertyChangedCallback = (obj, e) =>
                {
                    try
                    {
                        var richTextBox = (RichTextBox)obj;

                        var message = e.NewValue as Message;
                        if (message == null) return;

                        RichTextBoxHelper.SetRichTextBox(richTextBox, message);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            });

        public static void SetRichTextBox(RichTextBox richTextBox, Message message)
        {
            richTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            richTextBox.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            var maxHeight = Math.Max(0, RichTextBoxHelper.GetMaxHeightEvent(richTextBox));

            richTextBox.MaxHeight = maxHeight / 3 * 2;

            var fd = new EnabledFlowDocument();

            fd.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            fd.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            var p = new Paragraph();
            p.LineHeight = richTextBox.FontSize + 2;

            if (message.Certificate == null)
            {
                p.Inlines.Add(string.Format(" - Anonymous - {0}",
                    message.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            }
            else
            {
                p.Inlines.Add(string.Format(" - {0} - {1}",
                    MessageConverter.ToSignatureString(message.Certificate),
                    message.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            }

            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());

            p.Inlines.Add(RichTextBoxHelper.GetParagraph(richTextBox, message.Content, 0));

            fd.Blocks.Add(p);

            richTextBox.Document = fd;
        }

        private static Span GetParagraph(RichTextBox richTextBox, string text, int nest)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Span p = new Span();

            List<string> list = new List<string>();

            foreach (var line in text.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                var match = _titleRegex.Match(line);

                if (match.Success)
                {
                    try
                    {
                        var dateText = match.Groups[2] + " " + match.Groups[3];
                        var creationTime = DateTime.ParseExact(dateText, "yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToLocalTime();

                        var item = string.Format(" - {0} - {1}",
                             match.Groups[1].Value,
                             creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo));

                        list.Add(item);
                    }
                    catch (Exception)
                    {
                        list.Add(line);
                    }
                }
                else
                {
                    list.Add(line);
                }
            }

            foreach (var line in list)
            {
                try
                {
                    var rl = line.Trim();

                    if (rl.StartsWith("Seed@"))
                    {
                        var seed = Library.Net.Amoeba.AmoebaConverter.FromSeedString(rl);
                        if (seed == null) throw new Exception();
                        if (!seed.VerifyCertificate()) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Foreground = new SolidColorBrush(_d);
                            l.Cursor = Cursors.Hand;
                            l.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs ex) =>
                            {
                                if (RichTextBoxHelper.SeedClickEvent != null)
                                {
                                    RichTextBoxHelper.SeedClickEvent(sender, seed);
                                }

                                if (Settings.Instance.Global_SeedHistorys.Contains(seed))
                                {
                                    l.Foreground = new SolidColorBrush(_h);
                                }
                            };

                            if (Settings.Instance.Global_SeedHistorys.Contains(seed))
                            {
                                l.Foreground = new SolidColorBrush(_h);
                            }

                            {
                                Run r = new Run();
                                r.Text = rl1;

                                l.Inlines.Add(r);
                            }

                            if (!string.IsNullOrWhiteSpace(rl2))
                            {
                                Run r = new Run();
                                r.Text = rl2;
                                r.FontSize = 1;

                                l.Inlines.Add(r);
                            }

                            span.Inlines.Add(l);

                            if (!string.IsNullOrWhiteSpace(rl3))
                            {
                                Run r = new Run();
                                r.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                                r.Text = rl3;
                                r.FontSize = 0.1;
                                r.FontStretch = FontStretches.UltraCondensed;

                                span.Inlines.Add(r);
                            }

                            l.PreviewMouseRightButtonDown += (object sender, MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
                            };

                            p.Inlines.Add(span);
                        }

                        p.Inlines.Add(new LineBreak());

                        {
                            Run r = new Run();
                            r.Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            r.Text = MessageConverter.ToInfoMessage(seed);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Channel@"))
                    {
                        var channel = Library.Net.Lair.LairConverter.FromChannelString(rl);
                        if (channel == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Foreground = new SolidColorBrush(_d);
                            l.Cursor = Cursors.Hand;
                            l.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs ex) =>
                            {
                                if (RichTextBoxHelper.ChannelClickEvent != null)
                                {
                                    RichTextBoxHelper.ChannelClickEvent(sender, channel);
                                }

                                if (Settings.Instance.Global_ChannelHistorys.Contains(channel))
                                {
                                    l.Foreground = new SolidColorBrush(_h);
                                }
                            };

                            if (Settings.Instance.Global_ChannelHistorys.Contains(channel))
                            {
                                l.Foreground = new SolidColorBrush(_h);
                            }

                            {
                                Run r = new Run();
                                r.Text = rl1;

                                l.Inlines.Add(r);
                            }

                            if (!string.IsNullOrWhiteSpace(rl2))
                            {
                                Run r = new Run();
                                r.Text = rl2;
                                r.FontSize = 1;

                                l.Inlines.Add(r);
                            }

                            span.Inlines.Add(l);

                            if (!string.IsNullOrWhiteSpace(rl3))
                            {
                                Run r = new Run();
                                r.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                                r.Text = rl3;
                                r.FontSize = 0.1;
                                r.FontStretch = FontStretches.UltraCondensed;

                                span.Inlines.Add(r);
                            }

                            l.PreviewMouseRightButtonDown += (object sender, MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
                            };

                            p.Inlines.Add(span);
                        }

                        p.Inlines.Add(new LineBreak());

                        {
                            Run r = new Run();
                            r.Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            r.Text = MessageConverter.ToInfoMessage(channel);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        var line2 = line;

                        if (line2.StartsWith("> "))
                        {
                            stringBuilder.AppendLine(line2.Remove(0, 2));
                        }
                        else
                        {
                            if (stringBuilder.Length != 0)
                            {
                                var list2 = stringBuilder.ToString().TrimEnd('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                                if (list2.Length == 1)
                                {
                                    p.Inlines.Add("> " + list2[0]);
                                    p.Inlines.Add(new LineBreak());
                                }
                                else if (nest < 6)
                                {
                                    var richTextBox2 = new RichTextBox();

                                    richTextBox2.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
                                    richTextBox2.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");
                                    richTextBox2.MaxHeight = Math.Max(0, RichTextBoxHelper.GetMaxHeightEvent(richTextBox));

                                    var fd = new EnabledFlowDocument();
                                    fd.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
                                    fd.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

                                    var p2 = new Paragraph();
                                    p2.LineHeight = richTextBox.FontSize + 2;

                                    p2.Inlines.Add(RichTextBoxHelper.GetParagraph(richTextBox2, stringBuilder.ToString(), nest + 1));

                                    fd.Blocks.Add(p2);
                                    richTextBox2.Document = fd;

                                    var grid = new Grid();
                                    grid.Children.Add(richTextBox2);

                                    richTextBox2.SelectAll();
                                    var header = richTextBox2.Selection.Text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
                                    richTextBox2.Selection.Select(richTextBox2.Document.ContentStart, richTextBox2.Document.ContentStart);

                                    var label = new Label() { Content = header };

                                    var binding = new Binding("ActualWidth")
                                    {
                                        Source = richTextBox
                                    };

                                    label.SetBinding(Label.WidthProperty, binding);

                                    var expander = new Expander()
                                    {
                                        IsEnabled = true,
                                        IsExpanded = false,
                                        Header = label,
                                        Content = grid,
                                        Margin = new Thickness(2, 6, 2, 6),
                                    };

                                    expander.Expanded += (object sender, RoutedEventArgs e) =>
                                    {
                                        expander.Header = " ";

                                        e.Handled = true;
                                    };

                                    expander.Collapsed += (object sender, RoutedEventArgs e) =>
                                    {
                                        expander.Header = label;

                                        e.Handled = true;
                                    };

                                    p.Inlines.Add(new InlineUIContainer(expander));
                                }
                            }

                            stringBuilder.Clear();

                            for (; ; )
                            {
                                Match match = _urlRegex.Match(line2);

                                if (match.Success)
                                {
                                    p.Inlines.Add(match.Groups["start"].Value);

                                    var url = match.Groups["url"].Value;
                                    Hyperlink l = new Hyperlink();
                                    l.Foreground = new SolidColorBrush(_d);
                                    l.Inlines.Add(url);
                                    l.Cursor = Cursors.Hand;
                                    l.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs ex) =>
                                    {
                                        if (RichTextBoxHelper.LinkClickEvent != null)
                                        {
                                            RichTextBoxHelper.LinkClickEvent(sender, url);
                                        }

                                        if (Settings.Instance.Global_UrlHistorys.Contains(url))
                                        {
                                            l.Foreground = new SolidColorBrush(_h);
                                        }
                                    };
                                    l.PreviewMouseRightButtonDown += (object sender, MouseButtonEventArgs ex) =>
                                    {
                                        richTextBox.Selection.Select(l.ContentStart, l.ContentEnd);
                                    };

                                    if (Settings.Instance.Global_UrlHistorys.Contains(url))
                                    {
                                        l.Foreground = new SolidColorBrush(_h);
                                    }

                                    p.Inlines.Add(l);

                                    line2 = match.Groups["end"].Value;
                                }
                                else
                                {
                                    p.Inlines.Add(line2);
                                    p.Inlines.Add(new LineBreak());

                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    p.Inlines.Add(line);
                    p.Inlines.Add(new LineBreak());
                }
            }

            if (stringBuilder.Length != 0)
            {
                var list3 = stringBuilder.ToString().TrimEnd('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if (list3.Length == 1)
                {
                    p.Inlines.Add("> " + list3[0]);
                    p.Inlines.Add(new LineBreak());
                }
                else if (nest < 6)
                {
                    var richTextBox2 = new RichTextBox();

                    richTextBox2.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
                    richTextBox2.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");
                    richTextBox2.MaxHeight = Math.Max(0, RichTextBoxHelper.GetMaxHeightEvent(richTextBox));

                    var fd = new EnabledFlowDocument();
                    fd.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
                    fd.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

                    var p2 = new Paragraph();
                    p2.LineHeight = richTextBox.FontSize + 2;

                    p2.Inlines.Add(RichTextBoxHelper.GetParagraph(richTextBox2, stringBuilder.ToString(), nest + 1));

                    fd.Blocks.Add(p2);
                    richTextBox2.Document = fd;

                    var grid = new Grid();
                    grid.Children.Add(richTextBox2);

                    richTextBox2.SelectAll();
                    var header = richTextBox2.Selection.Text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
                    richTextBox2.Selection.Select(richTextBox2.Document.ContentStart, richTextBox2.Document.ContentStart);

                    var label = new Label() { Content = header };

                    var binding = new Binding("ActualWidth")
                    {
                        Source = richTextBox
                    };

                    label.SetBinding(Label.WidthProperty, binding);

                    var expander = new Expander()
                    {
                        IsEnabled = true,
                        IsExpanded = false,
                        Header = label,
                        Content = grid,
                        Margin = new Thickness(2, 6, 2, 6),
                    };

                    expander.Expanded += (object sender, RoutedEventArgs e) =>
                    {
                        expander.Header = " ";

                        e.Handled = true;
                    };

                    expander.Collapsed += (object sender, RoutedEventArgs e) =>
                    {
                        expander.Header = label;

                        e.Handled = true;
                    };

                    p.Inlines.Add(new InlineUIContainer(expander));
                }
            }

            while (p.Inlines.LastInline is LineBreak)
            {
                p.Inlines.Remove(p.Inlines.LastInline);
            }

            stringBuilder.Clear();

            return p;
        }
    }

    class EnabledFlowDocument : FlowDocument
    {
        protected override bool IsEnabledCore
        {
            get
            {
                return true;
            }
        }
    }
}
