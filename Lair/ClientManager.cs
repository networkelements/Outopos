using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Connection;
using Library.Net.Proxy;
using Library.Security;
using Library.Net.Lair;

namespace Lair
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ClientManager : ManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private BufferManager _bufferManager;
        private ConnectionManager _connectionManager;

        private Settings _settings;

        private bool _disposed = false;
        private object _thisLock = new object();

        private const int MaxReceiveCount = 1 * 1024 * 1024;

        public ClientManager(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            _settings = new Settings();
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

        public string Uri
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.Uri;
                }
            }
            set
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    _settings.Uri = value;
                }
            }
        }

        public ConnectionType ConnectionType
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.ConnectionType;
                }
            }
            set
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    _settings.ConnectionType = value;
                }
            }
        }

        public string ProxyUri
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.ProxyUri;
                }
            }
            set
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    _settings.ProxyUri = value;
                }
            }
        }

        public ChannelCollection Channels
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _settings.Channels;
                }
            }
            set
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    _settings.Channels = value;
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
                    if (memoryStream.Read(lengthBuffer, 0, 4) < 4)
                        break;
                    int length = NetworkConverter.ToInt32(lengthBuffer);

                    byte[] buffer2 = bufferManager.TakeBuffer(length);
                    if (memoryStream.Read(buffer2, 0, length) < length)
                        break;

                    list.Add(new ArraySegment<byte>(buffer2, 0, length));
                }
            }

            return list.ToArray();
        }

        private static IPAddress GetIpAddress(string host)
        {
            IPAddress remoteIP = null;

            if (!IPAddress.TryParse(host, out remoteIP))
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(host);

                if (hostEntry.AddressList.Length > 0)
                {
                    remoteIP = hostEntry.AddressList[0];
                }
                else
                {
                    return null;
                }
            }

            return remoteIP;
        }

        private static Socket Connect(IPEndPoint remoteEndPoint, TimeSpan timeout)
        {
            Socket socket = null;

            try
            {
                socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveTimeout = 1000 * 10;
                socket.SendTimeout = 1000 * 10;

                var asyncResult = socket.BeginConnect(remoteEndPoint, null, null);

                if (!asyncResult.IsCompleted && !asyncResult.CompletedSynchronously)
                {
                    if (!asyncResult.AsyncWaitHandle.WaitOne(timeout, false))
                    {
                        throw new ConnectionException();
                    }
                }

                socket.EndConnect(asyncResult);

                return socket;
            }
            catch (Exception)
            {
                if (socket != null)
                    socket.Close();
            }

            throw new SessionManagerException();
        }

        public Certificate Connect(TimeSpan timeout)
        {
            Socket socket = null;
            ConnectionBase connection = null;

            try
            {

                string scheme = null;
                string host = null;
                int port = -1;

                {
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(this.Uri);

                    if (match.Success)
                    {
                        scheme = match.Groups[1].Value;
                        host = match.Groups[2].Value;
                        port = int.Parse(match.Groups[3].Value);
                    }
                    else
                    {
                        Regex regex2 = new Regex(@"(.*?):(.*)");
                        var match2 = regex2.Match(this.Uri);

                        if (match2.Success)
                        {
                            scheme = match2.Groups[1].Value;
                            host = match2.Groups[2].Value;
                            port = 4050;
                        }
                    }
                }

                if (host == null) return null;

                if (this.ConnectionType == ConnectionType.Tcp)
                {
#if !DEBUG
                    {
                        Uri url = new Uri(string.Format("{0}://{1}:{2}", scheme, host, port));

                        if (url.HostNameType == UriHostNameType.IPv4)
                        {
                            var myIpAddress = IPAddress.Parse(url.Host);

                            if (IPAddress.Any.ToString() == myIpAddress.ToString()
                                || IPAddress.Loopback.ToString() == myIpAddress.ToString()
                                || IPAddress.Broadcast.ToString() == myIpAddress.ToString())
                            {
                                return null;
                            }
                            if (Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("10.0.0.0").GetAddressBytes()) >= 0
                                && Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("10.255.255.255").GetAddressBytes()) <= 0)
                            {
                                return null;
                            }
                            if (Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("172.16.0.0").GetAddressBytes()) >= 0
                                && Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("172.31.255.255").GetAddressBytes()) <= 0)
                            {
                                return null;
                            }
                            if (Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("127.0.0.0").GetAddressBytes()) >= 0
                                && Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("127.255.255.255").GetAddressBytes()) <= 0)
                            {
                                return null;
                            }
                            if (Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("192.168.0.0").GetAddressBytes()) >= 0
                                && Collection.Compare(myIpAddress.GetAddressBytes(), IPAddress.Parse("192.168.255.255").GetAddressBytes()) <= 0)
                            {
                                return null;
                            }
                        }
                        else if (url.HostNameType == UriHostNameType.IPv6)
                        {
                            var myIpAddress = IPAddress.Parse(url.Host);

                            if (IPAddress.IPv6Any.ToString() == myIpAddress.ToString()
                                || IPAddress.IPv6Loopback.ToString() == myIpAddress.ToString()
                                || IPAddress.IPv6None.ToString() == myIpAddress.ToString())
                            {
                                return null;
                            }
                            if (myIpAddress.ToString().ToLower().StartsWith("fe80:"))
                            {
                                return null;
                            }
                        }
                    }
