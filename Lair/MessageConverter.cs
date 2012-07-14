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

namespace Lair
{
    static class MessageConverter
    {
        public static string ToSignatureString(DigitalSignature digitalSignature)
        {
            if (digitalSignature == null || digitalSignature.Nickname == null || digitalSignature.PublicKey == null) return null;

            try
            {
                using (var sha512 = new SHA512Managed())
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (StreamWriter writer = new StreamWriter(new RangeStream(memoryStream, true), new UTF8Encoding(false)))
                        {
                            writer.Write(digitalSignature.Nickname);
                        }

                        memoryStream.Write(digitalSignature.PublicKey, 0, digitalSignature.PublicKey.Length);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        return digitalSignature.Nickname.Replace("@", "_") + "@" + Convert.ToBase64String(sha512.ComputeHash(memoryStream).ToArray())
                            .Replace('+', '-').Replace('/', '_').Substring(0, 64);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToSignatureString(Certificate certificate)
        {
            if (certificate == null || certificate.Nickname == null || certificate.PublicKey == null) return null;

            try
            {
                using (var sha512 = new SHA512Managed())
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (StreamWriter writer = new StreamWriter(new RangeStream(memoryStream, true), new UTF8Encoding(false)))
                        {
                            writer.Write(certificate.Nickname);
                        }

                        memoryStream.Write(certificate.PublicKey, 0, certificate.PublicKey.Length);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        return certificate.Nickname.Replace("@", "_") + "@" + Convert.ToBase64String(sha512.ComputeHash(memoryStream).ToArray())
                            .Replace('+', '-').Replace('/', '_').Substring(0, 64);
                    }
                }
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
                return channel.Name + " - " + Convert.ToBase64String(channel.Id)
                    .Replace('+', '-').Replace('/', '_').Substring(0, 64);
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

                if (!string.IsNullOrWhiteSpace(seed.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Name, seed.Name));
                if (seed.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Signature, MessageConverter.ToSignatureString(seed.Certificate)));
                builder.AppendLine(string.Format("{0}: {1:#,0}", LanguagesManager.Instance.ChannelControl_Length, seed.Length));
                if (keywords.Count != 0) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Keywords, String.Join(", ", keywords)));
                builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_CreationTime, seed.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                if (!string.IsNullOrWhiteSpace(seed.Comment)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Comment, seed.Comment));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Library.Net.Amoeba.Box box)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(box.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Name, box.Name));
                if (box.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Signature, MessageConverter.ToSignatureString(box.Certificate)));
                builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_CreationTime, box.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                if (!string.IsNullOrWhiteSpace(box.Comment)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Comment, box.Comment));

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

                if (!string.IsNullOrWhiteSpace(channel.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Name, channel.Name));
                if (channel.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.ChannelControl_Id, Convert.ToBase64String(channel.Id)
                    .Replace('+', '-').Replace('/', '_').Substring(0, 64)));

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
