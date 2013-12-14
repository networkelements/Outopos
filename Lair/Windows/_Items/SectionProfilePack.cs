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
    [DataContract(Name = "ChatMessagePack", Namespace = "http://Lair/Windows")]
    class ChatMessagePack
    {
        private ChatMessageHeader _header;
        private ChatMessageContent _content;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatMessagePack(ChatMessageHeader header, ChatMessageContent content)
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
        public ChatMessageHeader Header
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
        public ChatMessageContent Content
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
