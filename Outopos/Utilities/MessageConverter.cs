using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Outopos.Properties;
using Library.Io;
using Library.Net.Outopos;
using Library.Security;
using Library;
using Outopos.Windows;
using A = Library.Net.Amoeba;

namespace Outopos
{
    static class MessageConverter
    {
        public static string ToSectionString(Section tag)
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

        public static string ToWikiString(Wiki tag)
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

        public static string ToChatString(Chat tag)
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

        public static string ToInfoMessage(Section tag, string option)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(tag.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Name, tag.Name));
                if (tag.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Id, NetworkConverter.ToBase64UrlString(tag.Id)));
                if (!string.IsNullOrWhiteSpace(option)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Option, option));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Wiki tag, string option)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(tag.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Name, tag.Name));
                if (tag.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Id, NetworkConverter.ToBase64UrlString(tag.Id)));
                if (!string.IsNullOrWhiteSpace(option)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Option, option));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Chat tag, string option)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(tag.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Name, tag.Name));
                if (tag.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Id, NetworkConverter.ToBase64UrlString(tag.Id)));
                if (!string.IsNullOrWhiteSpace(option)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Option, option));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
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
    }
}
