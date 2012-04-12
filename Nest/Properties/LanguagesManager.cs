using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml;
using Library;

namespace Nest.Properties
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
            string path = @"C:\Public\Project\Nest\Nest\bin\Debug\Core\Languages";
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

        public string ServerWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ServerWindow_Ok");
                }
            }
        }

        public string ServerWindow_Canrel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ServerWindow_Canrel");
                }
            }
        }

        public string ServerWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ServerWindow_Name");
                }
            }
        }

        public string ServerWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ServerWindow_Host");
                }
            }
        }

        public string ServerWindow_Port
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ServerWindow_Port");
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
