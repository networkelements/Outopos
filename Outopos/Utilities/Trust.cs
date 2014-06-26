using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library.Collections;

namespace Outopos
{
    static class Trust
    {
        private static LockedHashSet<string> _trustSignatures = new LockedHashSet<string>();

        public static bool ContainSignature(string signature)
        {
            lock (_trustSignatures.ThisLock)
            {
                return _trustSignatures.Contains(signature);
            }
        }

        public static IEnumerable<string> GetSignatures()
        {
            lock (_trustSignatures.ThisLock)
            {
                return _trustSignatures.ToList();
            }
        }

        public static void SetSignatures(IEnumerable<string> signatures)
        {
            lock (_trustSignatures.ThisLock)
            {
                _trustSignatures.Clear();
                _trustSignatures.UnionWith(signatures);
            }
        }

        private static int _limit;

        public static int GetLimit()
        {
            return _limit;
        }

        public static void SetLimit(int limit)
        {
            _limit = limit;
        }
    }
}