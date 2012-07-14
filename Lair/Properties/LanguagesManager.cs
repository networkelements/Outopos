using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Data;
using Library;

namespace Lair.Properties
{
    class LanguagesManager : IThisLock
    {
        private static LanguagesManager _defaultInstance = new LanguagesManager();
        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private static string _usingLanguage = null;
        private static ObjectDataProvider provider;
        private object _thisLock = new object();

        static LanguagesManager()
        {
#if DEBUG
            string path = @"C:\Core\Project\Lair\Lair\bin\Debug\Core\Languages";
#else
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
#endif

            LanguagesManager.Load(path);
        }

        private static void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();

                using (XmlTextReader xml = new XmlTextReader(path))
                {
                    string key = "";
                    string value = "";

                    try
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Translate")
                                {
                                    key = xml.GetAttribute("Key");
                                    value = xml.GetAttribute("Value");
                                    dic.Add(key, value);
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

            if (_dic.Keys.Any(n => n == "English"))
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
            ResourceProvider.Refresh();
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


        public string MainWindow_Starting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Starting");
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

        public string MainWindow_Settings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Settings");
                }
            }
        }

        public string MainWindow_KeywordSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_KeywordSetting");
                }
            }
        }

        public string MainWindow_SignatureSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_SignatureSetting");
                }
            }
        }

        public string MainWindow_ConnectionSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ConnectionSetting");
                }
            }
        }

        public string MainWindow_UserInterfaceSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_UserInterfaceSetting");
                }
            }
        }

        public string MainWindow_CheckingBlocks
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckingBlocks");
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

        public string MainWindow_UpdateCheck
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_UpdateCheck");
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

        public string MainWindow_SpaceNotFound
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_SpaceNotFound");
                }
            }
        }

        public string MainWindow_CheckingBlocks_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckingBlocks_Message");
                }
            }
        }

        public string MainWindow_CheckingBlocks_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckingBlocks_State");
                }
            }
        }

        public string MainWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Copy");
                }
            }
        }

        public string MainWindow_UpdateCheck_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_UpdateCheck_Message");
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


        public string UserInterfaceWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Title");
                }
            }
        }

        public string UserInterfaceWindow_Update
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Update");
                }
            }
        }

        public string UserInterfaceWindow_UpdateUrl
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_UpdateUrl");
                }
            }
        }

        public string UserInterfaceWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_ProxyUri");
                }
            }
        }

        public string UserInterfaceWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Signature");
                }
            }
        }

        public string UserInterfaceWindow_Keyword
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Keyword");
                }
            }
        }

        public string UserInterfaceWindow_Miscellaneous
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Miscellaneous");
                }
            }
        }

        public string UserInterfaceWindow_SearchFilterSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_SearchFilterSettings");
                }
            }
        }

        public string UserInterfaceWindow_RelateSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_RelateSettings");
                }
            }
        }

        public string UserInterfaceWindow_RelateBoxFile
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_RelateBoxFile");
                }
            }
        }

        public string UserInterfaceWindow_Cache
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Cache");
                }
            }
        }

        public string UserInterfaceWindow_Uploading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Uploading");
                }
            }
        }

        public string UserInterfaceWindow_Downloading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Downloading");
                }
            }
        }

        public string UserInterfaceWindow_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Uploaded");
                }
            }
        }

        public string UserInterfaceWindow_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Downloaded");
                }
            }
        }

        public string UserInterfaceWindow_UpdateOption
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_UpdateOption");
                }
            }
        }

        public string UserInterfaceWindow_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_None");
                }
            }
        }

        public string UserInterfaceWindow_AutoCheck
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_AutoCheck");
                }
            }
        }

        public string UserInterfaceWindow_AutoUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_AutoUpdate");
                }
            }
        }

        public string UserInterfaceWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Value");
                }
            }
        }

        public string UserInterfaceWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Up");
                }
            }
        }

        public string UserInterfaceWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Down");
                }
            }
        }

        public string UserInterfaceWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Add");
                }
            }
        }

        public string UserInterfaceWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Edit");
                }
            }
        }

        public string UserInterfaceWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Delete");
                }
            }
        }

        public string UserInterfaceWindow_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Import");
                }
            }
        }

        public string UserInterfaceWindow_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Export");
                }
            }
        }

        public string UserInterfaceWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Ok");
                }
            }
        }

        public string UserInterfaceWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Cancel");
                }
            }
        }


        public string ConnectionWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Title");
                }
            }
        }

        public string ConnectionWindow_BaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_BaseNode");
                }
            }
        }

        public string ConnectionWindow_Node
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Node");
                }
            }
        }

        public string ConnectionWindow_Uris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Uris");
                }
            }
        }

        public string ConnectionWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Up");
                }
            }
        }

        public string ConnectionWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Down");
                }
            }
        }

        public string ConnectionWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Add");
                }
            }
        }

        public string ConnectionWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Edit");
                }
            }
        }

        public string ConnectionWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Delete");
                }
            }
        }

        public string ConnectionWindow_OtherNodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_OtherNodes");
                }
            }
        }

        public string ConnectionWindow_Nodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Nodes");
                }
            }
        }

        public string ConnectionWindow_Client
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Client");
                }
            }
        }

        public string ConnectionWindow_Filters
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Filters");
                }
            }
        }

        public string ConnectionWindow_ConnectionType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_ConnectionType");
                }
            }
        }

        public string ConnectionWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_ProxyUri");
                }
            }
        }

        public string ConnectionWindow_UriCondition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_UriCondition");
                }
            }
        }

        public string ConnectionWindow_Condition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Condition");
                }
            }
        }

        public string ConnectionWindow_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Type");
                }
            }
        }

        public string ConnectionWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Host");
                }
            }
        }

        public string ConnectionWindow_Server
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Server");
                }
            }
        }

        public string ConnectionWindow_ListenUris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_ListenUris");
                }
            }
        }

        public string ConnectionWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Ok");
                }
            }
        }

        public string ConnectionWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Cancel");
                }
            }
        }

        public string ConnectionWindow_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Uri");
                }
            }
        }

        public string ConnectionWindow_Keyword
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Keyword");
                }
            }
        }

        public string ConnectionWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Keywords");
                }
            }
        }

        public string ConnectionWindow_Miscellaneous
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Miscellaneous");
                }
            }
        }

        public string ConnectionWindow_DownloadDirectory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_DownloadDirectory");
                }
            }
        }

        public string ConnectionWindow_ConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_ConnectionCount");
                }
            }
        }

        public string ConnectionWindow_DownloadingLowerLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_DownloadingLowerLimit");
                }
            }
        }

        public string ConnectionWindow_UploadingLowerLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_UploadingLowerLimit");
                }
            }
        }

        public string ConnectionWindow_CacheSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_CacheSize");
                }
            }
        }

        public string ConnectionWindow_CoreSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_CoreSettings");
                }
            }
        }

        public string ConnectionWindow_AutoSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_AutoSettings");
                }
            }
        }

        public string ConnectionWindow_AutoUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_AutoUpdate");
                }
            }
        }

        public string ConnectionWindow_AutoBaseNodeSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_AutoBaseNodeSetting");
                }
            }
        }

        public string ConnectionWindow_UPnP
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_UPnP");
                }
            }
        }

        public string ConnectionWindow_Tor
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Tor");
                }
            }
        }

        public string ConnectionWindow_Ipv4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Ipv4");
                }
            }
        }

        public string ConnectionWindow_Ipv6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Ipv6");
                }
            }
        }

        public string ConnectionWindow_Extends
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Extends");
                }
            }
        }

        public string ConnectionWindow_AutoSetting
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_AutoSetting");
                }
            }
        }

        public string ConnectionWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Copy");
                }
            }
        }

        public string ConnectionWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Paste");
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

        public string ConnectionControl_SeedCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_SeedCount");
                }
            }
        }

        public string ConnectionControl_CacheSeedCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionControl_CacheSeedCount");
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


        public string ChannelControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Name");
                }
            }
        }

        public string ChannelControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Signature");
                }
            }
        }

        public string ChannelControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_State");
                }
            }
        }

        public string ChannelControl_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Keywords");
                }
            }
        }

        public string ChannelControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_CreationTime");
                }
            }
        }

        public string ChannelControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Length");
                }
            }
        }

        public string ChannelControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Comment");
                }
            }
        }

        public string ChannelControl_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Id");
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

        public string ChannelControl_AddCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_AddCategory");
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

        public string ChannelControl_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Download");
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

        public string ChannelControl_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Export");
                }
            }
        }

        public string ChannelControl_DownloadHistoryDelete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_DownloadHistoryDelete");
                }
            }
        }

        public string ChannelControl_UploadHistoryDelete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_UploadHistoryDelete");
                }
            }
        }

        public string ChannelControl_FilterName
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_FilterName");
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

        public string ChannelControl_FilterKeyword
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_FilterKeyword");
                }
            }
        }

        public string ChannelControl_FilterSeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_FilterSeed");
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

        public string MessageEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MessageEditWindow_Signature");
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


        public string CategoryEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Title");
                }
            }
        }

        public string CategoryEditWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Name");
                }
            }
        }

        public string CategoryEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Cancel");
                }
            }
        }

        public string CategoryEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Ok");
                }
            }
        }

        public string CategoryEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Copy");
                }
            }
        }

        public string CategoryEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Paste");
                }
            }
        }

        public string CategoryEditWindow_Word
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Word");
                }
            }
        }

        public string CategoryEditWindow_Regex
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Regex");
                }
            }
        }

        public string CategoryEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Signature");
                }
            }
        }

        public string CategoryEditWindow_Contains
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Contains");
                }
            }
        }

        public string CategoryEditWindow_NotContains
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_NotContains");
                }
            }
        }

        public string CategoryEditWindow_Condition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Condition");
                }
            }
        }

        public string CategoryEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Up");
                }
            }
        }

        public string CategoryEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Down");
                }
            }
        }

        public string CategoryEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Add");
                }
            }
        }

        public string CategoryEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Edit");
                }
            }
        }

        public string CategoryEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Delete");
                }
            }
        }

        public string CategoryEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Value");
                }
            }
        }

        public string CategoryEditWindow_IsIgnoreCase
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_IsIgnoreCase");
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
