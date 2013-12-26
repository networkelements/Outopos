using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lair.Windows;
using Library;
using Library.Collections;
using Library.Net.Lair;
using A = Library.Net.Amoeba;
using System.Threading;

namespace Lair
{
    static class Clipboard
    {
        private static LockedList<Windows.SectionCategorizeTreeItem> _sectionCategorizeTreeItemList = new LockedList<SectionCategorizeTreeItem>();
        private static LockedList<Windows.SectionTreeItem> _sectionTreeItemList = new LockedList<SectionTreeItem>();
        private static LockedList<Windows.ChatCategorizeTreeItem> _chatCategorizeTreeItemList = new LockedList<ChatCategorizeTreeItem>();
        private static LockedList<Windows.ChatTreeItem> _chatTreeItemList = new LockedList<ChatTreeItem>();
        private static LockedList<Windows.MailCategorizeTreeItem> _mailCategorizeTreeItemList = new LockedList<MailCategorizeTreeItem>();
        private static LockedList<Windows.MailTreeItem> _mailTreeItemList = new LockedList<MailTreeItem>();

        private static ClipboardWatcher _clipboardWatcher;
        private static ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        private static object _thisLock = new object();

        static Clipboard()
        {
            _clipboardWatcher = new ClipboardWatcher();
            // Clipboard呼び出しメソッドのスレッドから呼ばれる場合もあるし、そうでない場合もある。
            // つまり、ちゃんと同期しないといけない。
            _clipboardWatcher.DrawClipboard += (sender, e) =>
            {
                lock (_thisLock)
                {
                    _sectionCategorizeTreeItemList.Clear();
                    _sectionTreeItemList.Clear();

                    _chatCategorizeTreeItemList.Clear();
                    _chatTreeItemList.Clear();

                    _mailCategorizeTreeItemList.Clear();
                    _mailTreeItemList.Clear();

                    _manualResetEvent.Set();
                }
            };
        }

        public static void Clear()
        {
            lock (_thisLock)
            {
                _manualResetEvent.Reset();

                _sectionCategorizeTreeItemList.Clear();
                _sectionTreeItemList.Clear();

                _chatCategorizeTreeItemList.Clear();
                _chatTreeItemList.Clear();

                _mailCategorizeTreeItemList.Clear();
                _mailTreeItemList.Clear();

                System.Windows.Clipboard.Clear();
            }

            _manualResetEvent.WaitOne(1000);
        }

