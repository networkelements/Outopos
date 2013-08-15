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
        private static List<SectionCategorizeTreeItem> _sectionCategorizeTreeItemList = new List<SectionCategorizeTreeItem>();
        private static List<SectionTreeItem> _sectionTreeItemList = new List<SectionTreeItem>();
        private static List<ChannelCategorizeTreeItem> _channelCategorizeTreeItemList = new List<ChannelCategorizeTreeItem>();
        private static List<ChannelTreeItem> _channelTreeItemList = new List<ChannelTreeItem>();

        private static ClipboardWatcher _clipboardWatcher;

        private static object _thisLock = new object();

        static Clipboard()
        {
            _clipboardWatcher = new ClipboardWatcher();
            _clipboardWatcher.DrawClipboard += (sender, e) =>
            {
                _sectionCategorizeTreeItemList.Clear();
                _sectionTreeItemList.Clear();
                _channelCategorizeTreeItemList.Clear();
                _channelTreeItemList.Clear();
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

                return list.Where(n => n != null);
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

                return list.Where(n => n != null);
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
                        if (seed == null) continue;

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

        public static IEnumerable<SectionCategorizeTreeItem> GetSectionCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _sectionCategorizeTreeItemList.ToArray();
            }
        }

        public static void SetSectionCategorizeTreeItems(IEnumerable<SectionCategorizeTreeItem> items)
        {
            lock (_thisLock)
            {
                _sectionCategorizeTreeItemList.Clear();
                _sectionCategorizeTreeItemList.AddRange(items.Select(n => n.DeepClone()));
            }
        }

        public static IEnumerable<SectionTreeItem> GetSectionTreeItems()
        {
            lock (_thisLock)
            {
                var list = new List<SectionTreeItem>(_sectionTreeItemList);

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!item.StartsWith("Section@")) continue;

                    try
                    {
                        string leaderSignature;
                        var section = LairConverter.FromSectionString(item, out leaderSignature);
                        if (list.Any(n => n.Section == section && n.LeaderSignature == leaderSignature)) continue;

                        list.Add(new SectionTreeItem() { Section = section, LeaderSignature = leaderSignature });
                    }
                    catch (Exception)
                    {

                    }
                }

                return list.Where(n => n != null);
            }
        }

        public static void SetSectionTreeItems(IEnumerable<SectionTreeItem> items)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in items)
                    {
                        sb.AppendLine(LairConverter.ToSectionString(item.Section, item.LeaderSignature));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                {
                    _sectionTreeItemList.Clear();
                    _sectionTreeItemList.AddRange(items.Select(n => n.DeepClone()));
                }
            }
        }

        public static IEnumerable<ChannelCategorizeTreeItem> GetChannelCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return _channelCategorizeTreeItemList.ToArray();
            }
        }

        public static void SetChannelCategorizeTreeItems(IEnumerable<ChannelCategorizeTreeItem> items)
        {
            lock (_thisLock)
            {
                _channelCategorizeTreeItemList.Clear();
                _channelCategorizeTreeItemList.AddRange(items.Select(n => n.DeepClone()));
            }
        }

        public static IEnumerable<ChannelTreeItem> GetChannelTreeItems()
        {
            lock (_thisLock)
            {
                var list = new List<ChannelTreeItem>(_channelTreeItemList);

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!item.StartsWith("Channel@")) continue;

                    try
                    {
                        var channel = LairConverter.FromChannelString(item);
                        if (list.Any(n => n.Channel == channel)) continue;

                        list.Add(new ChannelTreeItem() { Channel = channel });
                    }
                    catch (Exception)
                    {

                    }
                }

                return list.Where(n => n != null);
            }
        }

        public static void SetChannelTreeItems(IEnumerable<ChannelTreeItem> items)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var item in items)
                    {
                        sb.AppendLine(LairConverter.ToChannelString(item.Channel));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                {
                    _channelTreeItemList.Clear();
                    _channelTreeItemList.AddRange(items.Select(n => n.DeepClone()));
                }
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
