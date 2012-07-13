using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Library;
using Library.Net.Lair;

namespace Lair
{
    static class Clipboard
    {
        public static IEnumerable<string> GetPaths()
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

        public static void SetPaths(IEnumerable<string> collection)
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

        public static string GetText()
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

        public static void SetText(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch (Exception)
            {

            }
        }

        public static IEnumerable<Node> GetNodes()
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

        public static void SetNodes(IEnumerable<Node> nodes)
        {
            var sb = new StringBuilder();

            foreach (var item in nodes)
            {
                sb.AppendLine(LairConverter.ToNodeString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        public static IEnumerable<Channel> GetChannels()
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

            return list;
        }

        public static void SetChannels(IEnumerable<Channel> channels)
        {
            var sb = new StringBuilder();

            foreach (var item in channels)
            {
                sb.AppendLine(LairConverter.ToChannelString(item));
            }

            Clipboard.SetText(sb.ToString());
        }
    }
}