        public static bool ContainsPaths()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsFileDropList();
            }
        }

        public static IEnumerable<string> GetPaths()
        {
            lock (_thisLock)
            {
                try
                {
                    return System.Windows.Clipboard.GetFileDropList().Cast<string>();
                }
                catch (Exception)
                {

                }

                return new string[0];
            }
        }

        public static void SetPaths(IEnumerable<string> collection)
        {
            lock (_thisLock)
            {
                try
                {
                    var list = new System.Collections.Specialized.StringCollection();
                    list.AddRange(collection.ToArray());
                    System.Windows.Clipboard.SetFileDropList(list);
                }
                catch (Exception)
                {

                }
            }
        }

        public static bool ContainsText()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsText();
            }
        }

        public static string GetText()
        {
            lock (_thisLock)
            {
                try
                {
                    return System.Windows.Clipboard.GetText();
                }
                catch (Exception)
                {

                }

                return "";
            }
        }

        public static void SetText(string text)
        {
            lock (_thisLock)
            {
                try
                {
                    System.Windows.Clipboard.SetText(text);
                }
                catch (Exception)
                {

                }
            }
        }

        public static bool ContainsNodes()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Node:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Node> GetNodes()
        {
            lock (_thisLock)
            {
                var list = new List<Node>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var node = LairConverter.FromNodeString(item);
                        if (node == null) continue;

                        list.Add(node);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetNodes(IEnumerable<Node> nodes)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var item in nodes)
                    {
                        sb.AppendLine(LairConverter.ToNodeString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        public static bool ContainsSections()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Section:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Tuple<Section, string>> GetSections()
        {
            lock (_thisLock)
            {
                var list = new List<Tuple<Section, string>>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string option;

                        var tag = LairConverter.FromSectionString(item, out option);
                        if (tag == null) continue;

                        list.Add(new Tuple<Section, string>(tag, option));
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetSections(IEnumerable<Tuple<Section, string>> sections)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var tuple in sections)
                    {
                        sb.AppendLine(LairConverter.ToSectionString(tuple.Item1, tuple.Item2));
                    }

                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        public static bool ContainsWikis()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Wiki:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Tuple<Wiki, string>> GetWikis()
        {
            lock (_thisLock)
            {
                var list = new List<Tuple<Wiki, string>>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string option;

                        var tag = LairConverter.FromWikiString(item, out option);
                        if (tag == null) continue;

                        list.Add(new Tuple<Wiki, string>(tag, option));
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetWikis(IEnumerable<Tuple<Wiki, string>> sections)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var tuple in sections)
                    {
                        sb.AppendLine(LairConverter.ToWikiString(tuple.Item1, tuple.Item2));
                    }

                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        public static bool ContainsChats()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Chat:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Tuple<Chat, string>> GetChats()
        {
            lock (_thisLock)
            {
                var list = new List<Tuple<Chat, string>>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string option;

                        var tag = LairConverter.FromChatString(item, out option);
                        if (tag == null) continue;

                        list.Add(new Tuple<Chat, string>(tag, option));
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetChats(IEnumerable<Tuple<Chat, string>> sections)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var tuple in sections)
                    {
                        sb.AppendLine(LairConverter.ToChatString(tuple.Item1, tuple.Item2));
                    }

                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        public static bool ContainsSeeds()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Seed:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<A.Seed> GetSeeds()
        {
            lock (_thisLock)
            {
                var list = new List<A.Seed>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var seed = A.AmoebaConverter.FromSeedString(item);
                        if (seed == null) continue;

                        list.Add(seed);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetSeeds(IEnumerable<A.Seed> seeds)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var item in seeds)
                    {
                        sb.AppendLine(A.AmoebaConverter.ToSeedString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        public static bool ContainsSectionCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _sectionCategorizeTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<Windows.SectionCategorizeTreeItem> GetSectionCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _sectionCategorizeTreeItemList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetSectionCategorizeTreeItems(IEnumerable<Windows.SectionCategorizeTreeItem> sectionCategorizeTreeItems)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                _sectionCategorizeTreeItemList.AddRange(sectionCategorizeTreeItems.Select(n => n.Clone()));
            }
        }

        public static bool ContainsSectionTreeItems()
        {
            lock (_thisLock)
            {
                return _sectionTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<Windows.SectionTreeItem> GetSectionTreeItems()
        {
            lock (_thisLock)
            {
                return _sectionTreeItemList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetSectionTreeItems(IEnumerable<Windows.SectionTreeItem> sectionTreeItems)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                {
                    var sb = new StringBuilder();

                    foreach (var item in sectionTreeItems)
                    {
                        sb.AppendLine(LairConverter.ToSectionString(item.Tag, item.LeaderSignature));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                _sectionTreeItemList.AddRange(sectionTreeItems.Select(n => n.Clone()));
            }
        }

        public static bool ContainsChatCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _chatCategorizeTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<Windows.ChatCategorizeTreeItem> GetChatCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _chatCategorizeTreeItemList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetChatCategorizeTreeItems(IEnumerable<Windows.ChatCategorizeTreeItem> chatCategorizeTreeItems)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                _chatCategorizeTreeItemList.AddRange(chatCategorizeTreeItems.Select(n => n.Clone()));
            }
        }

        public static bool ContainsChatTreeItems()
        {
            lock (_thisLock)
            {
                return _chatTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<Windows.ChatTreeItem> GetChatTreeItems()
        {
            lock (_thisLock)
            {
                return _chatTreeItemList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetChatTreeItems(IEnumerable<Windows.ChatTreeItem> chatTreeItems)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                _chatTreeItemList.AddRange(chatTreeItems.Select(n => n.Clone()));
            }
        }

        public static bool ContainsMailCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _mailCategorizeTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<Windows.MailCategorizeTreeItem> GetMailCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _mailCategorizeTreeItemList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetMailCategorizeTreeItems(IEnumerable<Windows.MailCategorizeTreeItem> mailCategorizeTreeItems)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                _mailCategorizeTreeItemList.AddRange(mailCategorizeTreeItems.Select(n => n.Clone()));
            }
        }

        public static bool ContainsMailTreeItems()
        {
            lock (_thisLock)
            {
                return _mailTreeItemList.Count != 0;
            }
        }

        public static IEnumerable<Windows.MailTreeItem> GetMailTreeItems()
        {
            lock (_thisLock)
            {
                return _mailTreeItemList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetMailTreeItems(IEnumerable<Windows.MailTreeItem> mailTreeItems)
        {
            lock (_thisLock)
            {
                Clipboard.Clear();

                _mailTreeItemList.AddRange(mailTreeItems.Select(n => n.Clone()));
            }
        }

        public class ClipboardWatcher : IDisposable
        {
            private ClipboardWatcherForm _form;

            public event EventHandler DrawClipboard;

            public ClipboardWatcher()
            {
                _form = new ClipboardWatcherForm();
                _form.StartWatch(this.OnDrawClipboard);
            }

            ~ClipboardWatcher()
            {
                this.Dispose();
            }

            private void OnDrawClipboard()
            {
                if (DrawClipboard != null)
                {
                    DrawClipboard(this, EventArgs.Empty);
                }
            }

            public void Dispose()
            {
                _form.Dispose();
            }

            private class ClipboardWatcherForm : System.Windows.Forms.Form
            {
                [DllImport("user32.dll")]
                private static extern IntPtr SetClipboardViewer(IntPtr hwnd);

                [DllImport("user32.dll")]
                private static extern bool ChangeClipboardChain(IntPtr hwnd, IntPtr hWndNext);

                private const int WM_DRAWCLIPBOARD = 0x0308;
                private const int WM_CHANGECBCHAIN = 0x030D;

                private IntPtr _nextHandle;
                private Action _drawClipboard;

                public void StartWatch(Action drawClipboard)
                {
                    _drawClipboard = drawClipboard;
                    _nextHandle = SetClipboardViewer(this.Handle);
                }

                protected override void WndProc(ref System.Windows.Forms.Message m)
                {
                    if (m.Msg == WM_DRAWCLIPBOARD)
                    {
                        _drawClipboard();
                    }
                    else if (m.Msg == WM_CHANGECBCHAIN)
                    {
                        if (m.WParam == _nextHandle)
                        {
                            _nextHandle = m.LParam;
                        }
                    }

                    base.WndProc(ref m);
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        ChangeClipboardChain(this.Handle, _nextHandle);
                        base.Dispose(disposing);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
    }
}
