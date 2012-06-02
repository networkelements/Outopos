using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library.Collections;
using Library;

namespace Lair
{
    public class IdCollection : LockedList<string>, IEnumerable<string>
    {
        public IdCollection() : base() { }
        public IdCollection(int capacity) : base(capacity) { }
        public IdCollection(IEnumerable<string> collections) : base(collections) { }

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
