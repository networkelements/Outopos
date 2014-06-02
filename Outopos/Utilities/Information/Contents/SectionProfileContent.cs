using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Library;
using Library.Io;
using Library.Net.Outopos;
using Library.Security;

namespace Outopos
{
    [DataContract(Name = "SectionProfileContent", Namespace = "http://Library/Net/Outopos")]
    sealed class SectionProfileContent : ItemBase<SectionProfileContent>
    {
        private enum SerializeId : byte
        {
            ExchangePublicKey = 0,
            TrustSignature = 1,
            Tag = 2,
        }

        private ExchangePublicKey _exchangePublicKey;
        private SignatureCollection _trustSignatures;
        private TagCollection _tags;

        public static readonly int MaxTrustSignatureCount = 1024;
        public static readonly int MaxTagCount = 256;

        public SectionProfileContent(ExchangePublicKey exchangePublicKey, IEnumerable<string> trustSignatures, IEnumerable<Tag> tags)
        {
            this.ExchangePublicKey = exchangePublicKey;
            if (trustSignatures != null) this.ProtectedTrustSignatures.AddRange(trustSignatures);
            if (tags != null) this.ProtectedTags.AddRange(tags);
        }

        protected override void ProtectedImport(Stream stream, BufferManager bufferManager, int count)
        {
            byte[] lengthBuffer = new byte[4];

            for (; ; )
            {
                if (stream.Read(lengthBuffer, 0, lengthBuffer.Length) != lengthBuffer.Length) return;
                int length = NetworkConverter.ToInt32(lengthBuffer);
                byte id = (byte)stream.ReadByte();

                using (RangeStream rangeStream = new RangeStream(stream, stream.Position, length, true))
                {
                    if (id == (byte)SerializeId.ExchangePublicKey)
                    {
                        this.ExchangePublicKey = ExchangePublicKey.Import(rangeStream, bufferManager);
                    }
                    else if (id == (byte)SerializeId.TrustSignature)
                    {
                        this.ProtectedTrustSignatures.Add(ItemUtilities.GetString(rangeStream));
                    }
                    else if (id == (byte)SerializeId.Tag)
                    {
                        this.ProtectedTags.Add(Tag.Import(rangeStream, bufferManager));
                    }
                }
            }
        }

        protected override Stream Export(BufferManager bufferManager, int count)
        {
            BufferStream bufferStream = new BufferStream(bufferManager);

            // ExchangePublicKey
            if (this.ExchangePublicKey != null)
            {
                using (var stream = this.ExchangePublicKey.Export(bufferManager))
                {
                    ItemUtilities.Write(bufferStream, (byte)SerializeId.ExchangePublicKey, stream);
                }
            }
            // TrustSignatures
            foreach (var value in this.TrustSignatures)
            {
                ItemUtilities.Write(bufferStream, (byte)SerializeId.TrustSignature, value);
            }
            // Tags
            foreach (var value in this.Tags)
            {
                using (var stream = value.Export(bufferManager))
                {
                    ItemUtilities.Write(bufferStream, (byte)SerializeId.Tag, stream);
                }
            }

            bufferStream.Seek(0, SeekOrigin.Begin);
            return bufferStream;
        }

        public override int GetHashCode()
        {
            if (this.ExchangePublicKey == null) return 0;
            else return this.ExchangePublicKey.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SectionProfileContent)) return false;

            return this.Equals((SectionProfileContent)obj);
        }

        public override bool Equals(SectionProfileContent other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.ExchangePublicKey != other.ExchangePublicKey
                || (this.TrustSignatures == null) != (other.TrustSignatures == null)
                || (this.Tags == null) != (other.Tags == null))
            {
                return false;
            }

            if (this.TrustSignatures != null && other.TrustSignatures != null)
            {
                if (!CollectionUtilities.Equals(this.TrustSignatures, other.TrustSignatures)) return false;
            }

            if (this.Tags != null && other.Tags != null)
            {
                if (!CollectionUtilities.Equals(this.Tags, other.Tags)) return false;
            }

            return true;
        }

        [DataMember(Name = "ExchangePublicKey")]
        public ExchangePublicKey ExchangePublicKey
        {
            get
            {
                return _exchangePublicKey;
            }
            private set
            {
                _exchangePublicKey = value;
            }
        }

        private volatile ReadOnlyCollection<string> _readOnlyTrustSignatures;

        public IEnumerable<string> TrustSignatures
        {
            get
            {
                if (_readOnlyTrustSignatures == null)
                    _readOnlyTrustSignatures = new ReadOnlyCollection<string>(this.ProtectedTrustSignatures);

                return _readOnlyTrustSignatures;
            }
        }

        [DataMember(Name = "TrustSignatures")]
        private SignatureCollection ProtectedTrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new SignatureCollection(SectionProfileContent.MaxTrustSignatureCount);

                return _trustSignatures;
            }
        }

        private volatile ReadOnlyCollection<Tag> _readOnlyTags;

        public IEnumerable<Tag> Tags
        {
            get
            {
                if (_readOnlyTags == null)
                    _readOnlyTags = new ReadOnlyCollection<Tag>(this.ProtectedTags);

                return _readOnlyTags;
            }
        }

        [DataMember(Name = "Tags")]
        private TagCollection ProtectedTags
        {
            get
            {
                if (_tags == null)
                    _tags = new TagCollection(SectionProfileContent.MaxTagCount);

                return _tags;
            }
        }
    }
}
