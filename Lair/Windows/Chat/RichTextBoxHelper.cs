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
using System.Windows.Markup;
using System.Windows.Media;
using Lair.Properties;
using Library;
using Library.Net.Lair;
using a = Library.Net.Amoeba;
using l = Library.Net.Lair;

namespace Lair.Windows
{
    delegate void LinkClickEventHandler(object sender, string link);
    delegate void SectionClickEventHandler(object sender, l.Section section, string leaderSignature);
    delegate void ArchiveClickEventHandler(object sender, Archive archive, string path);
    delegate void ChatClickEventHandler(object sender, Chat chat);
    delegate void SeedClickEventHandler(object sender, Library.Net.Amoeba.Seed seed);

    delegate ChatMessagePack GetAnchorMessageEventHandler(object sender, Chat chat, Key key);
    delegate double GetMaxHeightEventHandler(object sender);

    class RichTextBoxHelper : DependencyObject
    {
        public static event LinkClickEventHandler LinkClickEvent;
        public static event SeedClickEventHandler SeedClickEvent;
        public static event SectionClickEventHandler SectionClickEvent;
        public static event ChatClickEventHandler ChatClickEvent;
        public static GetAnchorMessageEventHandler GetAnchorMessageEvent;
        public static GetMaxHeightEventHandler GetMaxHeightEvent;

