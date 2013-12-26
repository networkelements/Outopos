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
using A = Library.Net.Amoeba;

namespace Lair
{
    static class MessageConverter
    {
        public static string ToSectionString(Section section)
        {
            if (section.Name == null || section.Id == null) return null;

            try
            {
                return section.Name + " - " + NetworkConverter.ToBase64UrlString(section.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToWikiString(Wiki wiki)
        {
            if (wiki.Name == null || wiki.Id == null) return null;

            try
            {
                return wiki.Name + " - " + NetworkConverter.ToBase64UrlString(wiki.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToChatString(Chat chat)
        {
            if (chat.Name == null || chat.Id == null) return null;

            try
            {
                return chat.Name + " - " + NetworkConverter.ToBase64UrlString(chat.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(A.Seed seed)
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

        public static string ToInfoMessage(Section section, string option)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(section.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Section_Name, section.Name));
                if (section.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Section_Id, NetworkConverter.ToBase64UrlString(section.Id)));
                if (!string.IsNullOrWhiteSpace(option)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Section_Option, option));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Wiki wiki, string option)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(wiki.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Wiki_Name, wiki.Name));
                if (wiki.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Wiki_Id, NetworkConverter.ToBase64UrlString(wiki.Id)));
                if (!string.IsNullOrWhiteSpace(option)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Wiki_Option, option));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Chat chat, string option)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(chat.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Chat_Name, chat.Name));
                if (chat.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Chat_Id, NetworkConverter.ToBase64UrlString(chat.Id)));
                if (!string.IsNullOrWhiteSpace(option)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Chat_Option, option));

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
