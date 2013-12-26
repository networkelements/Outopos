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
            string path = @"C:\Local\Project\Lair\Lair\bin\Debug\Core\Languages";

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

        public string Section_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Section_Option");
                }
            }
        }


        public string Wiki_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Wiki_Name");
                }
            }
        }

        public string Wiki_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Wiki_Id");
                }
            }
        }

        public string Wiki_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Wiki_Option");
                }
            }
        }


        public string Chat_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Chat_Name");
                }
            }
        }

        public string Chat_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Chat_Id");
                }
            }
        }

        public string Chat_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Chat_Option");
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

        public string MainWindow_Core
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Core");
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

        public string MainWindow_CoreOptions
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CoreOptions");
                }
            }
        }

        public string MainWindow_Cache
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Cache");
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

        public string MainWindow_ViewOptions
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ViewOptions");
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

        public string MainWindow_CheckInternalBlocks_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckInternalBlocks_Message");
                }
            }
        }

        public string MainWindow_CheckExternalBlocks_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckExternalBlocks_Message");
                }
            }
        }

        public string MainWindow_CheckBlocks_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckBlocks_State");
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


        public string ViewOptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Title");
                }
            }
        }

        public string ViewOptionsWindow_Update
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Update");
                }
            }
        }

        public string ViewOptionsWindow_UpdateUrl
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_UpdateUrl");
                }
            }
        }

        public string ViewOptionsWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_ProxyUri");
                }
            }
        }

        public string ViewOptionsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Signature");
                }
            }
        }

        public string ViewOptionsWindow_Signatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Signatures");
                }
            }
        }

        public string ViewOptionsWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Keywords");
                }
            }
        }

        public string ViewOptionsWindow_UpdateOption
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_UpdateOption");
                }
            }
        }

        public string ViewOptionsWindow_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_None");
                }
            }
        }

        public string ViewOptionsWindow_AutoCheck
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_AutoCheck");
                }
            }
        }

        public string ViewOptionsWindow_AutoUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_AutoUpdate");
                }
            }
        }

        public string ViewOptionsWindow_Amoeba
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Amoeba");
                }
            }
        }

        public string ViewOptionsWindow_AmoebaPath
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_AmoebaPath");
                }
            }
        }

        public string ViewOptionsWindow_Fonts
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Fonts");
                }
            }
        }

        public string ViewOptionsWindow_MessageFontFamily
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_MessageFontFamily");
                }
            }
        }

        public string ViewOptionsWindow_MessageFontSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_MessageFontSize");
                }
            }
        }

        public string ViewOptionsWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Value");
                }
            }
        }

        public string ViewOptionsWindow_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Import");
                }
            }
        }

        public string ViewOptionsWindow_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Export");
                }
            }
        }

        public string ViewOptionsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Up");
                }
            }
        }

        public string ViewOptionsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Down");
                }
            }
        }

        public string ViewOptionsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Add");
                }
            }
        }

        public string ViewOptionsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Edit");
                }
            }
        }

        public string ViewOptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Delete");
                }
            }
        }

        public string ViewOptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Ok");
                }
            }
        }

        public string ViewOptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Cancel");
                }
            }
        }

        public string ViewOptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Cut");
                }
            }
        }

        public string ViewOptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Copy");
                }
            }
        }

        public string ViewOptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewOptionsWindow_Paste");
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


        public string SignatureWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Title");
                }
            }
        }

        public string SignatureWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Signature");
                }
            }
        }

        public string SignatureWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Ok");
                }
            }
        }

        public string SignatureWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Cancel");
                }
            }
        }


        public string NameWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Title");
                }
            }
        }

        public string NameWindow_Title_Category
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Title_Category");
                }
            }
        }

        public string NameWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Name");
                }
            }
        }

        public string NameWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Ok");
                }
            }
        }

        public string NameWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Cancel");
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

        public string ConnectionControl_RelayBlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_RelayBlockCount");
                }
            }
        }

        public string ConnectionControl_FreeSpace
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_FreeSpace");
                }
            }
        }

        public string ConnectionControl_LockSpace
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_LockSpace");
                }
            }
        }

        public string ConnectionControl_UsingSpace
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_UsingSpace");
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

        public string ConnectionControl_HeaderCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_HeaderCount");
                }
            }
        }

        public string ConnectionControl_BlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_BlockCount");
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

        public string ConnectionControl_PushBlockLinkCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushBlockLinkCount");
                }
            }
        }

        public string ConnectionControl_PushBlockRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushBlockRequestCount");
                }
            }
        }

        public string ConnectionControl_PushBlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushBlockCount");
                }
            }
        }

        public string ConnectionControl_PushHeaderRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushHeaderRequestCount");
                }
            }
        }

        public string ConnectionControl_PushHeaderCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushHeaderCount");
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

        public string ConnectionControl_PullBlockLinkCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullBlockLinkCount");
                }
            }
        }

        public string ConnectionControl_PullBlockRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullBlockRequestCount");
                }
            }
        }

        public string ConnectionControl_PullBlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullBlockCount");
                }
            }
        }

        public string ConnectionControl_PullHeaderRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullHeaderRequestCount");
                }
            }
        }

        public string ConnectionControl_PullHeaderCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullHeaderCount");
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


        public string SectionControl_NewSection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_NewSection");
                }
            }
        }

        public string SectionControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_NewCategory");
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

        public string SectionControl_TrustSignaturesPreview
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_TrustSignaturesPreview");
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

        public string SectionControl_Wiki
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Wiki");
                }
            }
        }

        public string SectionControl_Mail
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionControl_Mail");
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

        public string NewSectionWindow_SectionName
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewSectionWindow_SectionName");
                }
            }
        }

        public string NewSectionWindow_LeaderSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NewSectionWindow_LeaderSignature");
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

        public string SectionTreeItemEditWindow_YourSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_YourSignature");
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

        public string SectionTreeItemEditWindow_YourProfile
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_YourProfile");
                }
            }
        }

        public string SectionTreeItemEditWindow_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Trust");
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

        public string SectionTreeItemEditWindow_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Chat");
                }
            }
        }

        public string SectionTreeItemEditWindow_Wiki
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Wiki");
                }
            }
        }

        public string SectionTreeItemEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Value");
                }
            }
        }

        public string SectionTreeItemEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Comment");
                }
            }
        }

        public string SectionTreeItemEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Up");
                }
            }
        }

        public string SectionTreeItemEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Down");
                }
            }
        }

        public string SectionTreeItemEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Add");
                }
            }
        }

        public string SectionTreeItemEditWindow_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_New");
                }
            }
        }

        public string SectionTreeItemEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Edit");
                }
            }
        }

        public string SectionTreeItemEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Delete");
                }
            }
        }

        public string SectionTreeItemEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Cut");
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

        public string SectionTreeItemEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionTreeItemEditWindow_Paste");
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


        public string TrustSignaturesPreviewWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Title");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Copy");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Signature");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Section");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Trust");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Chat");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Wiki
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Wiki");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Value");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Comment");
                }
            }
        }

        public string TrustSignaturesPreviewWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustSignaturesPreviewWindow_Close");
                }
            }
        }


        public string ChatControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_NewCategory");
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


        public string ChatMessageEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatMessageEditWindow_Title");
                }
            }
        }

        public string ChatMessageEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatMessageEditWindow_Edit");
                }
            }
        }

        public string ChatMessageEditWindow_Preview
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatMessageEditWindow_Preview");
                }
            }
        }

        public string ChatMessageEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatMessageEditWindow_Comment");
                }
            }
        }

        public string ChatMessageEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatMessageEditWindow_Ok");
                }
            }
        }

        public string ChatMessageEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatMessageEditWindow_Cancel");
                }
            }
        }


        public string MailControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_NewCategory");
                }
            }
        }

        public string MailControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Edit");
                }
            }
        }

        public string MailControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Delete");
                }
            }
        }

        public string MailControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Cut");
                }
            }
        }

        public string MailControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Copy");
                }
            }
        }

        public string MailControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_CopyInfo");
                }
            }
        }

        public string MailControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Paste");
                }
            }
        }

        public string MailControl_SignatureList
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_SignatureList");
                }
            }
        }

        public string MailControl_Respons
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Respons");
                }
            }
        }

        public string MailControl_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MailControl_Message");
                }
            }
        }


        public string SignatureListWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureListWindow_Title");
                }
            }
        }

        public string SignatureListWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureListWindow_Value");
                }
            }
        }

        public string SignatureListWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureListWindow_Copy");
                }
            }
        }

        public string SignatureListWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureListWindow_Add");
                }
            }
        }

        public string SignatureListWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureListWindow_Close");
                }
            }
        }


        public string SectionMessageEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionMessageEditWindow_Title");
                }
            }
        }

        public string SectionMessageEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionMessageEditWindow_Edit");
                }
            }
        }

        public string SectionMessageEditWindow_Preview
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionMessageEditWindow_Preview");
                }
            }
        }

        public string SectionMessageEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionMessageEditWindow_Comment");
                }
            }
        }

        public string SectionMessageEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionMessageEditWindow_Ok");
                }
            }
        }

        public string SectionMessageEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SectionMessageEditWindow_Cancel");
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
