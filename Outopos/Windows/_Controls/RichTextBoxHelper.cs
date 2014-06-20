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
using Outopos.Properties;
using Library;
using Library.Net.Outopos;
using A = Library.Net.Amoeba;
using O = Library.Net.Outopos;
using System.Windows.Input;

namespace Outopos.Windows
{
    delegate void WikiClickEventHandler(object sender, Wiki wiki);
    delegate void ChatClickEventHandler(object sender, Chat chat);
    delegate void SeedClickEventHandler(object sender, A.Seed seed);
    delegate void LinkClickEventHandler(object sender, string link);

    delegate ChatMessageWrapper GetAnchorChatMessageWrapperEventHandler(object sender, Anchor anchor);
    delegate double GetMaxHeightEventHandler(object sender);

    class RichTextBoxHelper : DependencyObject
    {
        public static event WikiClickEventHandler WikiClickEvent;
        public static event ChatClickEventHandler ChatClickEvent;
        public static event SeedClickEventHandler SeedClickEvent;
        public static event LinkClickEventHandler LinkClickEvent;

        public static GetAnchorChatMessageWrapperEventHandler GetAnchorChatMessageWrapperEvent;
        public static GetMaxHeightEventHandler GetMaxHeightEvent;

        private static Regex _urlRegex = new Regex(@"^(?<start>.*?)(?<url>http(s)?://(\S)+)(?<end>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static bool CheckAlphanumeric(char value)
        {
            if (!('A' <= value && value <= 'Z')
                && !('a' <= value && value <= 'z')
                && !('0' <= value && value <= '9')) return false;

            return true;
        }

        private static string GetNoBleakString(string value)
        {
            var list = new List<char>();

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];

                if (!CheckAlphanumeric(c))
                {
                    if (list.Count == 0 || list[list.Count - 1] != '\u00A0') list.Add('\u00A0');
                    list.Add(c);
                    list.Add('\u00A0');
                }
                else
                {
                    list.Add(c);
                }
            }

            return new string(list.ToArray());
        }

        public static string MessageToString(DateTime creationTime, string signature, string comment)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(string.Format("{0} - {1}",
                creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo),
                signature));

            stringBuilder.AppendLine();

            foreach (var line in comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    stringBuilder.AppendLine(line);

                    if (line.StartsWith("Wiki:"))
                    {
                        var wiki = O.OutoposConverter.FromWikiString(line);
                        if (wiki == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(wiki));
                        stringBuilder.AppendLine();
                    }
                    else if (line.StartsWith("Chat:"))
                    {
                        var chat = O.OutoposConverter.FromChatString(line);
                        if (chat == null) continue;

                        stringBuilder.AppendLine(MessageConverter.ToInfoMessage(chat));
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

        public static ChatMessageWrapper GetDocumentMessage(DependencyObject obj)
        {
            return (ChatMessageWrapper)obj.GetValue(DocumentMessageProperty);
        }

        public static void SetDocumentMessage(DependencyObject obj, ChatMessageWrapper value)
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

                            RichTextBoxHelper.SetRichTextBox(richTextBox, chatMessageWrapper.Info, chatMessageWrapper.IsTrust);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            });

        public static void SetRichTextBox(RichTextBox richTextBox, ChatTopicInfo info)
        {
            RichTextBoxHelper.SetRichTextBox(richTextBox, info.Header.Tag, info.Header.Certificate.ToString(), info.Header.CreationTime, info.Content.Comment, null, true);

            richTextBox.MaxHeight = double.PositiveInfinity;
        }

        public static void SetRichTextBox(RichTextBox richTextBox, ChatMessageInfo info, bool isTrust)
        {
            RichTextBoxHelper.SetRichTextBox(richTextBox, info.Header.Tag, info.Header.Certificate.ToString(), info.Header.CreationTime, info.Content.Comment, info.Content.Anchors, isTrust);
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
                {
                    var text = creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo) + " - ";

                    var span = new Span();

                    foreach (var c in RichTextBoxHelper.GetNoBleakString(text))
                    {
                        if (c == '\u00A0')
                        {
                            var r = new Run();
                            r.Text = c.ToString();
                            r.FontSize = 0.1;

                            span.Inlines.Add(r);
                        }
                        else
                        {
                            span.Inlines.Add(c.ToString());
                        }
                    }

                    p.Inlines.Add(span);
                }

                {
                    var span = new Span();

                    if (isTrust) span.Foreground = new SolidColorBrush(App.Colors.Message_Trust);
                    else span.Foreground = new SolidColorBrush(App.Colors.Message_Untrust);

                    span.PreviewMouseRightButtonDown += (object sender, System.Windows.Input.MouseButtonEventArgs ex) =>
                    {
                        richTextBox.Selection.Select(span.ContentStart, span.ContentEnd);
                    };

                    foreach (var c in RichTextBoxHelper.GetNoBleakString(signature))
                    {
                        if (c == '\u00A0')
                        {
                            var r = new Run();
                            r.Text = c.ToString();
                            r.FontSize = 0.1;

                            span.Inlines.Add(r);
                        }
                        else
                        {
                            span.Inlines.Add(c.ToString());
                        }
                    }

                    p.Inlines.Add(span);
                }
            }

            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new LineBreak());

            foreach (var line in comment.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                try
                {
                    var rl = line.Trim();

                    if (rl.StartsWith("Wiki:"))
                    {
                        var wiki = O.OutoposConverter.FromWikiString(rl);
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
                                RichTextBoxHelper.WikiClickEvent(sender, wiki);

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
                            r.Text = MessageConverter.ToInfoMessage(wiki);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Chat:"))
                    {
                        var chat = O.OutoposConverter.FromChatString(rl);
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
                            r.Text = MessageConverter.ToInfoMessage(chat);

                            p.Inlines.Add(r);
                        }

                        p.Inlines.Add(new LineBreak());
                    }
                    else if (rl.StartsWith("Seed:"))
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

            if (anchors != null)
            {
                foreach (var anchor in anchors)
                {
                    var wrapper = RichTextBoxHelper.GetAnchorChatMessageWrapperEvent(null, anchor);

                    if (wrapper != null)
                    {
                        var targetRichTextBox = new RichTextBox();
                        RichTextBoxHelper.SetRichTextBox(targetRichTextBox, wrapper.Info, wrapper.IsTrust);

                        var grid = new Grid();
                        grid.Children.Add(targetRichTextBox);

                        var textBlock = new TextBlock();

                        {
                            var span = new Span();

                            span.Inlines.Add(string.Format("{0}\u00A0-\u00A0",
                            wrapper.Info.Header.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                            Run r = new Run();
                            if (wrapper.IsTrust) r.Foreground = new SolidColorBrush(App.Colors.Message_Trust);
                            else r.Foreground = new SolidColorBrush(App.Colors.Message_Untrust);
                            r.Text = wrapper.Info.Header.Certificate.ToString();

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

    class RichTextBoxEx : RichTextBox
    {
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            ModifierKeys modifiers = e.KeyboardDevice.Modifiers;

            if (modifiers.HasFlag(ModifierKeys.Control) && e.Key == System.Windows.Input.Key.C)
            {
                Clipboard.SetText(this.Selection.Text.Replace("\u00A0", ""));

                e.Handled = true;
            }

            base.OnPreviewKeyDown(e);
        }
    }
}
