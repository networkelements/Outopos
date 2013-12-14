using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Library.Net.Caps;
using Library.Net.Connections;
using Library.Net.Proxy;
using Library;
using Library.Net.Lair;
using System.Threading;

namespace Lair.Windows
{
    class SectionManager : ManagerBase, IThisLock
    {
        private Section _tag;
        private string _leaderSignature;

        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private System.Threading.Timer _watchTimer;

        private List<SectionProfilePack> _sectionProfilePacks = new List<SectionProfilePack>();

        private volatile bool _disposed;
        private readonly object _thisLock = new object();

        public SectionManager(Section tag, string leaderSignature, LairManager lairManager, BufferManager bufferManager)
        {
            _tag = tag;
            _leaderSignature = leaderSignature;

            _lairManager = lairManager;
            _bufferManager = bufferManager;

            _watchTimer = new Timer(this.WatchTimer, null, new TimeSpan(0, 0, 0), new TimeSpan(0, 1, 0));
        }

        public Section Tag
        {
            get
            {
                return _tag;
            }
        }

        public string LeaderSignature
        {
            get
            {
                return _leaderSignature;
            }
        }

        private void WatchTimer(object state)
        {
            lock (this.ThisLock)
            {
                var headers = new Dictionary<string, SectionProfileHeader>();

                foreach (var item in _lairManager.GetSectionProfileHeaders(_tag))
                {
                    headers[item.Certificate.ToString()] = item;
                }

                var packs = new List<SectionProfilePack>();

                var checkedSignatures = new HashSet<string>();
                var checkingSignatures = new Queue<string>();

                checkingSignatures.Enqueue(_leaderSignature);

                while (checkingSignatures.Count != 0)
                {
                    var targetSignature = checkingSignatures.Dequeue();
                    if (targetSignature == null || checkedSignatures.Contains(targetSignature)) continue;

                    SectionProfileHeader header;

                    if (headers.TryGetValue(targetSignature, out header))
                    {
                        try
                        {
                            var content = _lairManager.GetContent(header);
                            if (content == null) continue;

                            foreach (var trustSignature in content.TrustSignatures)
                            {
                                checkingSignatures.Enqueue(trustSignature);
                            }

                            packs.Add(new SectionProfilePack(header, content));
                        }
                        catch (Exception)
                        {

                        }
                    }

                    checkedSignatures.Add(targetSignature);
                }

                _sectionProfilePacks.Clear();
                _sectionProfilePacks.AddRange(packs);
            }
        }

        public IEnumerable<SectionProfilePack> GetSectionProfile()
        {
            lock (this.ThisLock)
            {
                return _sectionProfilePacks.ToArray();
            }
        }

        public ChatTopicPack GetChatTopic(Chat tag)
        {
            lock (this.ThisLock)
            {
                var trustSigantures = new HashSet<string>(this.GetSectionProfile().SelectMany(n => n.Content.TrustSignatures));
                var headers = new List<ChatTopicHeader>();

                foreach (var item in _lairManager.GetChatTopicHeaders(tag))
                {
                    if (!trustSigantures.Contains(item.Certificate.ToString())) continue;

                    headers.Add(item);
                }

                headers.Sort((x, y) =>
                {
                    return y.CreationTime.CompareTo(x.CreationTime);
                });

                var lastHeader = headers.FirstOrDefault();
                if (lastHeader == null) return null;

                var content = _lairManager.GetContent(lastHeader);
                if (content == null) return null;

                return new ChatTopicPack(lastHeader, content);
            }
        }

        public IEnumerable<ChatMessagePack> GetChatMessage(Chat tag)
        {
            lock (this.ThisLock)
            {
                var trustSigantures = new HashSet<string>(this.GetSectionProfile().SelectMany(n => n.Content.TrustSignatures));
                var headers = new List<ChatMessageHeader>();

                foreach (var item in _lairManager.GetChatMessageHeaders(tag))
                {
                    if (!trustSigantures.Contains(item.Certificate.ToString())) continue;

                    headers.Add(item);
                }

                headers.Sort((x, y) =>
                {
                    return y.CreationTime.CompareTo(x.CreationTime);
                });

                var packs = new List<ChatMessagePack>();

                foreach (var header in headers.Take(8192))
                {
                    var content = _lairManager.GetContent(header);
                    if (content == null) return null;

                    packs.Add(new ChatMessagePack(header, content));
                    if (packs.Count > 1024) break;
                }

                return packs;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_watchTimer != null)
                {
                    try
                    {
                        _watchTimer.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _watchTimer = null;
                }
            }
        }

        #region IThisLock

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }
}
