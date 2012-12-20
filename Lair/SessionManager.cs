using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Library;
using Library.Net.Lair;
using Library.Net.Proxy.I2p.SamV3;
using Library.Net.Connection;

namespace Lair
{
    class SessionManager : StateManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private ManagerState _state = ManagerState.Stop;

        private SamV3Session _samMasterSession;
        private SamListener _samListener;
        private object _samMasterLock = new object();

        private object _thisLock = new object();
        private volatile bool _disposed = false;

        private const int MaxReceiveCount = 1024 * 1024 * 16;
        
        public SessionManager(LairManager lairManager, BufferManager bufferManager)
        {
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            _settings = new Settings();

            _lairManager.ConnectEvent = (object sender, string uri) =>
            {
                Socket socket = null;
                ConnectionBase connection = null;

                string scheme = null;
                string host = null;
                int port = -1;

                {
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);

                    if (match.Success)
                    {
                        scheme = match.Groups[1].Value;
                        host = match.Groups[2].Value;
                        port = int.Parse(match.Groups[3].Value);
                    }
                    else
                    {
                        Regex regex2 = new Regex(@"(.*?):(.*)");
                        var match2 = regex2.Match(uri);

                        if (match2.Success)
                        {
                            scheme = match2.Groups[1].Value;
                            host = match2.Groups[2].Value;
                            port = 4050;
                        }
                    }
                }

                if (host == null) return null;

                try
                {
                    string proxyScheme = null;
                    string proxyHost = null;
                    int proxyPort = -1;

                    {
                        Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(this.SamBridgeUri);

                        if (match.Success)
                        {
                            proxyScheme = match.Groups[1].Value;
                            proxyHost = match.Groups[2].Value;
                            proxyPort = int.Parse(match.Groups[3].Value);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }

                    if (proxyHost == null) return null;

                    SamV3Connector connector = null;

                    try
                    {
                        SamV3Session session;
                        lock (_samMasterLock)
                        {
                            if (_disposed)
                                throw new ObjectDisposedException("");

                            session = _samMasterSession;

                            if (session == null)
                            {
                                try
                                {
                                    socket = SessionManager.Connect(new IPEndPoint(SessionManager.GetIpAddress(proxyHost), proxyPort), new TimeSpan(0, 0, 10));

                                    string caption = "Lair";
                                    string[] options = new string[]
                                        {
                                            "inbound.nickname=" + caption,
                                            "outbound.nickname=" + caption
                                        };
                                    string optionsString = string.Join(" ", options);

                                    session = new SamV3Session(proxyHost, proxyPort, null, socket);
                                    socket = null;
                                    session.Handshake();
                                    session.Create(null, optionsString);

                                    _samMasterSession = session;
                                }
                                catch (Exception)
                                {
                                    if (session != null)
                                        session.Dispose();
                                    if (socket != null)
                                        socket.Close();
                                    throw;
                                }
                            }
                        }
                        connector = new SamV3Connector(session);
                        connector.Handshake();
                        connector.Connect(host);
                    }
                    catch (SamBridgeErrorException ex)
                    {
                        bool isLog = true;
                        if (connector != null)
                            connector.Dispose();
                        string result = ex.Reply.UniqueValue("RESULT", string.Empty).Value;
                        if (result == SamBridgeErrorMessage.INVALID_ID)
                        {
                            lock (_samMasterLock)
                            {
                                if (_samMasterSession != null)
                                {
                                    _samMasterSession.Dispose();
                                    _samMasterSession = null;
                                }
                            }
                            isLog = false;
                        }
                        else if (result == SamBridgeErrorMessage.CANT_REACH_PEER
                                || result == SamBridgeErrorMessage.PEER_NOT_FOUND)
                        {
                            isLog = false;
                        }
                        if (isLog)
                            Log.Error(ex);
                        throw;
                    }
                    catch (SamException ex)
                    {
                        Log.Error(ex);
                        if (connector != null)
                            connector.Dispose();
                        throw;
                    }

                    connection = new TcpConnection(connector.BridgeSocket, SessionManager.MaxReceiveCount, _bufferManager);
                }
                catch (Exception)
                {
                    if (socket != null) socket.Close();
                    if (connection != null) connection.Dispose();
                }

                return null;
            };

            _lairManager.AcceptEvent = (object sender, out string uri) =>
            {
                uri = null;

                try
                {
                    _samListener.Update();
                }
                catch (SamException)
                {
                    return null;
                }

                if (_samListener.Pending())
                {
                    SamV3StatefulAcceptor acceptor = _samListener.Dequeue();

                    try
                    {
                        acceptor.AcceptComplete();
                    }
                    catch (SamException ex)
                    {
                        Log.Error(ex);
                        acceptor.Dispose();

                        return null;
                    }

                    Socket socket = acceptor.BridgeSocket;
                    string base64Address = acceptor.DestinationBase64;
                    string base32Address = I2PEncoding.Base32Address.FromDestinationBase64(base64Address);
                    uri = "i2p:" + base32Address;

                    return new TcpConnection(socket, SessionManager.MaxReceiveCount, _bufferManager);
                }

                return null;
            };
        }

