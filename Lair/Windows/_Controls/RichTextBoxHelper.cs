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
using A = Library.Net.Amoeba;
using L = Library.Net.Lair;

namespace Lair.Windows
{
    delegate void LinkClickEventHandler(object sender, string link);
    delegate void SeedClickEventHandler(object sender, A.Seed seed);
    delegate void SectionClickEventHandler(object sender, L.Section section, string leaderSignature);
    delegate void WikiClickEventHandler(object sender, Wiki wiki, string path);
    delegate void ChatClickEventHandler(object sender, Chat chat);

    delegate SectionMessageWrapper GetAnchorSectionMessageWrapperEventHandler(object sender, L.Section section, Anchor anchor);
    delegate ChatMessageWrapper GetAnchorChatMessageWrapperEventHandler(object sender, Chat chat, Anchor anchor);
    delegate double GetMaxHeightEventHandler(object sender);

    class RichTextBoxHelper : DependencyObject
    {
        public static event LinkClickEventHandler LinkClickEvent;
        public static event SeedClickEventHandler SeedClickEvent;
        public static event SectionClickEventHandler SectionClickEvent;
        public static event WikiClickEventHandler WikiClickEvent;
        public static event ChatClickEventHandler ChatClickEvent;
        public static GetAnchorSectionMessageWrapperEventHandler GetAnchorSectionMessageWrapperEvent;
        public static GetAnchorChatMessageWrapperEventHandler GetAnchorChatMessageWrapperEvent;
        public static GetMaxHeightEventHandler GetMaxHeightEvent;

