using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Library.Security;
using System.Security.Cryptography;
using Library.Io;

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
                            .Replace('+', '-').Replace('/', '_').Substring(0, 30);
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
                            .Replace('+', '-').Replace('/', '_').Substring(0, 30);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }
    }
}
