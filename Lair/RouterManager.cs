using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Connection;
using Library.Net.Lair;
using Library.Security;
using System.Diagnostics;

namespace Lair
{
    public class RouterManager : StateManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private Settings _settings;
        private BufferManager _bufferManager;

        private List<TcpListener> _listeners = new List<TcpListener>();

        private LockedList<ConnectionManager> _connectionManagers = new LockedList<ConnectionManager>();
        private LockedDictionary<string, WaitQueue<IEnumerable<CommandMessage>>> _sendMessageQueue = new LockedDictionary<string, WaitQueue<IEnumerable<CommandMessage>>>();
        private LockedQueue<KeyValuePair<string, CommandMessage>> _receiveMessageQueue = new LockedQueue<KeyValuePair<string, CommandMessage>>();

        private volatile Thread _addConnectionManagerThread = null;
        private volatile Thread _connectionsManagerThread = null;

        private LockedDictionary<ConnectionManager, string> _connectionId = new LockedDictionary<ConnectionManager, string>();
        private LockedDictionary<string, HashSet<string>> _channelUsersDictionary = new LockedDictionary<string, HashSet<string>>();
        private LockedDictionary<string, CirculationCollection<KeyValuePair<string, string>>> _invite = new LockedDictionary<string, CirculationCollection<KeyValuePair<string, string>>>();

        private ManagerState _state = ManagerState.Stop;

        private bool _disposed = false;
        private object _thisLock = new object();

        private const int MaxReceiveCount = 1 * 1024 * 1024;

        public RouterManager(BufferManager bufferManager)
        {
            _settings = new Settings();
            _bufferManager = bufferManager;
        }