        private static Regex _urlRegex = new Regex(@"^(?<start>.*?)(?<url>http(s)?://(\S)+)(?<end>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string GetMessageToString(ChatMessagePack pack)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(string.Format("{0} UTC - {1}",
                pack.Header.CreationTime.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo),
                pack.Header.Certificate.ToString()));

            stringBuilder.AppendLine();

            foreach (var line in pack.Content.Comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    stringBuilder.AppendLine(line);

                    if (line.StartsWith("Section:"))
                    {
                        string leaderSignature;

                        var section = Library.Net.Lair.LairConverter.FromSectionString(line, out leaderSignature);
                        if (section == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(section, leaderSignature));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Archive:"))
                    {
                        string path;

                        var section = Library.Net.Lair.LairConverter.FromArchiveString(line, out path);
                        if (section == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(section, path));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Chat:"))
                    {
                        string dummy;

                        var chat = Library.Net.Lair.LairConverter.FromChatString(line, out dummy);
                        if (chat == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(chat, null));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Seed:"))
                    {
                        var seed = Library.Net.Amoeba.AmoebaConverter.FromSeedString(line);
                        if (seed == null || !seed.VerifyCertificate()) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(seed));
                        stringBuilder.AppendLine();
                    }

                }
                catch (Exception)
                {

                }
            }

            return stringBuilder.ToString().TrimEnd('\r', '\n');
        }

        public static ChatMessagePack GetDocumentMessage(DependencyObject obj)
        {
            return (ChatMessagePack)obj.GetValue(DocumentMessageProperty);
        }

        public static void SetDocumentMessage(DependencyObject obj, ChatMessagePack value)
        {
            obj.SetValue(DocumentMessageProperty, value);
        }

        public static readonly DependencyProperty DocumentMessageProperty = DependencyProperty.RegisterAttached("DocumentMessage", typeof(object), typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata()
            {
                AffectsMeasure = true,
                AffectsArrange = true,
                AffectsParentMeasure = true,
                AffectsParentArrange = true,
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                PropertyChangedCallback = (obj, e) =>
                {
                    if (obj == null || e.NewValue == e.OldValue) return;

                    try
                    {
                        var richTextBox = (RichTextBox)obj;

                        if (e.NewValue is ChatMessagePack)
                        {
                            var information = e.NewValue as ChatMessagePack;
                            if (information == null) return;

                            RichTextBoxHelper.SetRichTextBox(richTextBox, information);
                        }
                        else if (e.NewValue is ChatTopicPack)
                        {
                            var information = e.NewValue as ChatTopicPack;
                            if (information == null) return;

                            RichTextBoxHelper.SetRichTextBox(richTextBox, information);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            });

        public static void SetRichTextBox(RichTextBox richTextBox, ChatTopicPack pack)
        {
            richTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            richTextBox.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            var fd = new EnabledFlowDocument();

            fd.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            fd.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            var p = new Paragraph();
            p.LineHeight = richTextBox.FontSize + 2;

            p.Inlines.Add(string.Format("{0}\u00A0-\u00A0{1}",
                pack.Header.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo),
                pack.Header.Certificate.ToString()));

            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());

            foreach (var line in pack.Content.Comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    var rl = line.Trim();

                    if (rl.StartsWith("Seed@"))
                    {
                        var seed = Library.Net.Amoeba.AmoebaConverter.FromSeedString(rl);
                        if (seed == null || !seed.VerifyCertificate()) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SeedClickEvent(sender, seed);

                                if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            };
                            l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
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
                    else if (rl.StartsWith("Section:"))
                    {
                        string leaderSignature;

                        var section = Library.Net.Lair.LairConverter.FromSectionString(rl, out leaderSignature);
                        if (section == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SectionClickEvent(sender, section, leaderSignature);

                                if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            };
                            l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
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

                            p.Inlines.Add(span);
                        }

                        p.Inlines.Add(new LineBreak());

                        {
                            Run r = new Run();
                            r.Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            r.Text = MessageConverter.ToInfoMessage(section, leaderSignature);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Chat:"))
                    {
                        string dummy;

                        var chat = Library.Net.Lair.LairConverter.FromChatString(rl, out dummy);
                        if (chat == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.ChatClickEvent(sender, chat);

                                if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            };
                            l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
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

                            p.Inlines.Add(span);
                        }

                        p.Inlines.Add(new LineBreak());

                        {
                            Run r = new Run();
                            r.Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            r.Text = MessageConverter.ToInfoMessage(chat, null);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        var tempLine = line;

                        for (; ; )
                        {
                            Match match = _urlRegex.Match(tempLine);

                            if (match.Success)
                            {
                                p.Inlines.Add(match.Groups["start"].Value);

                                var url = match.Groups["url"].Value;

                                Hyperlink l = new Hyperlink();
                                l.Cursor = System.Windows.Input.Cursors.Hand;

                                if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                                else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                                l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                                {
                                    RichTextBoxHelper.LinkClickEvent(sender, url);

                                    if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                                };
                                l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                                {
                                    richTextBox.Selection.Select(l.ContentStart, l.ContentEnd);
                                };

                                l.Inlines.Add(url);
                                p.Inlines.Add(l);

                                tempLine = match.Groups["end"].Value;
                            }
                            else
                            {
                                p.Inlines.Add(tempLine);
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

        public static void SetRichTextBox(RichTextBox richTextBox, ChatMessagePack pack)
        {
            richTextBox.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            richTextBox.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            var maxHeight = Math.Max(0, RichTextBoxHelper.GetMaxHeightEvent(richTextBox));

            richTextBox.MaxHeight = maxHeight / 2;

            var fd = new EnabledFlowDocument();

            fd.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_MessageFontFamily);
            fd.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_MessageFontSize + "pt");

            var p = new Paragraph();
            p.LineHeight = richTextBox.FontSize + 2;

            p.Inlines.Add(string.Format("{0}\u00A0-\u00A0{1}",
                pack.Header.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo),
                pack.Header.Certificate.ToString()));

            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());

            foreach (var line in pack.Content.Comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    var rl = line.Trim();

                    if (rl.StartsWith("Seed@"))
                    {
                        var seed = Library.Net.Amoeba.AmoebaConverter.FromSeedString(rl);
                        if (seed == null || !seed.VerifyCertificate()) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SeedClickEvent(sender, seed);

                                if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            };
                            l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
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
                    else if (rl.StartsWith("Section:"))
                    {
                        string leaderSignature;

                        var section = Library.Net.Lair.LairConverter.FromSectionString(rl, out leaderSignature);
                        if (section == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SectionClickEvent(sender, section, leaderSignature);

                                if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            };
                            l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
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

                            p.Inlines.Add(span);
                        }

                        p.Inlines.Add(new LineBreak());

                        {
                            Run r = new Run();
                            r.Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            r.Text = MessageConverter.ToInfoMessage(section, leaderSignature);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Chat:"))
                    {
                        string dummy;

                        var chat = Library.Net.Lair.LairConverter.FromChatString(rl, out dummy);
                        if (chat == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.ChatClickEvent(sender, chat);

                                if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                            };
                            l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
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

                            p.Inlines.Add(span);
                        }

                        p.Inlines.Add(new LineBreak());

                        {
                            Run r = new Run();
                            r.Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            r.Text = MessageConverter.ToInfoMessage(chat, null);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        var tempLine = line;

                        for (; ; )
                        {
                            Match match = _urlRegex.Match(tempLine);

                            if (match.Success)
                            {
                                p.Inlines.Add(match.Groups["start"].Value);

                                var url = match.Groups["url"].Value;

                                Hyperlink l = new Hyperlink();
                                l.Cursor = System.Windows.Input.Cursors.Hand;

                                if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                                else l.Foreground = new SolidColorBrush(App.LairColors.Link_New);

                                l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                                {
                                    RichTextBoxHelper.LinkClickEvent(sender, url);

                                    if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.LairColors.Link);
                                };
                                l.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                                {
                                    richTextBox.Selection.Select(l.ContentStart, l.ContentEnd);
                                };

                                l.Inlines.Add(url);
                                p.Inlines.Add(l);

                                tempLine = match.Groups["end"].Value;
                            }
                            else
                            {
                                p.Inlines.Add(tempLine);
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

            foreach (var anchor in pack.Content.Anchors)
            {
                var anchorPack = RichTextBoxHelper.GetAnchorMessageEvent(null, pack.Header.Tag, anchor);

                if (anchorPack != null)
                {
                    var targetRichTextBox = new RichTextBox();
                    RichTextBoxHelper.SetRichTextBox(targetRichTextBox, anchorPack);

                    var grid = new Grid();
                    grid.Children.Add(targetRichTextBox);

                    var text = string.Format("{0} - {1}",
                            anchorPack.Header.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo),
                            anchorPack.Header.Certificate.ToString());

                    var textBlock = new TextBlock() { Text = text };

                    var binding = new Binding("ActualWidth")
                    {
                        Source = richTextBox
                    };

                    textBlock.SetBinding(TextBlock.WidthProperty, binding);

                    var expander = new Expander()
                    {
                        IsEnabled = true,
                        IsExpanded = false,
                        Header = textBlock,

                        Margin = new Thickness(2, 6, 2, 6),
                    };

                    expander.Expanded += (object sender, RoutedEventArgs e) =>
                    {
                        expander.Header = " ";
                        expander.Content = grid;

                        e.Handled = true;
                    };

                    expander.Collapsed += (object sender, RoutedEventArgs e) =>
                    {
                        expander.Header = textBlock;
                        expander.Content = null;

                        e.Handled = true;
                    };

                    p.Inlines.Add(new LineBreak());
                    p.Inlines.Add(new InlineUIContainer(expander));
                }
                else
                {
                    var targetRichTextBox = new RichTextBox();

                    var grid = new Grid();
                    grid.Children.Add(targetRichTextBox);

                    var textBlock = new TextBlock() { Text = " " };

                    var binding = new Binding("ActualWidth")
                    {
                        Source = richTextBox
                    };

                    textBlock.SetBinding(TextBlock.WidthProperty, binding);

                    var expander = new Expander()
                    {
                        IsEnabled = false,
                        IsExpanded = false,
                        Header = textBlock,

                        Margin = new Thickness(2, 6, 2, 6),
                    };

                    p.Inlines.Add(new LineBreak());
                    p.Inlines.Add(new InlineUIContainer(expander));
                }
            }

            fd.Blocks.Add(p);

            richTextBox.Document = fd;
        }
    }

    class EnabledFlowDocument : FlowDocument
    {
        public EnabledFlowDocument()
        {
            base.IsOptimalParagraphEnabled = false;
            base.IsHyphenationEnabled = false;
        }

        protected override bool IsEnabledCore
        {
            get
            {
                return true;
            }
        }
    }
}
