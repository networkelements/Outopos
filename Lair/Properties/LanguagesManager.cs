using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml;
using Library;

namespace Lair.Properties
{
    internal class LanguagesManager : IThisLock
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

        public static LanguagesManager Instance
        {
            get
            {
                return _defaultInstance;
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

        public string MainWindow_ServerAdd
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ServerAdd");
                }
            }
        }

        public string RouterWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Ok");
                }
            }
        }

        public string RouterWindow_Canrel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Canrel");
                }
            }
        }

        public string RouterWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Up");
                }
            }
        }

        public string RouterWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Down");
                }
            }
        }

        public string RouterWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Add");
                }
            }
        }

        public string RouterWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Edit");
                }
            }
        }

        public string RouterWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Delete");
                }
            }
        }

        public string RouterWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Name");
                }
            }
        }

        public string RouterWindow_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Uri");
                }
            }
        }

        public string RouterWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Signature");
                }
            }
        }

        public string RouterWindow_Port
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Port");
                }
            }
        }

        public string RouterWindow_Connection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Connection");
                }
            }
        }

        public string RouterWindow_ServerType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_ServerType");
                }
            }
        }

        public string RouterWindow_Ipv4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Ipv4");
                }
            }
        }

        public string RouterWindow_Ipv6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Ipv6");
                }
            }
        }

        public string RouterWindow_Tor
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Tor");
                }
            }
        }

        public string RouterWindow_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Type");
                }
            }
        }

        public string RouterWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Host");
                }
            }
        }

        public string RouterWindow_Server
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Server");
                }
            }
        }

        public string RouterWindow_ListenUris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_ListenUris");
                }
            }
        }

        public string RouterWindow_Miscellaneous
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_Miscellaneous");
                }
            }
        }

        public string RouterWindow_CoreSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_CoreSettings");
                }
            }
        }

        public string RouterWindow_ConnectionCountLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_ConnectionCountLimit");
                }
            }
        }

        public string RouterWindow_OptionSettings
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_OptionSettings");
                }
            }
        }

        public string RouterWindow_AutoStart
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("RouterWindow_AutoStart");
                }
            }
        }

        #endregion

        #region IThisLock メンバ

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion

        public static void ChangeLanguage(string language)
        {
            if (!_dic.ContainsKey(language))
            {
                throw new ArgumentException();
            }

            _usingLanguage = language;
            ResourceProvider.Refresh();
        }

        public static LanguagesManager GetInstance()
        {
            return _defaultInstance;
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

        private static void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

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
    }
}
