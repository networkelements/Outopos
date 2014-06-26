using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml;
using Library;

namespace Outopos.Properties
{
    public delegate void UsingLanguageChangedEventHandler(object sender);

    class LanguagesManager : IThisLock
    {
        private static LanguagesManager _defaultInstance = new LanguagesManager();
        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private static string _currentLanguage;
        private static ObjectDataProvider _provider;
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
            string path = @"C:\Local\Project\Alliance-Network\Outopos\Outopos\bin\Debug\Core\Languages";

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
                var dic = new Dictionary<string, string>();

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
                _currentLanguage = "Japanese";
            }
            else if (_dic.Keys.Any(n => n == "English"))
            {
                _currentLanguage = "English";
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

            _currentLanguage = language;
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
                var dic = new Dictionary<string, string>();

                foreach (var path in _dic.Keys.ToList())
                {
                    dic[System.IO.Path.GetFileNameWithoutExtension(path)] = path;
                }

                var pairs = dic.ToList();

                pairs.Sort((x, y) =>
                {
                    return x.Key.CompareTo(y.Key);
                });

                return pairs.Select(n => n.Value).ToArray();
            }
        }

        /// <summary>
        /// 現在使用している言語
        /// </summary>
        public string CurrentLanguage
        {
            get
            {
                return _currentLanguage;
            }
        }

        public static ObjectDataProvider ResourceProvider
        {
            get
            {
                if (System.Windows.Application.Current != null)
                {
                    _provider = (ObjectDataProvider)System.Windows.Application.Current.FindResource("ResourcesInstance");
                }

                return _provider;
            }
        }

        public string Translate(string key)
        {
            if (_currentLanguage == null) return null;

            string result;

            if (_dic[_currentLanguage].TryGetValue(key, out result))
            {
                return result;
            }

            return null;
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


        public string ConnectDirection_In
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectDirection_In");
                }
            }
        }

        public string ConnectDirection_Out
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectDirection_Out");
                }
            }
        }


        public string Tag_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Tag_Name");
                }
            }
        }

        public string Tag_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Tag_Id");
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

        public string MainWindow_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Chat");
                }
            }
        }

        public string MainWindow_Wiki
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Wiki");
                }
            }
        }

        public string MainWindow_Mail
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Mail");
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

        public string MainWindow_ProfileOptions
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ProfileOptions");
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

        public string MainWindow_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Trust");
                }
            }
        }

        public string MainWindow_TrustExplorer
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_TrustExplorer");
                }
            }
        }

        public string MainWindow_TrustOptions
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_TrustOptions");
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

        public string MainWindow_ViewHelp
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ViewHelp");
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

        public string MainWindow_CheckBlocks_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckBlocks_Message");
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


        public string TrustOptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Title");
                }
            }
        }

        public string TrustOptionsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Signature");
                }
            }
        }

        public string TrustOptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Delete");
                }
            }
        }

        public string TrustOptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Cut");
                }
            }
        }

        public string TrustOptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Copy");
                }
            }
        }

        public string TrustOptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Paste");
                }
            }
        }

        public string TrustOptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Ok");
                }
            }
        }

        public string TrustOptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustOptionsWindow_Cancel");
                }
            }
        }


        public string ProfileOptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Title");
                }
            }
        }

        public string ProfileOptionsWindow_YourSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_YourSignature");
                }
            }
        }

        public string ProfileOptionsWindow_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Trust");
                }
            }
        }

        public string ProfileOptionsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Signature");
                }
            }
        }

        public string ProfileOptionsWindow_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Chat");
                }
            }
        }

        public string ProfileOptionsWindow_Wiki
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Wiki");
                }
            }
        }

        public string ProfileOptionsWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Value");
                }
            }
        }

        public string ProfileOptionsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Up");
                }
            }
        }

        public string ProfileOptionsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Down");
                }
            }
        }

        public string ProfileOptionsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Add");
                }
            }
        }

        public string ProfileOptionsWindow_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_New");
                }
            }
        }

        public string ProfileOptionsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Edit");
                }
            }
        }

        public string ProfileOptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Delete");
                }
            }
        }

        public string ProfileOptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Cut");
                }
            }
        }

        public string ProfileOptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Copy");
                }
            }
        }

        public string ProfileOptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Paste");
                }
            }
        }

        public string ProfileOptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Ok");
                }
            }
        }

        public string ProfileOptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProfileOptionsWindow_Cancel");
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


        public string TrustExplorerWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Title");
                }
            }
        }

        public string TrustExplorerWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Copy");
                }
            }
        }

        public string TrustExplorerWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Signature");
                }
            }
        }

        public string TrustExplorerWindow_Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Section");
                }
            }
        }

        public string TrustExplorerWindow_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Trust");
                }
            }
        }

        public string TrustExplorerWindow_Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Chat");
                }
            }
        }

        public string TrustExplorerWindow_Wiki
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Wiki");
                }
            }
        }

        public string TrustExplorerWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Value");
                }
            }
        }

        public string TrustExplorerWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Comment");
                }
            }
        }

        public string TrustExplorerWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TrustExplorerWindow_Close");
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


        public string ConnectionControl_Direction
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_Direction");
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

        public string ConnectionControl_BufferManagerSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_BufferManagerSize");
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

        public string ConnectionControl_BlockedConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_BlockedConnectionCount");
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

        public string ConnectionControl_DownloadCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_DownloadCount");
                }
            }
        }

        public string ConnectionControl_UploadCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_UploadCount");
                }
            }
        }

        public string ConnectionControl_ShareCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_ShareCount");
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

        public string ChatControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatControl_Upload");
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


        public string ChatTopicEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicEditWindow_Title");
                }
            }
        }

        public string ChatTopicEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicEditWindow_Edit");
                }
            }
        }

        public string ChatTopicEditWindow_Preview
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicEditWindow_Preview");
                }
            }
        }

        public string ChatTopicEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicEditWindow_Comment");
                }
            }
        }

        public string ChatTopicEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicEditWindow_Ok");
                }
            }
        }

        public string ChatTopicEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicEditWindow_Cancel");
                }
            }
        }


        public string ChatTopicPreviewWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicPreviewWindow_Title");
                }
            }
        }

        public string ChatTopicPreviewWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChatTopicPreviewWindow_Close");
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