#endif
                    socket = ClientManager.Connect(new IPEndPoint(ClientManager.GetIpAddress(host), port), new TimeSpan(0, 0, 10));
                    connection = new TcpConnection(socket, ClientManager.MaxReceiveCount, _bufferManager);
                }
                else
                {
                    string proxyScheme = null;
                    string proxyHost = null;
                    int proxyPort = -1;

                    {
                        Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(this.ProxyUri);

                        if (match.Success)
                        {
                            proxyScheme = match.Groups[1].Value;
                            proxyHost = match.Groups[2].Value;
                            proxyPort = int.Parse(match.Groups[3].Value);
                        }
                        else
                        {
                            Regex regex2 = new Regex(@"(.*?):(.*)");
                            var match2 = regex2.Match(this.ProxyUri);

                            if (match2.Success)
                            {
                                proxyScheme = match2.Groups[1].Value;
                                proxyHost = match2.Groups[2].Value;

                                if (this.ConnectionType == ConnectionType.Socks4Proxy
                                    || this.ConnectionType == ConnectionType.Socks4aProxy
                                    || this.ConnectionType == ConnectionType.Socks5Proxy)
                                {
                                    proxyPort = 1080;
                                }
                                else if (this.ConnectionType == ConnectionType.HttpProxy)
                                {
                                    proxyPort = 80;
                                }
                            }
                        }
                    }

                    if (proxyHost == null) return null;

                    socket = ClientManager.Connect(new IPEndPoint(ClientManager.GetIpAddress(proxyHost), proxyPort), new TimeSpan(0, 0, 10));
                    ProxyClientBase proxy = null;

                    if (this.ConnectionType == ConnectionType.Socks4Proxy)
                    {
                        proxy = new Socks4ProxyClient(socket, host, port);
                    }
                    else if (this.ConnectionType == ConnectionType.Socks4aProxy)
                    {
                        proxy = new Socks4aProxyClient(socket, host, port);
                    }
                    else if (this.ConnectionType == ConnectionType.Socks5Proxy)
                    {
                        proxy = new Socks5ProxyClient(socket, host, port);
                    }
                    else if (this.ConnectionType == ConnectionType.HttpProxy)
                    {
                        proxy = new HttpProxyClient(socket, host, port);
                    }

                    connection = new TcpConnection(proxy.CreateConnection(new TimeSpan(0, 0, 30)), ClientManager.MaxReceiveCount, _bufferManager);
                }

                var secureConnection = new SecureClientConnection(connection, null, _bufferManager);
                secureConnection.Connect(new TimeSpan(0, 1, 0));

                _connectionManager = new ConnectionManager(new CompressConnection(secureConnection, ClientManager.MaxReceiveCount, _bufferManager), _bufferManager);
            }
            catch (Exception)
            {
                if (socket != null) socket.Close();
                if (connection != null) connection.Dispose();
            }

            return null;
        }

        public void Close(TimeSpan timeout)
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.ThisLock)
            {
                if (_connectionManager != null)
                {
                    try
                    {
                        _connectionManager.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _connectionManager = null;
                }
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
                    new Library.Configuration.SettingsContext<string>() { Name = "Uri", Value = "" },
                    new Library.Configuration.SettingsContext<ConnectionType>() { Name = "ConnectionType", Value = ConnectionType.Tcp },
                    new Library.Configuration.SettingsContext<string>() { Name = "ProxyUri", Value = "" },
                    new Library.Configuration.SettingsContext<ChannelCollection>() { Name = "Channels", Value = new ChannelCollection() },
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

            public string Uri
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (string)this["Uri"];
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        this["Uri"] = value;
                    }
                }
            }

            public ConnectionType ConnectionType
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (ConnectionType)this["ConnectionType"];
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        this["ConnectionType"] = value;
                    }
                }
            }

            public string ProxyUri
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (string)this["ProxyUri"];
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        this["ProxyUri"] = value;
                    }
                }
            }

            public ChannelCollection Channels
            {
                get
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        return (ChannelCollection)this["Channels"];
                    }
                }
                set
                {
                    using (DeadlockMonitor.Lock(this.ThisLock))
                    {
                        this["Channels"] = value;
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
            lock (this.ThisLock)
            {
                if (_disposed) return;

                if (disposing)
                {

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
    }

    [Serializable]
    class SessionManagerException : ManagerException
    {
        public SessionManagerException() : base() { }
        public SessionManagerException(string message) : base(message) { }
        public SessionManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
