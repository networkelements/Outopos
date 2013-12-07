using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lair.Properties;
using Library.Io;
using Library.Net.Lair;
using Library.Security;
using Library;
using Lair.Windows;
using a = Library.Net.Amoeba;

namespace Lair
{
    static class MessageConverter
    {
        public static string ToTagString(Tag tag)
        {
            if (tag.Name == null || tag.Id == null) return null;

            try
            {
                return tag.Name + " - " + NetworkConverter.ToBase64UrlString(tag.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(a.Seed seed)
        {
            if (seed == null) throw new ArgumentNullException("seed");

            try
            {
                var keywords = seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(seed.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Name, seed.Name));
                if (seed.Certificate != null) builder.AppendLine(string.Format("{0}:\u00A0{1}", LanguagesManager.Instance.Seed_Signature, seed.Certificate.ToString()));
                builder.AppendLine(string.Format("{0}: {1:#,0}", LanguagesManager.Instance.Seed_Length, seed.Length));
                if (keywords.Count != 0) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Keywords, String.Join(", ", keywords)));
                builder.AppendLine(string.Format("{0}: {1} UTC", LanguagesManager.Instance.Seed_CreationTime, seed.CreationTime.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                if (!string.IsNullOrWhiteSpace(seed.Comment)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Comment, seed.Comment));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(LinkOption linkOption)
        {
            if (linkOption == null) throw new ArgumentNullException("linkOption");

            try
            {
                StringBuilder builder = new StringBuilder();

                if (linkOption.Link != null)
                {

                    var link = linkOption.Link;

                    if (link.Tag != null)
                    {
                        var tag = link.Tag;

                        if (!string.IsNullOrWhiteSpace(tag.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Link_Tag_Name, tag.Name));
                        if (tag.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Link_Tag_Id, NetworkConverter.ToBase64UrlString(tag.Id)));
                    }

                    if (!string.IsNullOrWhiteSpace(link.Type)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Link_Type, link.Type));
                    if (!string.IsNullOrWhiteSpace(link.Path)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Link_Path, link.Path));

                }

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }
    }
}