        private static Regex _urlRegex = new Regex(@"^(?<start>.*?)(?<url>http(s)?://(\S)+)(?<end>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string MessageToString(DateTime creationTime, string signature, string comment)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(string.Format("{0} UTC - {1}",
                creationTime.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo),
                signature));

            stringBuilder.AppendLine();

            foreach (var line in comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    stringBuilder.AppendLine(line);

                    if (line.StartsWith("Section:"))
                    {
                        string leaderSignature;

                        var section = L.LairConverter.FromSectionString(line, out leaderSignature);
                        if (section == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(section, leaderSignature));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Wiki:"))
                    {
                        string path;

                        var section = L.LairConverter.FromWikiString(line, out path);
                        if (section == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(section, path));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Chat:"))
                    {
                        string dummy;

                        var chat = L.LairConverter.FromChatString(line, out dummy);
                        if (chat == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(chat, null));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Seed:"))
                    {
                        var seed = A.AmoebaConverter.FromSeedString(line);
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

        public static ChatMessage GetDocumentMessage(DependencyObject obj)
        {
            return (ChatMessage)obj.GetValue(DocumentMessageProperty);
        }

        public static void SetDocumentMessage(DependencyObject obj, ChatMessage value)
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

                        if (e.NewValue is ChatMessageWrapper)
                        {
                            var chatMessageWrapper = e.NewValue as ChatMessageWrapper;
                            if (chatMessageWrapper == null) return;

                            RichTextBoxHelper.SetRichTextBox(richTextBox, chatMessageWrapper.Value, chatMessageWrapper.IsTrust);
                        }
                        else if (e.NewValue is SectionMessageWrapper)
                        {
                            var sectionMessageWrapper = e.NewValue as SectionMessageWrapper;
                            if (sectionMessageWrapper == null) return;

                            RichTextBoxHelper.SetRichTextBox(richTextBox, sectionMessageWrapper.Value, sectionMessageWrapper.IsTrust);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            });

        public static void SetRichTextBox(RichTextBox richTextBox, SectionMessage sectionMessage, bool isTrust)
        {
            RichTextBoxHelper.SetRichTextBox(richTextBox, sectionMessage.Tag, sectionMessage.Signature, sectionMessage.CreationTime, sectionMessage.Comment, sectionMessage.Anchor, isTrust);
        }

        public static void SetRichTextBox(RichTextBox richTextBox, L.Section tag, string signature, DateTime creationTime, string comment, Anchor anchor, bool isTrust)
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

            {
                var span = new Span();

                span.Inlines.Add(string.Format("{0}\u00A0-\u00A0",
                creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                Run r = new Run();
                if (isTrust) r.Foreground = new SolidColorBrush(App.Colors.Message_Trust);
                else r.Foreground = new SolidColorBrush(App.Colors.Message_Untrust);
                r.Text = signature;

                r.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                {
                    richTextBox.Selection.Select(r.ContentStart, r.ContentEnd);
                };

                span.Inlines.Add(r);

                p.Inlines.Add(span);
            }

            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());

            foreach (var line in comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    var rl = line.Trim();

                    if (rl.StartsWith("Seed:"))
                    {
                        var seed = A.AmoebaConverter.FromSeedString(rl);
                        if (seed == null || !seed.VerifyCertificate()) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SeedClickEvent(sender, seed);

                                if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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

                        var section = L.LairConverter.FromSectionString(rl, out leaderSignature);
                        if (section == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SectionClickEvent(sender, section, leaderSignature);

                                if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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
                    else if (rl.StartsWith("Wiki:"))
                    {
                        string path;

                        var wiki = L.LairConverter.FromWikiString(rl, out path);
                        if (wiki == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_WikiHistorys.Contains(wiki)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.WikiClickEvent(sender, wiki, path);

                                if (Settings.Instance.Global_WikiHistorys.Contains(wiki)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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
                            r.Text = MessageConverter.ToInfoMessage(wiki, path);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Chat:"))
                    {
                        string dummy;

                        var chat = L.LairConverter.FromChatString(rl, out dummy);
                        if (chat == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.ChatClickEvent(sender, chat);

                                if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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

                                if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                                else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                                l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                                {
                                    RichTextBoxHelper.LinkClickEvent(sender, url);

                                    if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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

            if (anchor != null)
            {
                var wrapper = RichTextBoxHelper.GetAnchorSectionMessageWrapperEvent(null, tag, anchor);

                if (wrapper != null)
                {
                    var targetRichTextBox = new RichTextBox();
                    RichTextBoxHelper.SetRichTextBox(targetRichTextBox, wrapper.Value, wrapper.IsTrust);

                    var grid = new Grid();
                    grid.Children.Add(targetRichTextBox);

                    var textBlock = new TextBlock();

                    {
                        var span = new Span();

                        span.Inlines.Add(string.Format("{0}\u00A0-\u00A0",
                        wrapper.Value.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                        Run r = new Run();
                        if (wrapper.IsTrust) r.Foreground = new SolidColorBrush(App.Colors.Message_Trust);
                        else r.Foreground = new SolidColorBrush(App.Colors.Message_Untrust);
                        r.Text = wrapper.Value.Signature;

                        span.Inlines.Add(r);

                        textBlock.Inlines.Add(span);
                    }

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
            }

            fd.Blocks.Add(p);

            richTextBox.Document = fd;
        }

        public static void SetRichTextBox(RichTextBox richTextBox, ChatMessage chatMessage, bool isTrust)
        {
            RichTextBoxHelper.SetRichTextBox(richTextBox, chatMessage.Tag, chatMessage.Signature, chatMessage.CreationTime, chatMessage.Comment, chatMessage.Anchors, isTrust);
        }

        public static void SetRichTextBox(RichTextBox richTextBox, Chat tag, string signature, DateTime creationTime, string comment, IEnumerable<Anchor> anchors, bool isTrust)
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

            {
                var span = new Span();

                span.Inlines.Add(string.Format("{0}\u00A0-\u00A0",
                creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                Run r = new Run();
                if (isTrust) r.Foreground = new SolidColorBrush(App.Colors.Message_Trust);
                else r.Foreground = new SolidColorBrush(App.Colors.Message_Untrust);
                r.Text = signature;

                r.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                {
                    richTextBox.Selection.Select(r.ContentStart, r.ContentEnd);
                };

                span.Inlines.Add(r);

                p.Inlines.Add(span);
            }

            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());

            foreach (var line in comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    var rl = line.Trim();

                    if (rl.StartsWith("Seed:"))
                    {
                        var seed = A.AmoebaConverter.FromSeedString(rl);
                        if (seed == null || !seed.VerifyCertificate()) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SeedClickEvent(sender, seed);

                                if (Settings.Instance.Global_SeedHistorys.Contains(seed)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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

                        var section = L.LairConverter.FromSectionString(rl, out leaderSignature);
                        if (section == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.SectionClickEvent(sender, section, leaderSignature);

                                if (Settings.Instance.Global_SectionHistorys.Contains(section)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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
                    else if (rl.StartsWith("Wiki:"))
                    {
                        string path;

                        var wiki = L.LairConverter.FromWikiString(rl, out path);
                        if (wiki == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_WikiHistorys.Contains(wiki)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.WikiClickEvent(sender, wiki, path);

                                if (Settings.Instance.Global_WikiHistorys.Contains(wiki)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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
                            r.Text = MessageConverter.ToInfoMessage(wiki, path);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Chat:"))
                    {
                        string dummy;

                        var chat = L.LairConverter.FromChatString(rl, out dummy);
                        if (chat == null) throw new Exception();

                        {
                            var span = new Span();

                            var rl1 = rl.Substring(0, 64);
                            var rl2 = (64 < rl.Length) ? rl.Substring(64, Math.Min(rl.Length - 64, 16)) : "";
                            var rl3 = (80 < rl.Length) ? rl.Substring(80) : "";

                            Hyperlink l = new Hyperlink();
                            l.Cursor = System.Windows.Input.Cursors.Hand;

                            if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                            else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                            l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                            {
                                RichTextBoxHelper.ChatClickEvent(sender, chat);

                                if (Settings.Instance.Global_ChatHistorys.Contains(chat)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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

                                if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.Colors.Link);
                                else l.Foreground = new SolidColorBrush(App.Colors.Link_New);

                                l.PreviewMouseLeftButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                                {
                                    RichTextBoxHelper.LinkClickEvent(sender, url);

                                    if (Settings.Instance.Global_UrlHistorys.Contains(url)) l.Foreground = new SolidColorBrush(App.Colors.Link);
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

            foreach (var anchor in anchors)
            {
                var wrapper = RichTextBoxHelper.GetAnchorChatMessageWrapperEvent(null, tag, anchor);

                if (wrapper != null)
                {
                    var targetRichTextBox = new RichTextBox();
                    RichTextBoxHelper.SetRichTextBox(targetRichTextBox, wrapper.Value, wrapper.IsTrust);

                    var grid = new Grid();
                    grid.Children.Add(targetRichTextBox);

                    var textBlock = new TextBlock();

                    {
                        var span = new Span();

                        span.Inlines.Add(string.Format("{0}\u00A0-\u00A0",
                        wrapper.Value.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                        Run r = new Run();
                        if (wrapper.IsTrust) r.Foreground = new SolidColorBrush(App.Colors.Message_Trust);
                        else r.Foreground = new SolidColorBrush(App.Colors.Message_Untrust);
                        r.Text = wrapper.Value.Signature;

                        span.Inlines.Add(r);

                        textBlock.Inlines.Add(span);
                    }

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