        public DigitalSignature DigitalSignature
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.DigitalSignature;
                }
            }
            set
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    _settings.DigitalSignature = value;
                }
            }
        }

        public UriCollection ListenUris
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.ListenUris;
                }
            }
        }

        public int ConnectionCountLimit
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.ConnectionCountLimit;
                }
            }
            set
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    _settings.ConnectionCountLimit = value;
                }
            }
        }

        private static string[] Split(string message)
        {
            var list = message.Split(new string[] { "\r\n", "\r", "\n", " " }, StringSplitOptions.None);
            var list2 = new List<string>();
            bool flag = false;

            for (int i = 0; i < list.Length; i++)
            {
                if (!flag)
                {
                    flag = list[i].StartsWith("\"");

                    if (flag)
                    {
                        list2.Add(list[i].Substring(1, list[i].Length - 1));
                    }
                    else
                    {
                        list2.Add(list[i]);
                    }
                }
                else
                {
                    flag = !list[i].EndsWith("\"");

                    if (!flag)
                    {
                        list2[list2.Count - 1] += " " + list[i].Substring(0, list[i].Length - 2);
                    }
                    else
                    {
                        list2[list2.Count - 1] += " " + list[i];
                    }
                }
            }

            return list2.ToArray();
        }

        private static ArraySegment<byte> ToBuffer(IEnumerable<byte[]> buffers, BufferManager bufferManager)
        {
            int length = buffers.Sum(n => n.Length);
            byte[] buffer = bufferManager.TakeBuffer(length);

            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                foreach (var buffer2 in buffers)
                {
                    memoryStream.Write(NetworkConverter.GetBytes((int)buffer2.Length), 0, 4);
                    memoryStream.Write(buffer2, 0, buffer2.Length);
                }
            }

            return new ArraySegment<byte>(buffer, 0, length);
        }

        private static ArraySegment<byte>[] FromBuffer(ArraySegment<byte> buffer, BufferManager bufferManager)
        {
            List<ArraySegment<byte>> list = new List<ArraySegment<byte>>();

            using (MemoryStream memoryStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count))
            {
                byte[] lengthBuffer = new byte[4];

                for (; ; )
                {
                    if (memoryStream.Read(lengthBuffer, 0, 4) < 4) break;
                    int length = NetworkConverter.ToInt32(lengthBuffer);

                    byte[] buffer2 = bufferManager.TakeBuffer(length);
                    if (memoryStream.Read(buffer2, 0, length) < length) break;

                    list.Add(new ArraySegment<byte>(buffer2, 0, length));
                }
            }

            return list.ToArray();
        }

        private ConnectionBase AcceptConnection(out string id)
        {
            id = null;

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                if (this.State == ManagerState.Stop)
                    return null;

                try
                {
                    ConnectionBase connection = null;

                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        if (_listeners[i].Pending())
                        {
                            var socket = _listeners[i].AcceptTcpClient().Client;

                            connection = new TcpConnection(socket, RouterManager.MaxReceiveCount, _bufferManager);
                            break;
                        }
                    }

                    if (connection != null)
                    {
                        var secureConnection = new SecureServerConnection(connection, this.DigitalSignature, _bufferManager);
                        secureConnection.Connect(new TimeSpan(0, 0, 20));
                        id = MessageConverter.ToSignatureString(secureConnection.Certificate);

                        return new CompressConnection(secureConnection, RouterManager.MaxReceiveCount, _bufferManager);
                    }
                }
                catch (Exception)
                {

                }

                return null;
            }
        }

        private void AddConnectionManagerThread()
        {
            for (; ; )
            {
                Thread.Sleep(1000);
                if (this.State == ManagerState.Stop) return;

                string id;
                ConnectionManager connectionManager = new ConnectionManager(this.AcceptConnection(out id), _bufferManager);

                connectionManager.PullMessagesEvent += new PullMessagesEventHandler(connectionManager_PullMessagesEvent);
                connectionManager.CloseEvent += new CloseEventHandler(connectionManager_CloseEvent);

                connectionManager.Connect();

                ThreadPool.QueueUserWorkItem(new WaitCallback(this.ConnectionManagerThread), connectionManager);
                _connectionId.Add(connectionManager, id);
                _connectionManagers.Add(connectionManager);
            }
        }

        private void ConnectionsManagerThread()
        {
            for (; ; )
            {
                try
                {
                    string sender;
                    string[] command;
                    ArraySegment<byte>[] content;

                    {
                        var item = _receiveMessageQueue.Dequeue();

                        if (item.Key == null) continue;
                        sender = item.Key;

                        if (item.Value == null) continue;
                        if (item.Value.Command == null) continue;
                        command = RouterManager.Split(item.Value.Command);
                        if (item.Value.Content.Array == null) continue;
                        content = RouterManager.FromBuffer(item.Value.Content, _bufferManager);
                    }

                    if (this.State == ManagerState.Stop) return;

                    if (command[0] == "user")
                    {
                        if (command[1] == "connection")
                        {

                        }
                        else if (command[1] == "memo")
                        {

                        }
                    }
                    else if (command[0] == "channel")
                    {
                        if (command[1] == "join" && command.Length == 4)
                        {
                            var channelName = command[2];
                            if (string.IsNullOrWhiteSpace(channelName)) continue;

                            var channelConfig = _settings.ChannelConfigs.FirstOrDefault(n => n.Name == channelName);

                            if (channelConfig == null)
                            {
                                channelConfig = new ChannelConfig() { Name = channelName };
                                _settings.ChannelConfigs.Add(channelConfig);
                            }

                            if (channelConfig.State == ChannelState.Private)
                            {
                                if (!channelConfig.Managers.Any(n => Collection.Equals(n, sender))
                                    || !channelConfig.Members.Any(n => Collection.Equals(n, sender))) continue;
                            }

                            _channelUsersDictionary[channelName].Add(sender);

                            foreach (string id in _channelUsersDictionary[channelName])
                            {
                                using (DeadlockMonitor.Lock(_sendMessageQueue.ThisLock))
                                {
                                    if (!_sendMessageQueue.ContainsKey(id))
                                        _sendMessageQueue[id] = new WaitQueue<IEnumerable<CommandMessage>>();
                                }

                                _sendMessageQueue[id].Enqueue(new CommandMessage[] { 
                                    new CommandMessage() { 
                                        Command = string.Format("channel join {0} {1}", channelName, sender),
                                    }});
                            }
                        }
                        else if (command[1] == "quit")
                        {

                        }
                        else if (command[1] == "kick")
                        {

                        }
                        else if (command[1] == "invite")
                        {

                        }
                        else if (command[1] == "message" && command.Length == 4)
                        {
                            var channelName = command[2];
                            if (string.IsNullOrWhiteSpace(channelName)) continue;

                            var channelConfig = _settings.ChannelConfigs.FirstOrDefault(n => n.Name == channelName);
                            if (channelConfig == null) continue;

                            if (channelConfig.State == ChannelState.Private)
                            {
                                if (!channelConfig.Managers.Any(n => Collection.Equals(n, sender))
                                    || !channelConfig.Members.Any(n => Collection.Equals(n, sender))) continue;
                            }

                            var channelMessage = command[3];
                            if (string.IsNullOrWhiteSpace(channelMessage)) continue;

                            foreach (string id in _channelUsersDictionary[channelName])
                            {
                                using (DeadlockMonitor.Lock(_sendMessageQueue.ThisLock))
                                {
                                    if (!_sendMessageQueue.ContainsKey(id))
                                        _sendMessageQueue[id] = new WaitQueue<IEnumerable<CommandMessage>>();
                                }

                                _sendMessageQueue[id].Enqueue(new CommandMessage[] { 
                                    new CommandMessage() { 
                                        Command = string.Format("channel message {0} {1}", channelMessage, sender),
                                    }});
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private void ConnectionManagerThread(object state)
        {
            Thread.CurrentThread.Name = "ConnectionManagerThread";

            var connectionManager = state as ConnectionManager;
            if (connectionManager == null) return;

            for (; ; )
            {
                if (this.State == ManagerState.Stop) return;
                if (!_connectionManagers.Contains(connectionManager)) return;

                using (DeadlockMonitor.Lock(_sendMessageQueue.ThisLock))
                {
                    if (!_sendMessageQueue.ContainsKey(_connectionId[connectionManager]))
                        _sendMessageQueue[_connectionId[connectionManager]] = new WaitQueue<IEnumerable<CommandMessage>>();
                }

                var messages = _sendMessageQueue[_connectionId[connectionManager]].Dequeue();

                connectionManager.PushMessages(messages);

                foreach (var item in messages)
                {
                    _bufferManager.ReturnBuffer(item.Content.Array);
                }
            }
        }

        #region connectionManager_Event

        void connectionManager_PullMessagesEvent(object sender, MessagesEventArgs e)
        {
            var connectionManager = sender as ConnectionManager;
            if (connectionManager == null) return;

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                using (DeadlockMonitor.Lock(_receiveMessageQueue.ThisLock))
                {
                    foreach (var item in e.Messages)
                    {
                        _receiveMessageQueue.Enqueue(new KeyValuePair<string, CommandMessage>(_connectionId[connectionManager], item));
                    }
                }
            }
        }

        void connectionManager_CloseEvent(object sender, EventArgs e)
        {
            var connectionManager = sender as ConnectionManager;
            if (connectionManager == null) return;

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                _connectionManagers.Remove(connectionManager);

                _sendMessageQueue[_connectionId[connectionManager]].Clear();
            }
        }

        #endregion

        public override ManagerState State
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _state;
                }
            }
        }

        public override void Start()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            while (_addConnectionManagerThread != null) Thread.Sleep(1000);
            while (_connectionsManagerThread != null) Thread.Sleep(1000);

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                Regex regex = new Regex(@"(.*?):(.*):(\d*)");

                foreach (var uri in this.ListenUris)
                {
                    var match = regex.Match(uri);
                    if (!match.Success)
                        continue;

                    if (match.Groups[1].Value == "tcp")
                    {
                        try
                        {
                            var listener = new TcpListener(IPAddress.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
                            listener.Start(3);
                            _listeners.Add(listener);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                _addConnectionManagerThread = new Thread(this.AddConnectionManagerThread);
                _addConnectionManagerThread.IsBackground = true;
                _addConnectionManagerThread.Name = "AddConnectionManagerThread";
                _addConnectionManagerThread.Start();

                _connectionsManagerThread = new Thread(this.ConnectionsManagerThread);
                _connectionsManagerThread.IsBackground = true;
                _connectionsManagerThread.Name = "ConnectionManagerThread";
                _connectionsManagerThread.Start();
            }
        }

        public override void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;
            }

            for (int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].Stop();
            }

            _listeners.Clear();

            _addConnectionManagerThread.Join();
            _addConnectionManagerThread = null;

            _connectionsManagerThread.Join();
            _connectionsManagerThread = null;

            using (DeadlockMonitor.Lock(this.ThisLock))
            {

            }
        }

        #region ISettings メンバ

        public void Load(string directoryPath)
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                _settings.Load(directoryPath);
            }
        }

        public void Save(string directoryPath)
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                _settings.Save(directoryPath);
            }
        }

        #endregion

        private class Settings : Library.Configuration.SettingsBase, IThisLock
        {
            private object _thisLock = new object();

            public Settings()
                : base(new List<Library.Configuration.ISettingsContext>() { 
                    new Library.Configuration.SettingsContext<DigitalSignature>() { Name = "DigtalSignature", Value = null },
                    new Library.Configuration.SettingsContext<UriCollection>() { Name = "ListenUris", Value = new UriCollection() },
                    new Library.Configuration.SettingsContext<List<ChannelConfig>>() { Name = "ChannelConfigs", Value = new List<ChannelConfig>() },
                    new Library.Configuration.SettingsContext<int>() { Name = "ConnectionCountLimit", Value = 50 },
                })
            {

            }

            public override void Load(string directoryPath)
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    base.Load(directoryPath);
                }
            }

            public override void Save(string directoryPath)
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    base.Save(directoryPath);
                }
            }

            public UriCollection ListenUris
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (UriCollection)this["ListenUris"];
                    }
                }
            }

            public DigitalSignature DigitalSignature
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (DigitalSignature)this["DigitalSignature"];
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        this["DigitalSignature"] = value;
                    }
                }
            }

            public List<ChannelConfig> ChannelConfigs
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (List<ChannelConfig>)this["ChannelConfigs"];
                    }
                }
            }

            public int ConnectionCountLimit
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (int)this["ConnectionCountLimit"];
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        this["ConnectionCountLimit"] = value;
                    }
                }
            }

            #region IThisLock メンバ

            public object ThisLock
            {
                get
                {
                    return _thisLock;
                }
            }

            #endregion
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_addConnectionManagerThread != null)
                    {
                        try
                        {
                            _addConnectionManagerThread.Abort();
                        }
                        catch (Exception)
                        {

                        }

                        _addConnectionManagerThread = null;
                    }

                    if (_connectionsManagerThread != null)
                    {
                        try
                        {
                            _connectionsManagerThread.Abort();
                        }
                        catch (Exception)
                        {

                        }

                        _connectionsManagerThread = null;
                    }
                }

                _disposed = true;
            }
        }

        #region IThisLock メンバ

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion

        [DataContract(Name = "ChannelState", Namespace = "http://Library/Net/Lair")]
        private enum ChannelState
        {
            [EnumMember(Value = "Public")]
            Public,

            [EnumMember(Value = "Private")]
            Private,
        }

        [DataContract(Name = "ChannelConfig", Namespace = "http://Library/Net/Lair")]
        private class ChannelConfig : ItemBase<ChannelConfig>, IThisLock
        {
            private enum SerializeId : byte
            {
                Name = 0,
                State = 1,
                Manager = 2,
                Member = 3,
            }

            private string _name;
            private ChannelState _state;
            private IdCollection _managers;
            private IdCollection _members;

            public const int MaxNameLength = 256;

            private object _thisLock;
            private static object _thisStaticLock = new object();

            public ChannelConfig()
            {

            }

            protected override void ProtectedImport(Stream stream, BufferManager bufferManager)
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    Encoding encoding = new UTF8Encoding(false);
                    byte[] lengthBuffer = new byte[4];

                    for (; ; )
                    {
                        if (stream.Read(lengthBuffer, 0, lengthBuffer.Length) != lengthBuffer.Length)
                            return;
                        int length = NetworkConverter.ToInt32(lengthBuffer);
                        byte id = (byte)stream.ReadByte();

                        using (RangeStream rangeStream = new RangeStream(stream, stream.Position, length, true))
                        {
                            if (id == (byte)SerializeId.Name)
                            {
                                using (StreamReader reader = new StreamReader(rangeStream, encoding))
                                {
                                    this.Name = reader.ReadToEnd();
                                }
                            }
                            else if (id == (byte)SerializeId.State)
                            {
                                using (StreamReader reader = new StreamReader(rangeStream, encoding))
                                {
                                    this.State = (ChannelState)Enum.Parse(typeof(ChannelState), reader.ReadToEnd());
                                }
                            }
                            else if (id == (byte)SerializeId.Manager)
                            {
                                using (StreamReader reader = new StreamReader(rangeStream, encoding))
                                {
                                    this.Managers.Add(reader.ReadToEnd());
                                }
                            }
                            else if (id == (byte)SerializeId.Member)
                            {
                                using (StreamReader reader = new StreamReader(rangeStream, encoding))
                                {
                                    this.Members.Add(reader.ReadToEnd());
                                }
                            }
                        }
                    }
                }
            }

            public override Stream Export(BufferManager bufferManager)
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    List<Stream> streams = new List<Stream>();
                    Encoding encoding = new UTF8Encoding(false);

                    // Name
                    if (this.Name != null)
                    {
                        BufferStream bufferStream = new BufferStream(bufferManager);
                        bufferStream.SetLength(5);
                        bufferStream.Seek(5, SeekOrigin.Begin);

                        using (CacheStream cacheStream = new CacheStream(bufferStream, 1024, true, bufferManager))
                        using (StreamWriter writer = new StreamWriter(cacheStream, encoding))
                        {
                            writer.Write(this.Name);
                        }

                        bufferStream.Seek(0, SeekOrigin.Begin);
                        bufferStream.Write(NetworkConverter.GetBytes((int)bufferStream.Length - 5), 0, 4);
                        bufferStream.WriteByte((byte)SerializeId.Name);

                        streams.Add(bufferStream);
                    }
                    // State
                    if (this.State != 0)
                    {
                        BufferStream bufferStream = new BufferStream(bufferManager);
                        bufferStream.SetLength(5);
                        bufferStream.Seek(5, SeekOrigin.Begin);

                        using (CacheStream cacheStream = new CacheStream(bufferStream, 1024, true, bufferManager))
                        using (StreamWriter writer = new StreamWriter(cacheStream, encoding))
                        {
                            writer.Write(this.State.ToString());
                        }

                        bufferStream.Seek(0, SeekOrigin.Begin);
                        bufferStream.Write(NetworkConverter.GetBytes((int)bufferStream.Length - 5), 0, 4);
                        bufferStream.WriteByte((byte)SerializeId.State);

                        streams.Add(bufferStream);
                    }
                    // Managers
                    foreach (var m in this.Managers)
                    {
                        BufferStream bufferStream = new BufferStream(bufferManager);
                        bufferStream.SetLength(5);
                        bufferStream.Seek(5, SeekOrigin.Begin);

                        using (CacheStream cacheStream = new CacheStream(bufferStream, 1024, true, bufferManager))
                        using (StreamWriter writer = new StreamWriter(cacheStream, encoding))
                        {
                            writer.Write(m);
                        }

                        bufferStream.Seek(0, SeekOrigin.Begin);
                        bufferStream.Write(NetworkConverter.GetBytes((int)bufferStream.Length - 5), 0, 4);
                        bufferStream.WriteByte((byte)SerializeId.Name);

                        streams.Add(bufferStream);
                    }
                    // Members
                    foreach (var m in this.Members)
                    {
                        BufferStream bufferStream = new BufferStream(bufferManager);
                        bufferStream.SetLength(5);
                        bufferStream.Seek(5, SeekOrigin.Begin);

                        using (CacheStream cacheStream = new CacheStream(bufferStream, 1024, true, bufferManager))
                        using (StreamWriter writer = new StreamWriter(cacheStream, encoding))
                        {
                            writer.Write(m);
                        }

                        bufferStream.Seek(0, SeekOrigin.Begin);
                        bufferStream.Write(NetworkConverter.GetBytes((int)bufferStream.Length - 5), 0, 4);
                        bufferStream.WriteByte((byte)SerializeId.Name);

                        streams.Add(bufferStream);
                    }

                    return new AddStream(streams);
                }
            }

            public override int GetHashCode()
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return this.Managers.Count + this.Members.Count;
                }
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is ChannelConfig)) return false;

                return this.Equals((ChannelConfig)obj);
            }

            public override bool Equals(ChannelConfig other)
            {
                if ((object)other == null) return false;
                if (object.ReferenceEquals(this, other)) return true;
                if (this.GetHashCode() != other.GetHashCode()) return false;

                if (this.Name != other.Name
                    || this.State != other.State
                    || (this.Managers == null) != (other.Managers == null)
                    || (this.Members == null) != (other.Members == null))
                {
                    return false;
                }

                if (this.Managers != null && other.Managers != null)
                {
                    if (!Collection.Equals(this.Managers, other.Managers)) return false;
                }

                if (this.Members != null && other.Members != null)
                {
                    if (!Collection.Equals(this.Members, other.Members)) return false;
                }

                return true;
            }

            public override ChannelConfig DeepClone()
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    using (var bufferManager = new BufferManager())
                    using (var stream = this.Export(bufferManager))
                    {
                        return ChannelConfig.Import(stream, bufferManager);
                    }
                }
            }

            public override string ToString()
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return this.Name;
                }
            }

            [DataMember(Name = "Name")]
            public string Name
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return _name;
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        if (value != null && value.Length > ChannelConfig.MaxNameLength)
                        {
                            throw new ArgumentException();
                        }
                        else
                        {
                            _name = value;
                        }
                    }
                }
            }

            [DataMember(Name = "State")]
            public ChannelState State
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return _state;
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        if (!Enum.IsDefined(typeof(ChannelState), value))
                        {
                            throw new ArgumentException();
                        }
                        else
                        {
                            _state = value;
                        }
                    }
                }
            }

            [DataMember(Name = "Managers")]
            public IdCollection Managers
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        if (_managers == null)
                            _managers = new IdCollection();

                        return _managers;
                    }
                }
            }

            [DataMember(Name = "Members")]
            public IdCollection Members
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        if (_members == null)
                            _members = new IdCollection();

                        return _members;
                    }
                }
            }

            #region IThisLock メンバ

            public object ThisLock
            {
                get
                {
                    using (DeadlockMonitor.Lock(_thisStaticLock))
                    {
                        if (_thisLock == null)
                            _thisLock = new object();

                        return _thisLock;
                    }
                }
            }

            #endregion
        }
    }
}
