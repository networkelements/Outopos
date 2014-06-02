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

        public static bool ContainsTags()
        {
            lock (_thisLock)
            {
                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item.StartsWith("Tag:")) return true;
                }

                return false;
            }
        }

        public static IEnumerable<Tuple<Tag, string>> GetTags()
        {
            lock (_thisLock)
            {
                var list = new List<Tuple<Tag, string>>();

                foreach (var item in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string option;

                        var tag = OutoposConverter.FromTagString(item, out option);
                        if (tag == null) continue;

                        list.Add(new Tuple<Tag, string>(tag, option));
                    }
                    catch (Exception)
                    {

                    }
                }

                return list;
            }
        }

        public static void SetTags(IEnumerable<Tuple<Tag, string>> sections)
        {
            lock (_thisLock)
            {
                {
                    var sb = new StringBuilder();

                    foreach (var tuple in sections)
                    {
                        sb.AppendLine(OutoposConverter.ToTagString(tuple.Item1, tuple.Item2));
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

        public static bool ContainsSectionCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Outopos_SectionCategorizeTreeItems");
            }
        }

        public static IEnumerable<Windows.SectionCategorizeTreeItem> GetSectionCategorizeTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Outopos_SectionCategorizeTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.SectionCategorizeTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.SectionCategorizeTreeItem[0];
            }
        }

        public static void SetSectionCategorizeTreeItems(IEnumerable<Windows.SectionCategorizeTreeItem> items)
        {
            lock (_thisLock)
            {
                System.Windows.DataObject dataObject = new System.Windows.DataObject();
                dataObject.SetData("Outopos_SectionCategorizeTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }

        public static bool ContainsSectionTreeItems()
        {
            lock (_thisLock)
            {
                return System.Windows.Clipboard.ContainsData("Outopos_SectionTreeItems");
            }
        }

        public static IEnumerable<Windows.SectionTreeItem> GetSectionTreeItems()
        {
            lock (_thisLock)
            {
                try
                {
                    using (Stream stream = (Stream)System.Windows.Clipboard.GetData("Outopos_SectionTreeItems"))
                    {
                        return Clipboard.FromStream<IEnumerable<Windows.SectionTreeItem>>(stream);
                    }
                }
                catch (Exception)
                {

                }

                return new Windows.SectionTreeItem[0];
            }
        }

        public static void SetSectionTreeItems(IEnumerable<Windows.SectionTreeItem> items)
        {
            lock (_thisLock)
            {
                System.Windows.DataObject dataObject = new System.Windows.DataObject();

                {
                    var sb = new StringBuilder();

                    foreach (var item in items)
                    {
                        sb.AppendLine(OutoposConverter.ToTagString(item.Tag, item.LeaderSignature));
                    }

                    Clipboard.SetText(sb.ToString());
                }

                dataObject.SetData("Outopos_SectionTreeItems", Clipboard.ToStream(items));

                System.Windows.Clipboard.SetDataObject(dataObject);
            }
        }
    }
}