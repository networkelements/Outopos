﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Lair.Windows;
using Library;
using Library.Collections;
using Library.Net.Lair;

namespace Lair
{
    class TransfarLimitManager : ManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private LairManager _lairManager;

        private Settings _settings;

        private bool _isRun = true;

        private Thread _timerThread = null;

        public event EventHandler StartEvent;
        public event EventHandler StopEvent;

        private object _thisLock = new object();
        private volatile bool _disposed = false;

        public TransfarLimitManager(LairManager lairManager)
        {
            _lairManager = lairManager;

            _settings = new Settings();

            _timerThread = new Thread(new ThreadStart(this.Timer));
            _timerThread.Priority = ThreadPriority.Highest;
            _timerThread.IsBackground = true;
            _timerThread.Name = "TransfarLimitManager_TimerThread";
            _timerThread.Start();
        }

        public TransferLimit TransferLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.TransferLimit;
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    _settings.TransferLimit = value;
                }
            }
        }

        public long TotalUploadSize
        {
            get
            {
                return _settings.UploadTransferSizeList.Sum(n => n.Value);
            }
        }

        public long TotalDownloadSize
        {
            get
            {
                return _settings.DownloadTransferSizeList.Sum(n => n.Value);
            }
        }

        protected virtual void OnStartEvent()
        {
            if (this.StartEvent != null)
            {
                this.StartEvent(this, new EventArgs());
            }
        }

        protected virtual void OnStopEvent()
        {
            if (this.StopEvent != null)
            {
                this.StopEvent(this, new EventArgs());
            }
        }

        public void Reset()
        {
            _settings.UploadTransferSizeList.Clear();
            _settings.DownloadTransferSizeList.Clear();
        }

        private void Timer()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();

                var now = DateTime.Today;
                long uploadSize;
                long downloadSize;

                _settings.UploadTransferSizeList.TryGetValue(now, out uploadSize);
                _settings.UploadTransferSizeList.TryGetValue(now, out downloadSize);

                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (!_isRun) return;

                    if (!stopwatch.IsRunning || stopwatch.Elapsed > new TimeSpan(0, 1, 0))
                    {
                        stopwatch.Restart();

                        if (now != DateTime.Today)
                        {
                            now = DateTime.Today;

                            uploadSize = -_lairManager.SentByteCount;
                            downloadSize = -_lairManager.ReceivedByteCount;

                            if (_lairManager.State == ManagerState.Stop) this.OnStartEvent();
                        }
                        else
                        {
                            _settings.UploadTransferSizeList[now] = uploadSize + _lairManager.SentByteCount;
                            _settings.DownloadTransferSizeList[now] = downloadSize + _lairManager.ReceivedByteCount;
                        }

                        if (_settings.TransferLimit.Type != TransferLimitType.None)
                        {
                            foreach (var item in _settings.UploadTransferSizeList.ToArray())
                            {
                                if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                                    _settings.UploadTransferSizeList.Remove(item.Key);
                            }

                            foreach (var item in _settings.DownloadTransferSizeList.ToArray())
                            {
                                if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                                    _settings.DownloadTransferSizeList.Remove(item.Key);
                            }

                            if (_settings.TransferLimit.Type == TransferLimitType.Uploads)
                            {
                                var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);

                                if (totalUploadSize > _settings.TransferLimit.Size)
                                {
                                    if (_lairManager.State == ManagerState.Start) this.OnStopEvent();
                                }
                            }

                            if (_settings.TransferLimit.Type == TransferLimitType.Downloads)
                            {
                                var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                                if (totalDownloadSize > _settings.TransferLimit.Size)
                                {
                                    if (_lairManager.State == ManagerState.Start) this.OnStopEvent();
                                }
                            }

                            if (_settings.TransferLimit.Type == TransferLimitType.Total)
                            {
                                var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);
                                var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                                if ((totalUploadSize + totalDownloadSize) > _settings.TransferLimit.Size)
                                {
                                    if (_lairManager.State == ManagerState.Start) this.OnStopEvent();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
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
                new Library.Configuration.SettingsContext<TransferLimit>() { Name = "TransferLimit", Value = new TransferLimit() },
                new Library.Configuration.SettingsContext<LockedDictionary<DateTime, long>>() { Name = "UploadTransferSizeList", Value = new LockedDictionary<DateTime, long>() },
                new Library.Configuration.SettingsContext<LockedDictionary<DateTime, long>>() { Name = "DownloadTransferSizeList", Value = new LockedDictionary<DateTime, long>() },
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

            public TransferLimit TransferLimit
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (TransferLimit)this["TransferLimit"];
                    }
                }

                set
                {
                    lock (this.ThisLock)
                    {
                        this["TransferLimit"] = value;
                    }
                }
            }

            public LockedDictionary<DateTime, long> UploadTransferSizeList
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (LockedDictionary<DateTime, long>)this["UploadTransferSizeList"];
                    }
                }

                set
                {
                    lock (this.ThisLock)
                    {
                        this["UploadTransferSizeList"] = value;
                    }
                }
            }

            public LockedDictionary<DateTime, long> DownloadTransferSizeList
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (LockedDictionary<DateTime, long>)this["DownloadTransferSizeList"];
                    }
                }

                set
                {
                    lock (this.ThisLock)
                    {
                        this["DownloadTransferSizeList"] = value;
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
                _isRun = false;

                _timerThread.Join();
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

    [DataContract(Name = "TransferLimitType", Namespace = "http://Lair")]
    enum TransferLimitType
    {
        [EnumMember(Value = "None")]
        None,

        [EnumMember(Value = "Uploads")]
        Uploads,

        [EnumMember(Value = "Downloads")]
        Downloads,

        [EnumMember(Value = "Total")]
        Total,
    }

    [DataContract(Name = "TransferLimit", Namespace = "http://Lair")]
    class TransferLimit : IThisLock
    {
        private TransferLimitType _type = TransferLimitType.None;
        private int _span = 1;
        private long _size = 1024 * 1024;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Type")]
        public TransferLimitType Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _type;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _type = value;
                }
            }
        }

        [DataMember(Name = "Span")]
        public int Span
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _span;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _span = value;
                }
            }
        }

        [DataMember(Name = "Size")]
        public long Size
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _size;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _size = value;
                }
            }
        }

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
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
