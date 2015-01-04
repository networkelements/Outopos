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

        public string Languages_Chinese_Traditional
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Chinese_Traditional");
                }
            }
        }

        public string Languages_Chinese_Simplified
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Chinese_Simplified");
                }
            }
        }

        public string Languages_Ukrainian
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Ukrainian");
                }
            }
        }

        public string Languages_Russian
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Russian");
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

        public string MainWindow_SendingSpeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_SendingSpeed");
                }
            }
        }

        public string MainWindow_ReceivingSpeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ReceivingSpeed");
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


        public string OptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Title");
                }
            }
        }

        public string OptionsWindow_Core
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Core");
                }
            }
        }

        public string OptionsWindow_View
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_View");
                }
            }
        }

        public string OptionsWindow_BaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_BaseNode");
                }
            }
        }

        public string OptionsWindow_Node
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Node");
                }
            }
        }

        public string OptionsWindow_Uris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Uris");
                }
            }
        }

        public string OptionsWindow_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Uri");
                }
            }
        }

        public string OptionsWindow_OtherNodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_OtherNodes");
                }
            }
        }

        public string OptionsWindow_Nodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Nodes");
                }
            }
        }

        public string OptionsWindow_Client
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Client");
                }
            }
        }

        public string OptionsWindow_Filters
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Filters");
                }
            }
        }

        public string OptionsWindow_ConnectionType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ConnectionType");
                }
            }
        }

        public string OptionsWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ProxyUri");
                }
            }
        }

        public string OptionsWindow_UriCondition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_UriCondition");
                }
            }
        }

        public string OptionsWindow_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Option");
                }
            }
        }

        public string OptionsWindow_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Type");
                }
            }
        }

        public string OptionsWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Host");
                }
            }
        }

        public string OptionsWindow_Server
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Server");
                }
            }
        }

        public string OptionsWindow_ListenUris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ListenUris");
                }
            }
        }

        public string OptionsWindow_Data
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Data");
                }
            }
        }

        public string OptionsWindow_Bandwidth
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Bandwidth");
                }
            }
        }

        public string OptionsWindow_BandwidthLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_BandwidthLimit");
                }
            }
        }

        public string OptionsWindow_Transfer
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Transfer");
                }
            }
        }

        public string OptionsWindow_TransferLimitType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferLimitType");
                }
            }
        }

        public string OptionsWindow_TransferLimitSpan
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferLimitSpan");
                }
            }
        }

        public string OptionsWindow_TransferLimitSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferLimitSize");
                }
            }
        }

        public string OptionsWindow_TransferInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferInformation");
                }
            }
        }

        public string OptionsWindow_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Downloaded");
                }
            }
        }

        public string OptionsWindow_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Uploaded");
                }
            }
        }

        public string OptionsWindow_Total
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Total");
                }
            }
        }

        public string OptionsWindow_Reset
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Reset");
                }
            }
        }

        public string OptionsWindow_ConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ConnectionCount");
                }
            }
        }

        public string OptionsWindow_DownloadDirectory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_DownloadDirectory");
                }
            }
        }

        public string OptionsWindow_DownloadDirectory_Description
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_DownloadDirectory_Description");
                }
            }
        }

        public string OptionsWindow_CacheSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_CacheSize");
                }
            }
        }

        public string OptionsWindow_Events
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events");
                }
            }
        }

        public string OptionsWindow_Events_Connection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_Connection");
                }
            }
        }

        public string OptionsWindow_Events_OpenPortAndGetIpAddress
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_OpenPortAndGetIpAddress");
                }
            }
        }

        public string OptionsWindow_Events_UseI2p
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_UseI2p");
                }
            }
        }

        public string OptionsWindow_Events_UseI2p_SamBridgeUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_UseI2p_SamBridgeUri");
                }
            }
        }

        public string OptionsWindow_Tor
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Tor");
                }
            }
        }

        public string OptionsWindow_Ipv4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Ipv4");
                }
            }
        }

        public string OptionsWindow_Ipv6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Ipv6");
                }
            }
        }

        public string OptionsWindow_Update
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Update");
                }
            }
        }

        public string OptionsWindow_UpdateUrl
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_UpdateUrl");
                }
            }
        }

        public string OptionsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Signature");
                }
            }
        }

        public string OptionsWindow_Signatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Signatures");
                }
            }
        }

        public string OptionsWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Keywords");
                }
            }
        }

        public string OptionsWindow_Box
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Box");
                }
            }
        }

        public string OptionsWindow_RelateBoxFile
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_RelateBoxFile");
                }
            }
        }

        public string OptionsWindow_OpenBox
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_OpenBox");
                }
            }
        }

        public string OptionsWindow_ExtractTo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ExtractTo");
                }
            }
        }

        public string OptionsWindow_UpdateOption
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_UpdateOption");
                }
            }
        }

        public string OptionsWindow_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_None");
                }
            }
        }

        public string OptionsWindow_AutoCheck
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_AutoCheck");
                }
            }
        }

        public string OptionsWindow_AutoUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_AutoUpdate");
                }
            }
        }

        public string OptionsWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Value");
                }
            }
        }

        public string OptionsWindow_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Import");
                }
            }
        }

        public string OptionsWindow_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Export");
                }
            }
        }

        public string OptionsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Up");
                }
            }
        }

        public string OptionsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Down");
                }
            }
        }

        public string OptionsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Add");
                }
            }
        }

        public string OptionsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Edit");
                }
            }
        }

        public string OptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Delete");
                }
            }
        }

        public string OptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Ok");
                }
            }
        }

        public string OptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Cancel");
                }
            }
        }

        public string OptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Cut");
                }
            }
        }

        public string OptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Copy");
                }
            }
        }

        public string OptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Paste");
                }
            }
        }

        public string OptionsWindow_CacheResize_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_CacheResize_Message");
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
