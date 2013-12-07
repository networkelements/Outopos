using System;
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
        private static string _usingLanguage;
        private static ObjectDataProvider provider;
        private readonly object _thisLock = new object();

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


        public string Link_Tag
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Link_Tag");
                }
            }
        }

        public string Link_Tag_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Link_Tag_Name");
                }
            }
        }

        public string Link_Tag_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Link_Tag_Id");
                }
            }
        }

        public string Link_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Link_Type");
                }
            }
        }

        public string Link_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Link_Path");
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


        public string Message_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Message_Chat");
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

        public string MainWindow_StatesBar
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_StatesBar");
                }
            }
        }

        public string MainWindow_Running
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Running");
                }
            }
        }

        public string MainWindow_Stopping
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Stopping");
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


        public string CoreOptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Title");
                }
            }
        }

        public string CoreOptionsWindow_BaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_BaseNode");
                }
            }
        }

        public string CoreOptionsWindow_Node
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Node");
                }
            }
        }

        public string CoreOptionsWindow_Uris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Uris");
                }
            }
        }

        public string CoreOptionsWindow_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Uri");
                }
            }
        }

        public string CoreOptionsWindow_OtherNodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_OtherNodes");
                }
            }
        }

        public string CoreOptionsWindow_Nodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Nodes");
                }
            }
        }

        public string CoreOptionsWindow_Client
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Client");
                }
            }
        }

        public string CoreOptionsWindow_Filters
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Filters");
                }
            }
        }

        public string CoreOptionsWindow_ConnectionType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_ConnectionType");
                }
            }
        }

        public string CoreOptionsWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_ProxyUri");
                }
            }
        }

        public string CoreOptionsWindow_UriCondition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_UriCondition");
                }
            }
        }

        public string CoreOptionsWindow_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Option");
                }
            }
        }

        public string CoreOptionsWindow_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Type");
                }
            }
        }

        public string CoreOptionsWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Host");
                }
            }
        }

        public string CoreOptionsWindow_Server
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Server");
                }
            }
        }

        public string CoreOptionsWindow_ListenUris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_ListenUris");
                }
            }
        }

        public string CoreOptionsWindow_Data
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Data");
                }
            }
        }

        public string CoreOptionsWindow_Bandwidth
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Bandwidth");
                }
            }
        }

        public string CoreOptionsWindow_BandwidthLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_BandwidthLimit");
                }
            }
        }

        public string CoreOptionsWindow_Transfer
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Transfer");
                }
            }
        }

        public string CoreOptionsWindow_TransferLimitType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_TransferLimitType");
                }
            }
        }

        public string CoreOptionsWindow_TransferLimitSpan
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_TransferLimitSpan");
                }
            }
        }

        public string CoreOptionsWindow_TransferLimitSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_TransferLimitSize");
                }
            }
        }

        public string CoreOptionsWindow_TransferInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_TransferInformation");
                }
            }
        }

        public string CoreOptionsWindow_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Downloaded");
                }
            }
        }

        public string CoreOptionsWindow_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Uploaded");
                }
            }
        }

        public string CoreOptionsWindow_Total
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Total");
                }
            }
        }

        public string CoreOptionsWindow_Reset
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Reset");
                }
            }
        }

        public string CoreOptionsWindow_ConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_ConnectionCount");
                }
            }
        }

        public string CoreOptionsWindow_DownloadDirectory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_DownloadDirectory");
                }
            }
        }

        public string CoreOptionsWindow_CacheSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_CacheSize");
                }
            }
        }

        public string CoreOptionsWindow_Events
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Events");
                }
            }
        }

        public string CoreOptionsWindow_Events_Connection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Events_Connection");
                }
            }
        }

        public string CoreOptionsWindow_Events_OpenPortAndGetIpAddress
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Events_OpenPortAndGetIpAddress");
                }
            }
        }

        public string CoreOptionsWindow_Events_UseI2p
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Events_UseI2p");
                }
            }
        }

        public string CoreOptionsWindow_Events_UseI2p_SamBridgeUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Events_UseI2p_SamBridgeUri");
                }
            }
        }

        public string CoreOptionsWindow_Tor
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Tor");
                }
            }
        }

        public string CoreOptionsWindow_Ipv4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Ipv4");
                }
            }
        }

        public string CoreOptionsWindow_Ipv6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Ipv6");
                }
            }
        }

        public string CoreOptionsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Up");
                }
            }
        }

        public string CoreOptionsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Down");
                }
            }
        }

        public string CoreOptionsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Add");
                }
            }
        }

        public string CoreOptionsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Edit");
                }
            }
        }

        public string CoreOptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Delete");
                }
            }
        }

        public string CoreOptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Ok");
                }
            }
        }

        public string CoreOptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Cancel");
                }
            }
        }

        public string CoreOptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Cut");
                }
            }
        }

        public string CoreOptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Copy");
                }
            }
        }

        public string CoreOptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_Paste");
                }
            }
        }

        public string CoreOptionsWindow_CacheResize_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CoreOptionsWindow_CacheResize_Message");
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

        public string ViewSettingsWindow_Lair
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_Lair");
                }
            }
        }

        public string ViewSettingsWindow_LairPath
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_LairPath");
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

        public string ConnectionControl_ChatCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_ChatCount");
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

        public string ConnectionControl_PushChatRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushChatRequestCount");
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

        public string ConnectionControl_PullChatRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullChatRequestCount");
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

        public string SectionControl_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Chat");
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

        public string SectionControl_LairNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_LairNotFound_Message");
                }
            }
        }


        public string ChatControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_New");
                }
            }
        }

        public string ChatControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Edit");
                }
            }
        }

        public string ChatControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Delete");
                }
            }
        }

        public string ChatControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Cut");
                }
            }
        }

        public string ChatControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Copy");
                }
            }
        }

        public string ChatControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_CopyInfo");
                }
            }
        }

        public string ChatControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Paste");
                }
            }
        }

        public string ChatControl_ChatList
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_ChatList");
                }
            }
        }

        public string ChatControl_Respons
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Respons");
                }
            }
        }

        public string ChatControl_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Trust");
                }
            }
        }

        public string ChatControl_Trust_On
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Trust_On");
                }
            }
        }

        public string ChatControl_Trust_Off
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Trust_Off");
                }
            }
        }

        public string ChatControl_Lock
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Lock");
                }
            }
        }

        public string ChatControl_LockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_LockThis");
                }
            }
        }

        public string ChatControl_UnlockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_UnlockThis");
                }
            }
        }

        public string ChatControl_LockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_LockAll");
                }
            }
        }

        public string ChatControl_UnlockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_UnlockAll");
                }
            }
        }

        public string ChatControl_Filter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Filter");
                }
            }
        }

        public string ChatControl_FilterWord
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_FilterWord");
                }
            }
        }

        public string ChatControl_FilterSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_FilterSignature");
                }
            }
        }

        public string ChatControl_NewMessageOnly
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_NewMessageOnly");
                }
            }
        }

        public string ChatControl_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Message");
                }
            }
        }

        public string ChatControl_Topic
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Topic");
                }
            }
        }

        public string ChatControl_LairNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_LairNotFound_Message");
                }
            }
        }

        public string ChatControl_MarkAllMessagesRead
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_MarkAllMessagesRead");
                }
            }
        }

        public string ChatControl_MarkAllMessagesRead_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_MarkAllMessagesRead_Message");
                }
            }
        }

        public string ChatControl_ImportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_ImportLockedMessages");
                }
            }
        }

        public string ChatControl_ExportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_ExportLockedMessages");
                }
            }
        }

        public string ChatControl_TopicUpload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_TopicUpload");
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


        public string NewChatWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChatWindow_Title");
                }
            }
        }

        public string NewChatWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChatWindow_Name");
                }
            }
        }

        public string NewChatWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChatWindow_Ok");
                }
            }
        }

        public string NewChatWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewChatWindow_Cancel");
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


        public string CreatorControl_Chats
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorControl_Chats");
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


        public string ChatListWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_Title");
                }
            }
        }

        public string ChatListWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_Name");
                }
            }
        }

        public string ChatListWindow_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_Id");
                }
            }
        }

        public string ChatListWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_Copy");
                }
            }
        }

        public string ChatListWindow_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_CopyInfo");
                }
            }
        }

        public string ChatListWindow_Join
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_Join");
                }
            }
        }

        public string ChatListWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatListWindow_Close");
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
