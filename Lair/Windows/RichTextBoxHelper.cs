using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
        private static Regex _urlRegex = new Regex(@"^(?<start>.*?)(?<url>http(s)?://([\w-]+\.)+[\w-]+(/[\w-./?%&=]*)?)(?<end>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

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
                PropertyChangedCallback = (obj, e) =>
                {
                    var richTextBox = (RichTextBox)obj;

                    richTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
                    richTextBox.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

                    richTextBox.MaxHeight = RichTextBoxHelper.GetMaxHeightEvent(richTextBox);

                    var message = e.NewValue as Message;
                    if (message == null) return;

                    var fd = new FlowDocument();
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

                    foreach (var line in message.Content.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        try
                        {
                            var rl = line.Trim();

                            if (rl.StartsWith("Seed@"))
                            {
                                var seed = Library.Net.Amoeba.AmoebaConverter.FromSeedString(rl);
                                if (!seed.VerifyCertificate()) throw new Exception();

                                {
                                    var span = new Span();

                                    var rl1 = rl.Substring(0, 64);
                                    var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                                    var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                                    Hyperlink l = new Hyperlink();
                                    l.Foreground = new SolidColorBrush(Color.FromRgb(0xDF, 0xDF, 0xDF));
                                    l.Cursor = Cursors.Hand;
                                    l.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs ex) =>
                                    {
                                        if (RichTextBoxHelper.SeedClickEvent != null)
                                        {
                                            RichTextBoxHelper.SeedClickEvent(sender, seed);
                                        }
                                    };

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
                                    r.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xEF));
                                    r.Text = MessageConverter.ToInfoMessage(seed);

                                    p.Inlines.Add(r);
                                }

                                p.Inlines.Add(new LineBreak());
                            }
                            else if (rl.StartsWith("Channel@"))
                            {
                                var channel = Library.Net.Lair.LairConverter.FromChannelString(rl);

                                {
                                    var span = new Span();

                                    var rl1 = rl.Substring(0, 64);
                                    var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                                    var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                                    Hyperlink l = new Hyperlink();
                                    l.Foreground = new SolidColorBrush(Color.FromRgb(0xDF, 0xDF, 0xDF));
                                    l.Cursor = Cursors.Hand;
                                    l.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs ex) =>
                                    {
                                        if (RichTextBoxHelper.ChannelClickEvent != null)
                                        {
                                            RichTextBoxHelper.ChannelClickEvent(sender, channel);
                                        }
                                    };

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
                                    r.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xEF));
                                    r.Text = MessageConverter.ToInfoMessage(channel);

                                    p.Inlines.Add(r);
                                }

                                p.Inlines.Add(new LineBreak());
                            }
                            else
                            {
                                var line2 = line;

                                for (; ; )
                                {
                                    Match match = _urlRegex.Match(line2);

                                    if (match.Success)
                                    {
                                        p.Inlines.Add(match.Groups["start"].Value);

                                        Hyperlink l = new Hyperlink();
                                        l.Foreground = new SolidColorBrush(Color.FromRgb(0xDF, 0xDF, 0xDF));
                                        l.Inlines.Add(match.Groups["url"].Value);
                                        l.Cursor = Cursors.Hand;
                                        l.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs ex) =>
                                        {
                                            if (RichTextBoxHelper.LinkClickEvent != null)
                                            {
                                                RichTextBoxHelper.LinkClickEvent(sender, match.Groups["url"].Value);
                                            }
                                        };

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
                        catch (Exception)
                        {
                            p.Inlines.Add(line);
                            p.Inlines.Add(new LineBreak());
                        }
                    }

                    while (p.Inlines.LastInline is LineBreak)
                    {
                        p.Inlines.Remove(p.Inlines.LastInline);
                    }

                    fd.Blocks.Add(p);
                    richTextBox.Document = fd;
                }
            });
    }
}