        public string SamBridgeUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.SamBridgeUri;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _settings.SamBridgeUri = value;
                }
            }
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
                socket.ReceiveTimeout = (int)timeout.TotalMilliseconds;
                socket.SendTimeout = (int)timeout.TotalMilliseconds;

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
                if (socket != null) socket.Close();
            }

            throw new Exception();
        }

        public override ManagerState State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _state;
                }
            }
        }

        public override void Start()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                Regex regex = new Regex(@"(.*?):(.*):(\d*)");

                try
                {
                    var match = regex.Match(this.SamBridgeUri);
                    if (!match.Success) throw new Exception();

                    string caption = "Lair";
                    string[] options = new string[]
                    {
                        "inbound.nickname=" + caption,
                        "outbound.nickname=" + caption
                    };
                    string optionsString = string.Join(" ", options);

                    _samListener = new SamListener(match.Groups[2].Value, int.Parse(match.Groups[3].Value), optionsString);

                    string base64Address = _samListener.Session.DestinationBase64;
                    string base32Address = I2PEncoding.Base32Address.FromDestinationBase64(base64Address);
                    Log.Information("New I2P BaseNode generated." + "\n" +
                            "i2p:" + base64Address + "\n" +
                            "i2p:" + base32Address);
                    //_newBaseNodeEvent("i2p:" + base32Address);
                }
                catch (SamException ex)
                {
                    Log.Error(ex);

                    if (_samListener != null) _samListener.Dispose();
                    _samListener = null;
                }
                catch (Exception)
                {

                }
            }
        }

        public override void Stop()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;

                lock (_samMasterLock)
                {
                    if (_samMasterSession != null)
                    {
                        _samMasterSession.Dispose();
                        _samMasterSession = null;
                    }
                }

                _samListener.Dispose();
            }
        }

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                _settings.Load(directoryPath);
            }
        }

        public void Save(string directoryPath)
        {
            lock (this.ThisLock)
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
                    new Library.Configuration.SettingsContext<string>() { Name = "SamBridgeUri", Value = "tcp:127.0.0.1:7656" },
                })
            {

            }

            public override void Load(string directoryPath)
            {
                lock (this.ThisLock)
                {
                    base.Load(directoryPath);
                }
            }

            public override void Save(string directoryPath)
            {
                lock (this.ThisLock)
                {
                    base.Save(directoryPath);
                }
            }

            public string SamBridgeUri
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (string)this["SamBridgeUri"];
                    }
                }
                set
                {
                    lock (this.ThisLock)
                    {
                        this["SamBridgeUri"] = value;
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    this.Stop();
                }
                catch (Exception)
                {

                }
            }

            _disposed = true;
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
