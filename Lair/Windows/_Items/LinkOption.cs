using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Library.Net.Lair;

namespace Lair.Windows
{
    [DataContract(Name = "LinkOption", Namespace = "http://Lair/Windows")]
    class LinkOption
    {
        private Link _link;
        private string _option;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public LinkOption(Link link, string option)
        {
            this.Link = link;
            this.Option = option;
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

        [DataMember(Name = "Link")]
        public Link Link
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _link;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _link = value;
                }
            }
        }

        [DataMember(Name = "Option")]
        public string Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _option;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _option = value;
                }
            }
        }
    }
}
