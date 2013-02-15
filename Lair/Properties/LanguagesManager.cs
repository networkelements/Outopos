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

                list.Sort(delegate(string x, string y)
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

        public string MainWindow_Channel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Channel");
                }
            }
        }

        public string MainWindow_Control
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Control");
                }
            }
        }

        public string MainWindow_Search
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Search");
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

        public string ViewSettingsWindow_SeedDeleteExpires
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ViewSettingsWindow_SeedDeleteExpires");
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

        public string ConnectionControl_FilterCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_FilterCount");
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

        public string ConnectionControl_PushFilterCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PushFilterCount");
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

        public string ConnectionControl_PullFilterCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_PullFilterCount");
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


        public string ChannelControl_NewChannel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_NewChannel");
                }
            }
        }

        public string ChannelControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_NewCategory");
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

        public string ChannelControl_FilterMessage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_FilterMessage");
                }
            }
        }

        public string ChannelControl_SignatureFilter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_SignatureFilter");
                }
            }
        }

        public string ChannelControl_NewMessage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_NewMessage");
                }
            }
        }

        public string ChannelControl_OnlyUnread
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_OnlyUnread");
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


        public string ControlControl_Chart
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlControl_Chart");
                }
            }
        }

        public string ControlControl_Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlControl_Section");
                }
            }
        }

        public string ControlControl_Channel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlControl_Channel");
                }
            }
        }


        public string ControlSectionControl_Leader
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Leader");
                }
            }
        }

        public string ControlSectionControl_Manager
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Manager");
                }
            }
        }

        public string ControlSectionControl_Creator
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Creator");
                }
            }
        }

        public string ControlSectionControl_NewSection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_NewSection");
                }
            }
        }

        public string ControlSectionControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_New");
                }
            }
        }

        public string ControlSectionControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Edit");
                }
            }
        }

        public string ControlSectionControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Delete");
                }
            }
        }

        public string ControlSectionControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Cut");
                }
            }
        }

        public string ControlSectionControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Copy");
                }
            }
        }

        public string ControlSectionControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_CopyInfo");
                }
            }
        }

        public string ControlSectionControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Paste");
                }
            }
        }

        public string ControlSectionControl_Respons
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Respons");
                }
            }
        }

        public string ControlSectionControl_Lock
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Lock");
                }
            }
        }

        public string ControlSectionControl_LockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_LockThis");
                }
            }
        }

        public string ControlSectionControl_UnlockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_UnlockThis");
                }
            }
        }

        public string ControlSectionControl_LockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_LockAll");
                }
            }
        }

        public string ControlSectionControl_UnlockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_UnlockAll");
                }
            }
        }

        public string ControlSectionControl_Filter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_Filter");
                }
            }
        }

        public string ControlSectionControl_FilterWord
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_FilterWord");
                }
            }
        }

        public string ControlSectionControl_FilterSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_FilterSignature");
                }
            }
        }

        public string ControlSectionControl_FilterMessage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_FilterMessage");
                }
            }
        }

        public string ControlSectionControl_SignatureFilter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_SignatureFilter");
                }
            }
        }

        public string ControlSectionControl_NewMessage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_NewMessage");
                }
            }
        }

        public string ControlSectionControl_OnlyUnread
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_OnlyUnread");
                }
            }
        }

        public string ControlSectionControl_AmoebaNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_AmoebaNotFound_Message");
                }
            }
        }

        public string ControlSectionControl_MarkAllMessagesRead
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_MarkAllMessagesRead");
                }
            }
        }

        public string ControlSectionControl_MarkAllMessagesRead_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_MarkAllMessagesRead_Message");
                }
            }
        }

        public string ControlSectionControl_ImportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_ImportLockedMessages");
                }
            }
        }

        public string ControlSectionControl_ExportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlSectionControl_ExportLockedMessages");
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


        public string LeaderEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Title");
                }
            }
        }

        public string LeaderEditWindow_Managers
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Managers");
                }
            }
        }

        public string LeaderEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Comment");
                }
            }
        }

        public string LeaderEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Signature");
                }
            }
        }

        public string LeaderEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Value");
                }
            }
        }

        public string LeaderEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Up");
                }
            }
        }

        public string LeaderEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Down");
                }
            }
        }

        public string LeaderEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Add");
                }
            }
        }

        public string LeaderEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Edit");
                }
            }
        }

        public string LeaderEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Delete");
                }
            }
        }

        public string LeaderEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Ok");
                }
            }
        }

        public string LeaderEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Cancel");
                }
            }
        }

        public string LeaderEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Cut");
                }
            }
        }

        public string LeaderEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Copy");
                }
            }
        }

        public string LeaderEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LeaderEditWindow_Paste");
                }
            }
        }


        public string ManagerEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Title");
                }
            }
        }

        public string ManagerEditWindow_Managers
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Managers");
                }
            }
        }

        public string ManagerEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Comment");
                }
            }
        }

        public string ManagerEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Signature");
                }
            }
        }

        public string ManagerEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Value");
                }
            }
        }

        public string ManagerEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Up");
                }
            }
        }

        public string ManagerEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Down");
                }
            }
        }

        public string ManagerEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Add");
                }
            }
        }

        public string ManagerEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Edit");
                }
            }
        }

        public string ManagerEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Delete");
                }
            }
        }

        public string ManagerEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Ok");
                }
            }
        }

        public string ManagerEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Cancel");
                }
            }
        }

        public string ManagerEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Cut");
                }
            }
        }

        public string ManagerEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Copy");
                }
            }
        }

        public string ManagerEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ManagerEditWindow_Paste");
                }
            }
        }


        public string CreatorEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Title");
                }
            }
        }

        public string CreatorEditWindow_Filters
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Filters");
                }
            }
        }

        public string CreatorEditWindow_Channels
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Channels");
                }
            }
        }

        public string CreatorEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Comment");
                }
            }
        }

        public string CreatorEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Signature");
                }
            }
        }

        public string CreatorEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Value");
                }
            }
        }

        public string CreatorEditWindow_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_New");
                }
            }
        }

        public string CreatorEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Up");
                }
            }
        }

        public string CreatorEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Down");
                }
            }
        }

        public string CreatorEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Add");
                }
            }
        }

        public string CreatorEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Edit");
                }
            }
        }

        public string CreatorEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Delete");
                }
            }
        }

        public string CreatorEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Ok");
                }
            }
        }

        public string CreatorEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Cancel");
                }
            }
        }

        public string CreatorEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Cut");
                }
            }
        }

        public string CreatorEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Copy");
                }
            }
        }

        public string CreatorEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CreatorEditWindow_Paste");
                }
            }
        }


        public string ControlChannelControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_New");
                }
            }
        }

        public string ControlChannelControl_NewChannel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_NewChannel");
                }
            }
        }

        public string ControlChannelControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_NewCategory");
                }
            }
        }

        public string ControlChannelControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Edit");
                }
            }
        }

        public string ControlChannelControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Delete");
                }
            }
        }

        public string ControlChannelControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Cut");
                }
            }
        }

        public string ControlChannelControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Copy");
                }
            }
        }

        public string ControlChannelControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_CopyInfo");
                }
            }
        }

        public string ControlChannelControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Paste");
                }
            }
        }

        public string ControlChannelControl_Respons
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Respons");
                }
            }
        }

        public string ControlChannelControl_Lock
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Lock");
                }
            }
        }

        public string ControlChannelControl_LockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_LockThis");
                }
            }
        }

        public string ControlChannelControl_UnlockThis
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_UnlockThis");
                }
            }
        }

        public string ControlChannelControl_LockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_LockAll");
                }
            }
        }

        public string ControlChannelControl_UnlockAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_UnlockAll");
                }
            }
        }

        public string ControlChannelControl_Filter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_Filter");
                }
            }
        }

        public string ControlChannelControl_FilterWord
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_FilterWord");
                }
            }
        }

        public string ControlChannelControl_FilterSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_FilterSignature");
                }
            }
        }

        public string ControlChannelControl_FilterMessage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_FilterMessage");
                }
            }
        }

        public string ControlChannelControl_SignatureFilter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_SignatureFilter");
                }
            }
        }

        public string ControlChannelControl_NewMessage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_NewMessage");
                }
            }
        }

        public string ControlChannelControl_OnlyUnread
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_OnlyUnread");
                }
            }
        }

        public string ControlChannelControl_AmoebaNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_AmoebaNotFound_Message");
                }
            }
        }

        public string ControlChannelControl_MarkAllMessagesRead
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_MarkAllMessagesRead");
                }
            }
        }

        public string ControlChannelControl_MarkAllMessagesRead_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_MarkAllMessagesRead_Message");
                }
            }
        }

        public string ControlChannelControl_ImportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_ImportLockedMessages");
                }
            }
        }

        public string ControlChannelControl_ExportLockedMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ControlChannelControl_ExportLockedMessages");
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
