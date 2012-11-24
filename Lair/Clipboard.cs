﻿using System;
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
        private static List<Category> _categoryList = new List<Category>();
        private static List<Board> _boardList = new List<Board>();
        private static LockedList<Windows.SearchTreeItem> _searchTreeItemList = new LockedList<Windows.SearchTreeItem>();

        private static ClipboardWatcher _clipboardWatcher;

        private static object _thisLock = new object();

        static Clipboard()
        {
            _clipboardWatcher = new ClipboardWatcher();
            _clipboardWatcher.DrawClipboard += (sender2, e2) =>
            {
                _categoryList.Clear();
                _boardList.Clear();
                _searchTreeItemList.Clear();
            };
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

        public static IEnumerable<Node> GetNodes()
        {
            lock (_thisLock)
            {
                var list = new List<Node>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!item.StartsWith("Node@")) continue;

                    try
                    {
                        list.Add(LairConverter.FromNodeString(item));
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
                var sb = new StringBuilder();

                foreach (var item in nodes)
                {
                    sb.AppendLine(LairConverter.ToNodeString(item));
                }

                Clipboard.SetText(sb.ToString());
            }
        }

        public static IEnumerable<Channel> GetChannels()
        {
            lock (_thisLock)
            {
                if (_categoryList.Count != 0) return new Channel[0];

                var list = new List<Channel>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!item.StartsWith("Channel@")) continue;

                    try
                    {
                        list.Add(LairConverter.FromChannelString(item));
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetChannels(IEnumerable<Channel> channels)
        {
            lock (_thisLock)
            {
                var sb = new StringBuilder();

                foreach (var item in channels)
                {
                    sb.AppendLine(LairConverter.ToChannelString(item));
                }

                Clipboard.SetText(sb.ToString());
            }
        }

        public static IEnumerable<Board> GetBoards()
        {
            lock (_thisLock)
            {
                return _boardList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetBoards(IEnumerable<Board> boards)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in boards)
                    {
                        sb.AppendLine(LairConverter.ToChannelString(item.Channel));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                {
                    _boardList.Clear();
                    _boardList.AddRange(boards.Select(n => n.DeepClone()));
                }
            }
        }

        public static IEnumerable<Category> GetCategories()
        {
            lock (_thisLock)
            {
                return _categoryList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetCategories(IEnumerable<Category> categories)
        {
            lock (_thisLock)
            {
                {
                    List<Channel> channels = new List<Channel>();
                    List<Category> categoryList = new List<Category>();

                    categoryList.AddRange(categories);

                    for (int i = 0; i < categoryList.Count; i++)
                    {
                        categoryList.AddRange(categoryList[i].Categories);

                        var tempList = categoryList[i].Boards.Select(n => n.Channel).ToList();

                        tempList.Sort(delegate(Channel x, Channel y)
                        {
                            int c = x.Name.CompareTo(y.Name);
                            if (c != 0) return c;

                            return Collection.Compare(x.Id, y.Id);
                        });

                        channels.AddRange(tempList);
                    }

                    var sb = new StringBuilder();

                    foreach (var item in channels)
                    {
                        sb.AppendLine(LairConverter.ToChannelString(item));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                {
                    _categoryList.Clear();
                    _categoryList.AddRange(categories.Select(n => n.DeepClone()));
                }
            }
        }

        public static IEnumerable<a.Seed> GetSeeds()
        {
            lock (_thisLock)
            {
                var list = new List<a.Seed>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!item.StartsWith("Seed@")) continue;

                    try
                    {
                        var seed = a.AmoebaConverter.FromSeedString(item);
                        if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                        list.Add(seed);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetSeeds(IEnumerable<a.Seed> seeds)
        {
            lock (_thisLock)
            {
                var sb = new StringBuilder();

                foreach (var item in seeds)
                {
                    sb.AppendLine(a.AmoebaConverter.ToSeedString(item));
                }

                Clipboard.SetText(sb.ToString());
            }
        }

        public static IEnumerable<Windows.SearchTreeItem> GetSearchTreeItems()
        {
            lock (_thisLock)
            {
                return _searchTreeItemList.Select(n => n.DeepClone()).ToArray();
            }
        }

        public static void SetSearchTreeItems(IEnumerable<Windows.SearchTreeItem> searchTreeItems)
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();

                _searchTreeItemList.Clear();
                _searchTreeItemList.AddRange(searchTreeItems.Select(n => n.DeepClone()));
            }
        }

        public class ClipboardWatcher : IDisposable
        {
            private ClipBoardWatcherForm form;

            public event EventHandler DrawClipboard;

            public ClipboardWatcher()
            {
                form = new ClipBoardWatcherForm();
                form.StartWatch(raiseDrawClipboard);
            }

            ~ClipboardWatcher()
            {
                this.Dispose();
            }

            private void raiseDrawClipboard()
            {
                if (DrawClipboard != null)
                {
                    DrawClipboard(this, EventArgs.Empty);
                }
            }

            public void Dispose()
            {
                form.Dispose();
            }

            private class ClipBoardWatcherForm : System.Windows.Forms.Form
            {
                [DllImport("user32.dll")]
                private static extern IntPtr SetClipboardViewer(IntPtr hwnd);

                [DllImport("user32.dll")]
                private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

                [DllImport("user32.dll")]
                private static extern bool ChangeClipboardChain(IntPtr hwnd, IntPtr hWndNext);

                const int WM_DRAWCLIPBOARD = 0x0308;
                const int WM_CHANGECBCHAIN = 0x030D;

                IntPtr nextHandle;
                System.Threading.ThreadStart proc;

                public void StartWatch(System.Threading.ThreadStart proc)
                {
                    this.proc = proc;
                    nextHandle = SetClipboardViewer(this.Handle);
                }

                protected override void WndProc(ref System.Windows.Forms.Message m)
                {
                    if (m.Msg == WM_DRAWCLIPBOARD)
                    {
                        SendMessage(nextHandle, m.Msg, m.WParam, m.LParam);
                        proc();
                    }
                    else if (m.Msg == WM_CHANGECBCHAIN)
                    {
                        if (m.WParam == nextHandle)
                        {
                            nextHandle = m.LParam;
                        }
                        else
                        {
                            SendMessage(nextHandle, m.Msg, m.WParam, m.LParam);
                        }
                    }

                    base.WndProc(ref m);
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        ChangeClipboardChain(this.Handle, nextHandle);
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
