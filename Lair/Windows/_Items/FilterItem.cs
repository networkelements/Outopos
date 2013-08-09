using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Lair;
using Library.Security;
using Library.Collections;
using System.Text.RegularExpressions;

namespace Lair.Windows
{
    [DataContract(Name = "FilterItem", Namespace = "http://Lair/Windows")]
    class FilterItem : IDeepCloneable<FilterItem>, IThisLock
    {
        private string _name = "default";
        private LockedList<SearchContains<string>> _searchWordCollection;
        private LockedList<SearchContains<SearchRegex>> _searchRegexCollection;
        private LockedList<SearchContains<SearchRegex>> _searchSignatureCollection;
        private LockedList<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _name;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = "SearchWordCollection")]
        public LockedList<SearchContains<string>> SearchWordCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchWordCollection == null)
                        _searchWordCollection = new LockedList<SearchContains<string>>();

                    return _searchWordCollection;
                }
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public LockedList<SearchContains<SearchRegex>> SearchRegexCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchRegexCollection == null)
                        _searchRegexCollection = new LockedList<SearchContains<SearchRegex>>();

                    return _searchRegexCollection;
                }
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public LockedList<SearchContains<SearchRegex>> SearchSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSignatureCollection == null)
                        _searchSignatureCollection = new LockedList<SearchContains<SearchRegex>>();

                    return _searchSignatureCollection;
                }
            }
        }

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public LockedList<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchCreationTimeRangeCollection == null)
                        _searchCreationTimeRangeCollection = new LockedList<SearchContains<SearchRange<DateTime>>>();

                    return _searchCreationTimeRangeCollection;
                }
            }
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("Name = {0}", this.Name);
            }
        }

        #region IDeepClone<SearchItem>

        public FilterItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(FilterItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (FilterItem)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Lair/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>, IDeepCloneable<SearchContains<T>>
    {
        private bool _contains;
        private T _value;

        [DataMember(Name = "Contains")]
        public bool Contains
        {
            get
            {
                return _contains;
            }
            set
            {
                _contains = value;
            }
        }

        [DataMember(Name = "Value")]
        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchContains<T>)) return false;

            return this.Equals((SearchContains<T>)obj);
        }

        public bool Equals(SearchContains<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.Contains != other.Contains)
                || (!this.Value.Equals(other.Value)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Contains, this.Value);
        }

        #region IDeepClone<SearchContains<T>>

        public SearchContains<T> DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchContains<T>));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchContains<T>)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Lair/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, IDeepCloneable<SearchRegex>
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        [DataMember(Name = "Value")]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.RegexUpdate();
            }
        }

        [DataMember(Name = "IsIgnoreCase")]
        public bool IsIgnoreCase
        {
            get
            {
                return _isIgnoreCase;
            }
            set
            {
                _isIgnoreCase = value;

                this.RegexUpdate();
            }
        }

        private void RegexUpdate()
        {
            var o = RegexOptions.Compiled | RegexOptions.Singleline;
            if (_isIgnoreCase) o |= RegexOptions.IgnoreCase;

            if (_value != null) _regex = new Regex(_value, o);
            else _regex = null;
        }

        public bool IsMatch(string value)
        {
            if (_regex == null) return false;

            return _regex.IsMatch(value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRegex)) return false;

            return this.Equals((SearchRegex)obj);
        }

        public bool Equals(SearchRegex other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.IsIgnoreCase != other.IsIgnoreCase)
                || (this.Value != other.Value))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.IsIgnoreCase, this.Value);
        }

        #region IDeepClone<SearchRegex>

        public SearchRegex DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchRegex));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchRegex)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Lair/Windows")]
    class SearchRange<T> : IEquatable<SearchRange<T>>, IDeepCloneable<SearchRange<T>>
        where T : IComparable
    {
        T _max;
        T _min;

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
                _min = (_min.CompareTo(_max) > 0) ? _max : _min;
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
                _max = (_max.CompareTo(_min) < 0) ? _min : _max;
            }
        }

        public bool Verify(T value)
        {
            if (value.CompareTo(this.Min) < 0 || value.CompareTo(this.Max) > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override int GetHashCode()
        {
            return this.Min.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRange<T>)) return false;

            return this.Equals((SearchRange<T>)obj);
        }

        public bool Equals(SearchRange<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((!this.Min.Equals(other.Min))
                || (!this.Max.Equals(other.Max)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
        }

        #region IDeepClone<SearchRange<T>>

        public SearchRange<T> DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchRange<T>));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchRange<T>)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }
}
