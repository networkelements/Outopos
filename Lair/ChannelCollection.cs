﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library;
using Library.Collections;

namespace Lair
{
    public class ChannelCollection : LockedList<string>, IEnumerable<string>
    {
        public ChannelCollection() : base() { }
        public ChannelCollection(int capacity) : base(capacity) { }
        public ChannelCollection(IEnumerable<string> collections) : base(collections) { }

        #region IEnumerable<string> メンバ

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            using (DeadlockMonitor.Lock(base.ThisLock))
            {
                return base.GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable メンバ

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            using (DeadlockMonitor.Lock(base.ThisLock))
            {
                return this.GetEnumerator();
            }
        }

        #endregion
    }
}
