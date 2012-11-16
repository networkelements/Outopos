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


        public string Box_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_Name");
                }
            }
        }

        public string Box_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_Signature");
                }
            }
        }

        public string Box_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_CreationTime");
                }
            }
        }

        public string Box_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_Comment");
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

        public string UserInterfaceWindow_Amoeba
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Amoeba");
                }
            }
        }

        public string UserInterfaceWindow_AmoebaPath
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_AmoebaPath");
                }
            }
        }

        public string UserInterfaceWindow_Fonts
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Fonts");
                }
            }
        }

        public string UserInterfaceWindow_MessageFontFamily
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_MessageFontFamily");
                }
            }
        }

        public string UserInterfaceWindow_MessageFontSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_MessageFontSize");
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

        public string UserInterfaceWindow_Event
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_Event");
                }
            }
        }

        public string UserInterfaceWindow_ClearUrlHistory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UserInterfaceWindow_ClearUrlHistory");
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

        public string ConnectionWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionWindow_Cut");
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

        public string ChannelControl_Signature_Filter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_Signature_Filter");
                }
            }
        }

        public string ChannelControl_New_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ChannelControl_New_Message");
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


        public string SignWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignWindow_Title");
                }
            }
        }

        public string SignWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignWindow_Signature");
                }
            }
        }

        public string SignWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignWindow_Ok");
                }
            }
        }

        public string SignWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignWindow_Cancel");
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

        public string CategoryEditWindow_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Message");
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

        public string CategoryEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("CategoryEditWindow_Cut");
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
