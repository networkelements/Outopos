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
    [DataContract(Name = "ChatTopicPack", Namespace = "http://Lair/Windows")]
    class ChatTopicPack
    {
        private ChatTopicHeader _header;
        private ChatTopicContent _content;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatTopicPack(ChatTopicHeader header, ChatTopicContent content)
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
        public ChatTopicHeader Header
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
        public ChatTopicContent Content
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
