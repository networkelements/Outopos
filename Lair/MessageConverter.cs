﻿using System;
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

namespace Lair
{
    static class MessageConverter
    {
        public static string ToSectionString(Section section)
        {
            if (section.Name == null || section.Id == null) return null;

            try
            {
                return section.Name + " - " + NetworkConverter.ToBase64String(section.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToChannelString(Channel channel)
        {
            if (channel.Name == null || channel.Id == null) return null;

            try
            {
                return channel.Name + " - " + NetworkConverter.ToBase64String(channel.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Library.Net.Amoeba.Seed seed)
        {
            try
            {
                var keywords = seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(seed.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Name, seed.Name));
                if (seed.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Signature, seed.Certificate.ToString()));
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

        public static string ToInfoMessage(Section section)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(section.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Section_Name, section.Name));
                if (section.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Section_Id, NetworkConverter.ToBase64String(section.Id)));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Channel channel)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(channel.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Channel_Name, channel.Name));
                if (channel.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Channel_Id, NetworkConverter.ToBase64String(channel.Id)));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Message message)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (message.Channel != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Message_Channel, MessageConverter.ToChannelString(message.Channel)));
                if (message.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Message_Signature, message.Certificate.ToString()));
                builder.AppendLine(string.Format("{0}: {1} UTC", LanguagesManager.Instance.Message_CreationTime, message.CreationTime.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                if (!string.IsNullOrWhiteSpace(message.Content)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Message_Content, message.Content));

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
