using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Xml;
using Outopos.Windows;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Outopos;
using A = Library.Net.Amoeba;

namespace Outopos
{
    static class Clipboard
    {
        private static object _thisLock = new object();

        private static Stream ToStream<T>(T item)
        {
            var ds = new DataContractSerializer(typeof(T));

            MemoryStream stream = null;

            try
            {
                stream = new MemoryStream();

                using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(wrapperStream, new UTF8Encoding(false)))
                {
                    ds.WriteObject(xmlDictionaryWriter, item);
                }
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Dispose();
            }

            return stream;
        }

        private static T FromStream<T>(Stream stream)
        {
            var ds = new DataContractSerializer(typeof(T));

            using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return (T)ds.ReadObject(xmlDictionaryReader);
            }
        }

        public static void Clear()
        {
            lock (_thisLock)
            {
                System.Windows.Clipboard.Clear();
            }
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
                        var node = OutoposConverter.FromNodeString(item);
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
                {
                    var sb = new StringBuilder();

                    foreach (var item in nodes)
                    {
                        sb.AppendLine(OutoposConverter.ToNodeString(item));
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

        public static IEnumerable<Wiki> GetWikis()
        {
            lock (_thisLock)
            {
                var list = new List<Wiki>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var tag = OutoposConverter.FromWikiString(item);
                        if (tag == null) continue;

                        list.Add(tag);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetWikis(IEnumerable<Wiki> tags)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var tag in tags)
                    {
                        sb.AppendLine(OutoposConverter.ToWikiString(tag));
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

        public static IEnumerable<Chat> GetChats()
        {
            lock (_thisLock)
            {
                var list = new List<Chat>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var tag = OutoposConverter.FromChatString(item);
                        if (tag == null) continue;

                        list.Add(tag);
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetChats(IEnumerable<Chat> tags)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var tag in tags)
                    {
                        sb.AppendLine(OutoposConverter.ToChatString(tag));
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

        public static bool ContainsChatCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Outopos_ChatCategorizeTreeItems");
            }
        }

        public static IEnumerable<Windows.ChatCategorizeTreeItem> GetChatCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Outopos_ChatCategorizeTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.ChatCategorizeTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.ChatCategorizeTreeItem[0];
            }
        }

        public static void SetChatCategorizeTreeItems(IEnumerable<Windows.ChatCategorizeTreeItem> items)
        {
            lock (_thisLock)
            {
                System.Windows.DataObject dataObject = new System.Windows.DataObject();
                dataObject.SetData("Outopos_ChatCategorizeTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsChatTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Outopos_ChatTreeItems");
            }
        }

        public static IEnumerable<Windows.ChatTreeItem> GetChatTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Outopos_ChatTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.ChatTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.ChatTreeItem[0];
            }
        }

        public static void SetChatTreeItems(IEnumerable<Windows.ChatTreeItem> items)
        {
            lock (_thisLock)
            {
                System.Windows.DataObject dataObject = new System.Windows.DataObject();

                {
                    var sb = new StringBuilder();

                    foreach (var item in items)
                    {
                        sb.AppendLine(OutoposConverter.ToChatString(item.Tag));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                dataObject.SetData("Outopos_ChatTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }
    }
}
