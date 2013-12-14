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
using Library.Io;

namespace Lair.Windows
{
    [DataContract(Name = "SectionProfilePack", Namespace = "http://Lair/Windows")]
    class SectionProfilePack
    {
        private SectionProfileHeader _header;
        private SectionProfileContent _content;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SectionProfilePack(SectionProfileHeader header, SectionProfileContent content)
        {
            this.Header = header;
            this.Content = content;
        }

        private object ThisLock
        {
            get
            {
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        [DataMember(Name = "Header")]
        public SectionProfileHeader Header
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _header;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _header = value;
                }
            }
        }

        [DataMember(Name = "Content")]
        public SectionProfileContent Content
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _content;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _content = value;
                }
            }
        }
    }
}
