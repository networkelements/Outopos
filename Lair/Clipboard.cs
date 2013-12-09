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
using a = Library.Net.Amoeba;

namespace Lair
{
    static class Clipboard
    {
        private static bool _isNodesCached;
        private static bool _isTagsCached;
        private static bool _isLinkOptionsCached;
        private static bool _isSeedsCached;

        private static LockedList<Node> _nodeList = new LockedList<Node>();
        private static LockedList<Tag> _tagList = new LockedList<Tag>();
        private static LockedList<LinkOption> _linkOptionList = new LockedList<LinkOption>();
        private static LockedList<a.Seed> _seedList = new LockedList<a.Seed>();

        private static LockedList<Windows.SectionCategorizeTreeItem> _sectionCategorizeTreeItemList = new LockedList<SectionCategorizeTreeItem>();
        private static LockedList<Windows.SectionTreeItem> _sectionTreeItemList = new LockedList<SectionTreeItem>();
        private static LockedList<Windows.ChatCategorizeTreeItem> _chatCategorizeTreeItemList = new LockedList<ChatCategorizeTreeItem>();
        private static LockedList<Windows.ChatTreeItem> _chatTreeItemList = new LockedList<ChatTreeItem>();

        private static ClipboardWatcher _clipboardWatcher;

        private static object _thisLock = new object();

        static Clipboard()
        {
            _clipboardWatcher = new ClipboardWatcher();
            _clipboardWatcher.DrawClipboard += (sender, e) =>
            {
                lock (_thisLock)
                {
                    _isNodesCached = false;
                    _isTagsCached = false;
                    _isLinkOptionsCached = false;
                    _isSeedsCached = false;

                    _nodeList.Clear();
                    _seedList.Clear();
                    _linkOptionList.Clear();

                    _sectionCategorizeTreeItemList.Clear();
                    _sectionTreeItemList.Clear();

                    _chatCategorizeTreeItemList.Clear();
                    _chatTreeItemList.Clear();
                }
            };
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
                if (!_isNodesCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var node = LairConverter.FromNodeString(item);
                            if (node == null) continue;

                            _nodeList.Add(node);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isNodesCached = true;
                }

                return _nodeList.Count != 0;
            }
        }

        public static IEnumerable<Node> GetNodes()
        {
            lock (_thisLock)
            {
                if (!_isNodesCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var node = LairConverter.FromNodeString(item);
                            if (node == null) continue;

                            _nodeList.Add(node);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isNodesCached = true;
                }

                return _nodeList.ToArray();
            }
        }

        public static void SetNodes(IEnumerable<Node> nodes)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in nodes)
                    {
                        sb.AppendLine(LairConverter.ToNodeString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                _nodeList.AddRange(nodes);
                _isNodesCached = true;
            }
        }

        public static IEnumerable<LinkOption> GetLinkOptions()
        {
            lock (_thisLock)
            {
                if (!_isLinkOptionsCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var list = item.Split(new char[] { ',' }, 1);

                            if (list.Length == 1)
                            {
                                var link = LairConverter.FromLinkString(list[0]);
                                if (link == null) continue;

                                var option = list[1];

                                _linkOptionList.Add(new LinkOption(link, option));
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isLinkOptionsCached = true;
                }

                return _linkOptionList.ToArray();
            }
        }

        public static void SetLinkOptions(IEnumerable<LinkOption> linkOptions)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in linkOptions)
                    {
                        sb.AppendLine(string.Format("{0},{1}", LairConverter.ToLinkString(item.Link), item.Option));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                _linkOptionList.AddRange(linkOptions);
                _isLinkOptionsCached = true;
            }
        }

        public static bool ContainsSeeds()
        {
            lock (_thisLock)
            {
                if (!_isSeedsCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var seed = a.AmoebaConverter.FromSeedString(item);
                            if (seed == null) continue;

                            if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                            _seedList.Add(seed);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isSeedsCached = true;
                }

                return _seedList.Count != 0;
            }
        }

        public static IEnumerable<a.Seed> GetSeeds()
        {
            lock (_thisLock)
            {
                if (!_isSeedsCached)
                {
                    foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            var seed = a.AmoebaConverter.FromSeedString(item);
                            if (seed == null) continue;

                            if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                            _seedList.Add(seed);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _isSeedsCached = true;
                }

                return _seedList.Select(n => n.Clone()).ToArray();
            }
        }

        public static void SetSeeds(IEnumerable<a.Seed> seeds)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in seeds)
                    {
                        sb.AppendLine(a.AmoebaConverter.ToSeedString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                _seedList.AddRange(seeds.Select(n => n.Clone()));
                _isSeedsCached = true;
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
                System.Windows.Clipboard.Clear();

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
                {
                    var sb = new StringBuilder();

                    foreach (var item in sectionTreeItems)
                    {
                        var link = new Link(item.Tag, "Section", null);
                        sb.AppendLine(LairConverter.ToLinkString(link));
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
                System.Windows.Clipboard.Clear();

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
                System.Windows.Clipboard.Clear();

                _chatTreeItemList.AddRange(chatTreeItems.Select(n => n.Clone()));
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
