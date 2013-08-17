﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml;
using Library;

namespace Lair.Properties
{
    public delegate void UsingLanguageChangedEventHandler(object sender);

    class LanguagesManager : IThisLock
    {
        private static LanguagesManager _defaultInstance = new LanguagesManager();
        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private static string _usingLanguage = null;
        private static ObjectDataProvider provider;
        private object _thisLock = new object();

        public static UsingLanguageChangedEventHandler UsingLanguageChangedEvent;

        protected static void OnUsingLanguageChangedEvent()
        {
            if (LanguagesManager.UsingLanguageChangedEvent != null)
            {
                LanguagesManager.UsingLanguageChangedEvent(_defaultInstance);
            }
        }

        static LanguagesManager()
        {
#if DEBUG
            string path = @"C:\Core\Project\Lair\Lair\bin\Debug\Core\Languages";

            if (!Directory.Exists(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
#else
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
#endif

            LanguagesManager.Load(path);
        }

        private static void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();

                using (XmlTextReader xml = new XmlTextReader(path))
                {
                    try
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Translate")
                                {
                                    dic.Add(xml.GetAttribute("Key"), xml.GetAttribute("Value"));
                                }
                            }
                        }
                    }
                    catch (XmlException)
                    {

                    }
                }

                _dic[Path.GetFileNameWithoutExtension(path)] = dic;
            }

            if (CultureInfo.CurrentUICulture.Name == "ja-JP" && _dic.Keys.Any(n => n == "Japanese"))
            {
                _usingLanguage = "Japanese";
            }
            else if (_dic.Keys.Any(n => n == "English"))
            {
                _usingLanguage = "English";
            }
        }

        public static LanguagesManager GetInstance()
        {
            return _defaultInstance;
        }

        public static LanguagesManager Instance
        {
            get
            {
                return _defaultInstance;
            }
        }

        /// <summary>
        /// 言語の切り替えメソッド
        /// </summary>
        /// <param name="language">使用言語を指定する</param>
        public static void ChangeLanguage(string language)
        {
            if (!_dic.ContainsKey(language)) throw new ArgumentException();

            _usingLanguage = language;
            LanguagesManager.ResourceProvider.Refresh();

            LanguagesManager.OnUsingLanguageChangedEvent();
        }

        /// <summary>
        /// 使用できる言語リスト
        /// </summary>
        public IEnumerable<string> Languages
        {
            get
            {
                var list = _dic.Keys.ToList();

                list.Sort((x, y) =>
                {
                    return System.IO.Path.GetFileNameWithoutExtension(x).CompareTo(System.IO.Path.GetFileNameWithoutExtension(y));
                });

                return list.ToArray();
            }
        }

        public static ObjectDataProvider ResourceProvider
        {
            get
            {
                if (System.Windows.Application.Current != null)
                {
                    provider = (ObjectDataProvider)System.Windows.Application.Current.FindResource("ResourcesInstance");
                }

                return provider;
            }
        }

        public string Translate(string value)
        {
            if (_usingLanguage != null && _dic[_usingLanguage].ContainsKey(value))
            {
                return _dic[_usingLanguage][value];
            }
            else
            {
                return null;
            }
        }

        #region Property

        public string FontFamily
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("FontFamily");
                }
            }
        }

        public string FontSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("FontSize");
                }
            }
        }

        public string DateTime_StringFormat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DateTime_StringFormat");
                }
            }
        }


        public string Languages_English
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_English");
                }
            }
        }

        public string Languages_Japanese
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Japanese");
                }
            }
        }


        public string Seed_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Name");
                }
            }
        }

        public string Seed_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Signature");
                }
            }
        }

        public string Seed_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Length");
                }
            }
        }

        public string Seed_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Keywords");
                }
            }
        }

        public string Seed_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_CreationTime");
                }
            }
        }

        public string Seed_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Comment");
                }
            }
        }


        public string Channel_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Channel_Name");
                }
            }
        }

        public string Channel_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Channel_Id");
                }
            }
        }


        public string Section_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Section_Name");
                }
            }
        }

        public string Section_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Section_Id");
                }
            }
        }


        public string SectionTreeItem_LeaderSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItem_LeaderSignature");
                }
            }
        }


        public string Message_Channel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Message_Channel");
                }
            }
        }

        public string Message_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Message_Signature");
                }
            }
        }

        public string Message_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Message_CreationTime");
                }
            }
        }

        public string Message_Content
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Message_Content");
                }
            }
        }


        public string MainWindow_Connection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Connection");
                }
            }
        }

        public string MainWindow_Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Section");
                }
            }
        }

        public string MainWindow_Log
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Log");
                }
            }
        }

        public string MainWindow_Connections
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Connections");
                }
            }
        }

        public string MainWindow_Start
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Start");
                }
            }
        }

        public string MainWindow_Stop
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Stop");
                }
            }
        }

        public string MainWindow_UpdateBaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_UpdateBaseNode");
                }
            }
        }

        public string MainWindow_ConnectionsSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ConnectionsSettings");
                }
            }
        }

        public string MainWindow_View
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_View");
                }
            }
        }

        public string MainWindow_ClearUrlHistory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ClearUrlHistory");
                }
            }
        }

        public string MainWindow_ViewSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ViewSettings");
                }
            }
        }

        public string MainWindow_Languages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Languages");
                }
            }
        }

        public string MainWindow_Help
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Help");
                }
            }
        }

        public string MainWindow_ManualSite
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ManualSite");
                }
            }
        }

        public string MainWindow_DeveloperSite
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_DeveloperSite");
                }
            }
        }

        public string MainWindow_CheckUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckUpdate");
                }
            }
        }

        public string MainWindow_VersionInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_VersionInformation");
                }
            }
        }

        public string MainWindow_SendSpeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_SendSpeed");
                }
            }
        }

        public string MainWindow_ReceiveSpeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ReceiveSpeed");
                }
            }
        }

        public string MainWindow_DiskSpaceNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_DiskSpaceNotFound_Message");
                }
            }
        }

        public string MainWindow_CacheSpaceNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CacheSpaceNotFound_Message");
                }
            }
        }

        public string MainWindow_CheckUpdate_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckUpdate_Message");
                }
            }
        }

        public string MainWindow_LatestVersion_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_LatestVersion_Message");
                }
            }
        }

        public string MainWindow_Restart_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Restart_Message");
                }
            }
        }

        public string MainWindow_Close_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Close_Message");
                }
            }
        }

        public string MainWindow_Delete_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Delete_Message");
                }
            }
        }

        public string MainWindow_TransferLimit_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_TransferLimit_Message");
                }
            }
        }

        public string MainWindow_Upload_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Upload_Message");
                }
            }
        }


        public string ConnectionsSettingsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Title");
                }
            }
        }

        public string ConnectionsSettingsWindow_BaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_BaseNode");
                }
            }
        }

        public string ConnectionsSettingsWindow_Node
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Node");
                }
            }
        }

        public string ConnectionsSettingsWindow_Uris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Uris");
                }
            }
        }

        public string ConnectionsSettingsWindow_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Uri");
                }
            }
        }

        public string ConnectionsSettingsWindow_OtherNodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_OtherNodes");
                }
            }
        }

        public string ConnectionsSettingsWindow_Nodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Nodes");
                }
            }
        }

        public string ConnectionsSettingsWindow_Client
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Client");
                }
            }
        }

        public string ConnectionsSettingsWindow_Filters
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Filters");
                }
            }
        }

        public string ConnectionsSettingsWindow_ConnectionType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_ConnectionType");
                }
            }
        }

        public string ConnectionsSettingsWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_ProxyUri");
                }
            }
        }

        public string ConnectionsSettingsWindow_UriCondition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_UriCondition");
                }
            }
        }

        public string ConnectionsSettingsWindow_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Option");
                }
            }
        }

        public string ConnectionsSettingsWindow_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Type");
                }
            }
        }

        public string ConnectionsSettingsWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Host");
                }
            }
        }

        public string ConnectionsSettingsWindow_Server
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Server");
                }
            }
        }

        public string ConnectionsSettingsWindow_ListenUris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_ListenUris");
                }
            }
        }

        public string ConnectionsSettingsWindow_Events
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Events");
                }
            }
        }

        public string ConnectionsSettingsWindow_Bandwidth
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Bandwidth");
                }
            }
        }

        public string ConnectionsSettingsWindow_BandwidthLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_BandwidthLimit");
                }
            }
        }

        public string ConnectionsSettingsWindow_Transfer
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Transfer");
                }
            }
        }

        public string ConnectionsSettingsWindow_TransferLimitType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_TransferLimitType");
                }
            }
        }

        public string ConnectionsSettingsWindow_TransferLimitSpan
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_TransferLimitSpan");
                }
            }
        }

        public string ConnectionsSettingsWindow_TransferLimitSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_TransferLimitSize");
                }
            }
        }

        public string ConnectionsSettingsWindow_TransferInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_TransferInformation");
                }
            }
        }

        public string ConnectionsSettingsWindow_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Downloaded");
                }
            }
        }

        public string ConnectionsSettingsWindow_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Uploaded");
                }
            }
        }

        public string ConnectionsSettingsWindow_Total
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Total");
                }
            }
        }

        public string ConnectionsSettingsWindow_Reset
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Reset");
                }
            }
        }

        public string ConnectionsSettingsWindow_ConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_ConnectionCount");
                }
            }
        }

        public string ConnectionsSettingsWindow_AutoBaseNodeSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_AutoBaseNodeSetting");
                }
            }
        }

        public string ConnectionsSettingsWindow_Tor
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Tor");
                }
            }
        }

        public string ConnectionsSettingsWindow_Ipv4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Ipv4");
                }
            }
        }

        public string ConnectionsSettingsWindow_Ipv6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Ipv6");
                }
            }
        }

        public string ConnectionsSettingsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Up");
                }
            }
        }

        public string ConnectionsSettingsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Down");
                }
            }
        }

        public string ConnectionsSettingsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Add");
                }
            }
        }

        public string ConnectionsSettingsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Edit");
                }
            }
        }

        public string ConnectionsSettingsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Delete");
                }
            }
        }

        public string ConnectionsSettingsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Ok");
                }
            }
        }

        public string ConnectionsSettingsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Cancel");
                }
            }
        }

        public string ConnectionsSettingsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Cut");
                }
            }
        }

        public string ConnectionsSettingsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Copy");
                }
            }
        }

        public string ConnectionsSettingsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_Paste");
                }
            }
        }

        public string ConnectionsSettingsWindow_CacheResize_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionsSettingsWindow_CacheResize_Message");
                }
            }
        }


        public string ConnectionType_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_None");
                }
            }
        }

        public string ConnectionType_Tcp
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Tcp");
                }
            }
        }

        public string ConnectionType_Socks4Proxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Socks4Proxy");
                }
            }
        }

        public string ConnectionType_Socks4aProxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Socks4aProxy");
                }
            }
        }

        public string ConnectionType_Socks5Proxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Socks5Proxy");
                }
            }
        }

        public string ConnectionType_HttpProxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_HttpProxy");
                }
            }
        }


        public string TransferLimitType_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_None");
                }
            }
        }

        public string TransferLimitType_Downloads
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_Downloads");
                }
            }
        }

        public string TransferLimitType_Uploads
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_Uploads");
                }
            }
        }

        public string TransferLimitType_Total
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_Total");
                }
            }
        }


        public string ProgressWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProgressWindow_Title");
                }
            }
        }

        public string ProgressWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProgressWindow_Cancel");
                }
            }
        }

        public string ProgressWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProgressWindow_Ok");
                }
            }
        }


        public string ViewSettingsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Title");
                }
            }
        }

        public string ViewSettingsWindow_Update
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Update");
                }
            }
        }

        public string ViewSettingsWindow_UpdateUrl
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_UpdateUrl");
                }
            }
        }

        public string ViewSettingsWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_ProxyUri");
                }
            }
        }

        public string ViewSettingsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Signature");
                }
            }
        }

        public string ViewSettingsWindow_Signatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Signatures");
                }
            }
        }

        public string ViewSettingsWindow_UpdateOption
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_UpdateOption");
                }
            }
        }

        public string ViewSettingsWindow_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_None");
                }
            }
        }

        public string ViewSettingsWindow_AutoCheck
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_AutoCheck");
                }
            }
        }

        public string ViewSettingsWindow_AutoUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_AutoUpdate");
                }
            }
        }

        public string ViewSettingsWindow_Amoeba
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Amoeba");
                }
            }
        }

        public string ViewSettingsWindow_AmoebaPath
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_AmoebaPath");
                }
            }
        }

        public string ViewSettingsWindow_Fonts
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Fonts");
                }
            }
        }

        public string ViewSettingsWindow_MessageFontFamily
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_MessageFontFamily");
                }
            }
        }

        public string ViewSettingsWindow_MessageFontSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_MessageFontSize");
                }
            }
        }

        public string ViewSettingsWindow_Events
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Events");
                }
            }
        }

        public string ViewSettingsWindow_ClearUrlHistory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_ClearUrlHistory");
                }
            }
        }

        public string ViewSettingsWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Value");
                }
            }
        }

        public string ViewSettingsWindow_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Import");
                }
            }
        }

        public string ViewSettingsWindow_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Export");
                }
            }
        }

        public string ViewSettingsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Up");
                }
            }
        }

        public string ViewSettingsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Down");
                }
            }
        }

        public string ViewSettingsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Add");
                }
            }
        }

        public string ViewSettingsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Edit");
                }
            }
        }

        public string ViewSettingsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Delete");
                }
            }
        }

        public string ViewSettingsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Copy");
                }
            }
        }

        public string ViewSettingsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Ok");
                }
            }
        }

        public string ViewSettingsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Cancel");
                }
            }
        }


        public string VersionInformationWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_Title");
                }
            }
        }

        public string VersionInformationWindow_FileName
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_FileName");
                }
            }
        }

        public string VersionInformationWindow_Version
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_Version");
                }
            }
        }

        public string VersionInformationWindow_License
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_License");
                }
            }
        }

        public string VersionInformationWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_Close");
                }
            }
        }


        public string CreateSignatureWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreateSignatureWindow_Title");
                }
            }
        }

        public string CreateSignatureWindow_Nickname
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreateSignatureWindow_Nickname");
                }
            }
        }

        public string CreateSignatureWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreateSignatureWindow_Ok");
                }
            }
        }

        public string CreateSignatureWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreateSignatureWindow_Cancel");
                }
            }
        }


        public string ConnectionControl_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Uri");
                }
            }
        }

        public string ConnectionControl_Priority
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Priority");
                }
            }
        }

        public string ConnectionControl_SentByteCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_SentByteCount");
                }
            }
        }

        public string ConnectionControl_ReceivedByteCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_ReceivedByteCount");
                }
            }
        }

        public string ConnectionControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Name");
                }
            }
        }

        public string ConnectionControl_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Value");
                }
            }
        }

        public string ConnectionControl_CreateConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_CreateConnectionCount");
                }
            }
        }

        public string ConnectionControl_AcceptConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_AcceptConnectionCount");
                }
            }
        }

        public string ConnectionControl_SurroundingNodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_SurroundingNodeCount");
                }
            }
        }

        public string ConnectionControl_NodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_NodeCount");
                }
            }
        }

        public string ConnectionControl_SectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_SectionCount");
                }
            }
        }

        public string ConnectionControl_LeaderCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_LeaderCount");
                }
            }
        }

        public string ConnectionControl_CreatorCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_CreatorCount");
                }
            }
        }

        public string ConnectionControl_ManagerCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_ManagerCount");
                }
            }
        }

        public string ConnectionControl_ChannelCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_ChannelCount");
                }
            }
        }

        public string ConnectionControl_TopicCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_TopicCount");
                }
            }
        }

        public string ConnectionControl_MessageCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_MessageCount");
                }
            }
        }

        public string ConnectionControl_PushNodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushNodeCount");
                }
            }
        }

        public string ConnectionControl_PushSectionRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushSectionRequestCount");
                }
            }
        }

        public string ConnectionControl_PushLeaderCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushLeaderCount");
                }
            }
        }

        public string ConnectionControl_PushCreatorCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushCreatorCount");
                }
            }
        }

        public string ConnectionControl_PushManagerCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushManagerCount");
                }
            }
        }

        public string ConnectionControl_PushChannelRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushChannelRequestCount");
                }
            }
        }

        public string ConnectionControl_PushTopicCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushTopicCount");
                }
            }
        }

        public string ConnectionControl_PushMessageCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushMessageCount");
                }
            }
        }

        public string ConnectionControl_PullNodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullNodeCount");
                }
            }
        }

        public string ConnectionControl_PullSectionRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullSectionRequestCount");
                }
            }
        }

        public string ConnectionControl_PullLeaderCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullLeaderCount");
                }
            }
        }

        public string ConnectionControl_PullCreatorCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullCreatorCount");
                }
            }
        }

        public string ConnectionControl_PullManagerCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullManagerCount");
                }
            }
        }

        public string ConnectionControl_PullChannelRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullChannelRequestCount");
                }
            }
        }

        public string ConnectionControl_PullTopicCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullTopicCount");
                }
            }
        }

        public string ConnectionControl_PullMessageCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullMessageCount");
                }
            }
        }

        public string ConnectionControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Copy");
                }
            }
        }

        public string ConnectionControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Paste");
                }
            }
        }


        public string SectionControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_New");
                }
            }
        }

        public string SectionControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Edit");
                }
            }
        }

        public string SectionControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Delete");
                }
            }
        }

        public string SectionControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Cut");
                }
            }
        }

        public string SectionControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Copy");
                }
            }
        }

        public string SectionControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_CopyInfo");
                }
            }
        }

        public string SectionControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Paste");
                }
            }
        }

        public string SectionControl_Chart
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Chart");
                }
            }
        }

        public string SectionControl_Channel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Channel");
                }
            }
        }

        public string SectionControl_Document
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Document");
                }
            }
        }


        public string ChannelControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_New");
                }
            }
        }

        public string ChannelControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Edit");
                }
            }
        }

        public string ChannelControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Delete");
                }
            }
        }

        public string ChannelControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Cut");
                }
            }
        }

        public string ChannelControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Copy");
                }
            }
        }

        public string ChannelControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_CopyInfo");
                }
            }
        }

        public string ChannelControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Paste");
                }
            }
        }

        public string ChannelControl_ChannelList
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_ChannelList");
                }
            }
        }

        public string ChannelControl_Respons
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Respons");
                }
            }
        }

        public string ChannelControl_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Trust");
                }
            }
        }

        public string ChannelControl_Trust_On
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Trust_On");
                }
            }
        }

        public string ChannelControl_Trust_Off
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Trust_Off");
                }
            }
        }

        public string ChannelControl_Lock
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Lock");
                }
            }
        }

        public string ChannelControl_LockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_LockThis");
                }
            }
        }

        public string ChannelControl_UnlockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_UnlockThis");
                }
            }
        }

        public string ChannelControl_LockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_LockAll");
                }
            }
        }

        public string ChannelControl_UnlockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_UnlockAll");
                }
            }
        }

        public string ChannelControl_Filter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Filter");
                }
            }
        }

        public string ChannelControl_FilterWord
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_FilterWord");
                }
            }
        }

        public string ChannelControl_FilterSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_FilterSignature");
                }
            }
        }

        public string ChannelControl_NewMessageOnly
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_NewMessageOnly");
                }
            }
        }

        public string ChannelControl_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Message");
                }
            }
        }

        public string ChannelControl_Topic
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Topic");
                }
            }
        }

        public string ChannelControl_AmoebaNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_AmoebaNotFound_Message");
                }
            }
        }

        public string ChannelControl_MarkAllMessagesRead
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_MarkAllMessagesRead");
                }
            }
        }

        public string ChannelControl_MarkAllMessagesRead_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_MarkAllMessagesRead_Message");
                }
            }
        }

        public string ChannelControl_ImportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_ImportLockedMessages");
                }
            }
        }

        public string ChannelControl_ExportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_ExportLockedMessages");
                }
            }
        }

        public string ChannelControl_TopicUpload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_TopicUpload");
                }
            }
        }


        public string ChartWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Title");
                }
            }
        }

        public string ChartWindow_Leader
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Leader");
                }
            }
        }

        public string ChartWindow_Creator
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Creator");
                }
            }
        }

        public string ChartWindow_Manager
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Manager");
                }
            }
        }

        public string ChartWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Name");
                }
            }
        }

        public string ChartWindow_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Id");
                }
            }
        }

        public string ChartWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Signature");
                }
            }
        }

        public string ChartWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Copy");
                }
            }
        }

        public string ChartWindow_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_CopyInfo");
                }
            }
        }

        public string ChartWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChartWindow_Close");
                }
            }
        }


        public string TopicEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicEditWindow_Title");
                }
            }
        }

        public string TopicEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicEditWindow_Edit");
                }
            }
        }

        public string TopicEditWindow_Preview
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicEditWindow_Preview");
                }
            }
        }

        public string TopicEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicEditWindow_Comment");
                }
            }
        }

        public string TopicEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicEditWindow_Ok");
                }
            }
        }

        public string TopicEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicEditWindow_Cancel");
                }
            }
        }


        public string TopicPreviewWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicPreviewWindow_Title");
                }
            }
        }

        public string TopicPreviewWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TopicPreviewWindow_Close");
                }
            }
        }


        public string MessageEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Title");
                }
            }
        }

        public string MessageEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Edit");
                }
            }
        }

        public string MessageEditWindow_Preview
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Preview");
                }
            }
        }

        public string MessageEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Comment");
                }
            }
        }

        public string MessageEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Ok");
                }
            }
        }

        public string MessageEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Cancel");
                }
            }
        }


        public string NewSectionWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewSectionWindow_Title");
                }
            }
        }

        public string NewSectionWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewSectionWindow_Name");
                }
            }
        }

        public string NewSectionWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewSectionWindow_Ok");
                }
            }
        }

        public string NewSectionWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewSectionWindow_Cancel");
                }
            }
        }


        public string NewChannelWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChannelWindow_Title");
                }
            }
        }

        public string NewChannelWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChannelWindow_Name");
                }
            }
        }

        public string NewChannelWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChannelWindow_Ok");
                }
            }
        }

        public string NewChannelWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChannelWindow_Cancel");
                }
            }
        }


        public string SectionTreeItemEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Title");
                }
            }
        }

        public string SectionTreeItemEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Signature");
                }
            }
        }

        public string SectionTreeItemEditWindow_Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Section");
                }
            }
        }

        public string SectionTreeItemEditWindow_LeaderSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_LeaderSignature");
                }
            }
        }

        public string SectionTreeItemEditWindow_Leader
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Leader");
                }
            }
        }

        public string SectionTreeItemEditWindow_Creator
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Creator");
                }
            }
        }

        public string SectionTreeItemEditWindow_Manager
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Manager");
                }
            }
        }

        public string SectionTreeItemEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Copy");
                }
            }
        }

        public string SectionTreeItemEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Ok");
                }
            }
        }

        public string SectionTreeItemEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Cancel");
                }
            }
        }


        public string LeaderControl_Creators
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Creators");
                }
            }
        }

        public string LeaderControl_Managers
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Managers");
                }
            }
        }

        public string LeaderControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Comment");
                }
            }
        }

        public string LeaderControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Signature");
                }
            }
        }

        public string LeaderControl_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Up");
                }
            }
        }

        public string LeaderControl_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Down");
                }
            }
        }

        public string LeaderControl_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Add");
                }
            }
        }

        public string LeaderControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Edit");
                }
            }
        }

        public string LeaderControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Delete");
                }
            }
        }

        public string LeaderControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Upload");
                }
            }
        }

        public string LeaderControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Cut");
                }
            }
        }

        public string LeaderControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Copy");
                }
            }
        }

        public string LeaderControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderControl_Paste");
                }
            }
        }


        public string CreatorControl_Channels
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Channels");
                }
            }
        }

        public string CreatorControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Comment");
                }
            }
        }

        public string CreatorControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Name");
                }
            }
        }

        public string CreatorControl_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Id");
                }
            }
        }

        public string CreatorControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_New");
                }
            }
        }

        public string CreatorControl_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Up");
                }
            }
        }

        public string CreatorControl_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Down");
                }
            }
        }

        public string CreatorControl_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Add");
                }
            }
        }

        public string CreatorControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Edit");
                }
            }
        }

        public string CreatorControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Delete");
                }
            }
        }

        public string CreatorControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Upload");
                }
            }
        }

        public string CreatorControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Cut");
                }
            }
        }

        public string CreatorControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Copy");
                }
            }
        }

        public string CreatorControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Paste");
                }
            }
        }


        public string ManagerControl_Trusts
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Trusts");
                }
            }
        }

        public string ManagerControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Comment");
                }
            }
        }

        public string ManagerControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Signature");
                }
            }
        }

        public string ManagerControl_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Up");
                }
            }
        }

        public string ManagerControl_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Down");
                }
            }
        }

        public string ManagerControl_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Add");
                }
            }
        }

        public string ManagerControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Edit");
                }
            }
        }

        public string ManagerControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Delete");
                }
            }
        }

        public string ManagerControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Upload");
                }
            }
        }

        public string ManagerControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Cut");
                }
            }
        }

        public string ManagerControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Copy");
                }
            }
        }

        public string ManagerControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerControl_Paste");
                }
            }
        }


        public string SearchItemEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Title");
                }
            }
        }

        public string SearchItemEditWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Name");
                }
            }
        }

        public string SearchItemEditWindow_Word
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Word");
                }
            }
        }

        public string SearchItemEditWindow_Regex
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Regex");
                }
            }
        }

        public string SearchItemEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Signature");
                }
            }
        }

        public string SearchItemEditWindow_CreationTimeRange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_CreationTimeRange");
                }
            }
        }

        public string SearchItemEditWindow_Contains
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Contains");
                }
            }
        }

        public string SearchItemEditWindow_IsIgnoreCase
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_IsIgnoreCase");
                }
            }
        }

        public string SearchItemEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Value");
                }
            }
        }

        public string SearchItemEditWindow_Min
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Min");
                }
            }
        }

        public string SearchItemEditWindow_Max
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Max");
                }
            }
        }

        public string SearchItemEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Up");
                }
            }
        }

        public string SearchItemEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Down");
                }
            }
        }

        public string SearchItemEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Add");
                }
            }
        }

        public string SearchItemEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Edit");
                }
            }
        }

        public string SearchItemEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Delete");
                }
            }
        }

        public string SearchItemEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Ok");
                }
            }
        }

        public string SearchItemEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Cancel");
                }
            }
        }

        public string SearchItemEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Cut");
                }
            }
        }

        public string SearchItemEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Copy");
                }
            }
        }

        public string SearchItemEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Paste");
                }
            }
        }


        public string ChannelListWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_Title");
                }
            }
        }

        public string ChannelListWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_Name");
                }
            }
        }

        public string ChannelListWindow_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_Id");
                }
            }
        }

        public string ChannelListWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_Copy");
                }
            }
        }

        public string ChannelListWindow_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_CopyInfo");
                }
            }
        }

        public string ChannelListWindow_Join
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_Join");
                }
            }
        }

        public string ChannelListWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelListWindow_Close");
                }
            }
        }

        #endregion

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
