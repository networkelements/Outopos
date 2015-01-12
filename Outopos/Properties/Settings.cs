using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Library;
using Library.Collections;
using Library.Net;
using Library.Net.Outopos;
using Library.Security;
using Outopos.Windows;
using A = Library.Net.Amoeba;

namespace Outopos.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static Settings _defaultInstance = new Settings();
        private readonly object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingContent>()
            {
                new Library.Configuration.SettingContent<ProfileItem>() { Name = "Global_ProfileItem", Value = null },
                new Library.Configuration.SettingContent<LockedHashDictionary<string, Profile>>() { Name = "Global_Profiles", Value = new LockedHashDictionary<string, Profile>() },
                new Library.Configuration.SettingContent<int>() { Name = "Global_Limit", Value = 0 },
                new Library.Configuration.SettingContent<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingContent<LockedHashSet<string>>() { Name = "Global_UrlHistorys", Value = new LockedHashSet<string>() },
                new Library.Configuration.SettingContent<LockedHashSet<Wiki>>() { Name = "Global_WikiHistorys", Value = new LockedHashSet<Wiki>() },
                new Library.Configuration.SettingContent<LockedHashSet<Chat>>() { Name = "Global_ChatHistorys", Value = new LockedHashSet<Chat>() },
                new Library.Configuration.SettingContent<LockedHashSet<A.Seed>>() { Name = "Global_SeedHistorys", Value = new LockedHashSet<A.Seed>() },
                new Library.Configuration.SettingContent<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_I2p_SamBridge_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Outopos" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:28118" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Signature", Value = "Lyrise@OTAhpWvmegu50LT-p5dZ16og7U6bdpO4z5TInZxGsCs" },
                new Library.Configuration.SettingContent<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Amoeba_Path", Value = "" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Fonts_MessageFontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingContent<double>() { Name = "Global_Fonts_MessageFontSize", Value = 12 },

                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },
                
                new Library.Configuration.SettingContent<double>() { Name = "TrustControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustControl_GridViewColumn_TrustSignature_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustControl_GridViewColumn_Wiki_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustControl_GridViewColumn_Chat_Width", Value = 600 },

                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ProfileWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_GridViewColumn_Signature_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_GridViewColumn_Wiki_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "ProfileWindow_GridViewColumn_Chat_Width", Value = 600 },

                new Library.Configuration.SettingContent<double>() { Name = "ChatControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<ChatCategorizeTreeItem>() { Name = "ChatControl_ChatCategorizeTreeItem", Value = new ChatCategorizeTreeItem(){ Name = "Category", IsExpanded = true } },
          
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ChatMessageEditWindow_WindowState", Value = WindowState.Normal },
            
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ChatListWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<string>() { Name = "ChatListWindow_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "ChatListWindow_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_GridViewColumn_Id_Width", Value = 120 },

                new Library.Configuration.SettingContent<double>() { Name = "MailControl_Grid_ColumnDefinitions_Width", Value = 200 },
             
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "OptionsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_BaseNode_Uris_Uri_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_OtherNodes_Node_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_Option_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Grid_ColumnDefinitions_Width", Value = 160 },
                new Library.Configuration.SettingContent<string>() { Name = "OptionsWindow_BandwidthLimit_Unit", Value = "KB" },
                new Library.Configuration.SettingContent<string>() { Name = "OptionsWindow_TransferLimit_Unit", Value = "GB" },
                new Library.Configuration.SettingContent<string>() { Name = "OptionsWindow_DataCacheSize_Unit", Value = "GB" },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Signature_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Keyword_GridViewColumn_Value_Width", Value = 400 },
            })
        {

        }

        public override void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                try
                {
                    base.Load(directoryPath);
                }
                catch (Exception)
                {

                }
            }
        }

        public override void Save(string directoryPath)
        {
            lock (this.ThisLock)
            {
                base.Save(directoryPath);
            }
        }

        public static Settings Instance
        {
            get
            {
                return _defaultInstance;
            }
        }

        #region Property

        public ProfileItem Global_ProfileItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ProfileItem)this["Global_ProfileItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_ProfileItem"] = value;
                }
            }
        }

        public LockedHashDictionary<string, Profile> Global_Profiles
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashDictionary<string, Profile>)this["Global_Profiles"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Profiles"] = value;
                }
            }
        }

        public int Global_Limit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (int)this["Global_Limit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Limit"] = value;
                }
            }
        }

        public LockedList<DigitalSignature> Global_DigitalSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedList<DigitalSignature>)this["Global_DigitalSignatureCollection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_DigitalSignatureCollection"] = value;
                }
            }
        }

        public LockedHashSet<string> Global_UrlHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<string>)this["Global_UrlHistorys"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UrlHistorys"] = value;
                }
            }
        }

        public LockedHashSet<Wiki> Global_WikiHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<Wiki>)this["Global_WikiHistorys"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_WikiHistorys"] = value;
                }
            }
        }

        public LockedHashSet<Chat> Global_ChatHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<Chat>)this["Global_ChatHistorys"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_ChatHistorys"] = value;
                }
            }
        }

        public LockedHashSet<A.Seed> Global_SeedHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<A.Seed>)this["Global_SeedHistorys"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_SeedHistorys"] = value;
                }
            }
        }

        public string Global_UseLanguage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_UseLanguage"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UseLanguage"] = value;
                }
            }
        }

        public bool Global_AutoBaseNodeSetting_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_AutoBaseNodeSetting_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_AutoBaseNodeSetting_IsEnabled"] = value;
                }
            }
        }

        public bool Global_I2p_SamBridge_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_I2p_SamBridge_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_I2p_SamBridge_IsEnabled"] = value;
                }
            }
        }

        public string Global_Update_Url
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Update_Url"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_Url"] = value;
                }
            }
        }

        public string Global_Update_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Update_ProxyUri"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_ProxyUri"] = value;
                }
            }
        }

        public string Global_Update_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Update_Signature"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_Signature"] = value;
                }
            }
        }

        public UpdateOption Global_Update_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (UpdateOption)this["Global_Update_Option"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_Option"] = value;
                }
            }
        }

        public string Global_Amoeba_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Amoeba_Path"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Amoeba_Path"] = value;
                }
            }
        }

        public string Global_Fonts_MessageFontFamily
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Fonts_MessageFontFamily"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Fonts_MessageFontFamily"] = value;
                }
            }
        }

        public double Global_Fonts_MessageFontSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["Global_Fonts_MessageFontSize"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Fonts_MessageFontSize"] = value;
                }
            }
        }


        public double MainWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Top"] = value;
                }
            }
        }

        public double MainWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Left"] = value;
                }
            }
        }

        public double MainWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Height"] = value;
                }
            }
        }

        public double MainWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Width"] = value;
                }
            }
        }

        public WindowState MainWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["MainWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_WindowState"] = value;
                }
            }
        }


        public double TrustControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["TrustControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double TrustControl_GridViewColumn_TrustSignature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["TrustControl_GridViewColumn_TrustSignature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustControl_GridViewColumn_TrustSignature_Width"] = value;
                }
            }
        }

        public double TrustControl_GridViewColumn_Wiki_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["TrustControl_GridViewColumn_Wiki_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustControl_GridViewColumn_Wiki_Width"] = value;
                }
            }
        }

        public double TrustControl_GridViewColumn_Chat_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["TrustControl_GridViewColumn_Chat_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustControl_GridViewColumn_Chat_Width"] = value;
                }
            }
        }


        public double ProfileWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_Top"] = value;
                }
            }
        }

        public double ProfileWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_Left"] = value;
                }
            }
        }

        public double ProfileWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_Height"] = value;
                }
            }
        }

        public double ProfileWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_Width"] = value;
                }
            }
        }

        public WindowState ProfileWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["ProfileWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_WindowState"] = value;
                }
            }
        }

        public double ProfileWindow_GridViewColumn_Signature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_GridViewColumn_Signature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_GridViewColumn_Signature_Width"] = value;
                }
            }
        }

        public double ProfileWindow_GridViewColumn_Wiki_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_GridViewColumn_Wiki_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_GridViewColumn_Wiki_Width"] = value;
                }
            }
        }

        public double ProfileWindow_GridViewColumn_Chat_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProfileWindow_GridViewColumn_Chat_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProfileWindow_GridViewColumn_Chat_Width"] = value;
                }
            }
        }


        public double ChatControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public ChatCategorizeTreeItem ChatControl_ChatCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ChatCategorizeTreeItem)this["ChatControl_ChatCategorizeTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatControl_ChatCategorizeTreeItem"] = value;
                }
            }
        }


        public double ChatMessageEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatMessageEditWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatMessageEditWindow_Top"] = value;
                }
            }
        }

        public double ChatMessageEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatMessageEditWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatMessageEditWindow_Left"] = value;
                }
            }
        }

        public double ChatMessageEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatMessageEditWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatMessageEditWindow_Height"] = value;
                }
            }
        }

        public double ChatMessageEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatMessageEditWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatMessageEditWindow_Width"] = value;
                }
            }
        }

        public WindowState ChatMessageEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["ChatMessageEditWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatMessageEditWindow_WindowState"] = value;
                }
            }
        }


        public double ChatListWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatListWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_Top"] = value;
                }
            }
        }

        public double ChatListWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatListWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_Left"] = value;
                }
            }
        }

        public double ChatListWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatListWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_Height"] = value;
                }
            }
        }

        public double ChatListWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatListWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_Width"] = value;
                }
            }
        }

        public WindowState ChatListWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["ChatListWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_WindowState"] = value;
                }
            }
        }

        public string ChatListWindow_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["ChatListWindow_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection ChatListWindow_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["ChatListWindow_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_ListSortDirection"] = value;
                }
            }
        }

        public double ChatListWindow_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatListWindow_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double ChatListWindow_GridViewColumn_Id_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ChatListWindow_GridViewColumn_Id_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ChatListWindow_GridViewColumn_Id_Width"] = value;
                }
            }
        }


        public double MailControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MailControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MailControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }


        public double OptionsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Top"] = value;
                }
            }
        }

        public double OptionsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Left"] = value;
                }
            }
        }

        public double OptionsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Height"] = value;
                }
            }
        }

        public double OptionsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Width"] = value;
                }
            }
        }

        public WindowState OptionsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["OptionsWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_WindowState"] = value;
                }
            }
        }

        public double OptionsWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_BaseNode_Uris_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double OptionsWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_OtherNodes_Node_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_Option_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_Option_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_Option_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public string OptionsWindow_BandwidthLimit_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["OptionsWindow_BandwidthLimit_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_BandwidthLimit_Unit"] = value;
                }
            }
        }

        public string OptionsWindow_TransferLimit_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["OptionsWindow_TransferLimit_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_TransferLimit_Unit"] = value;
                }
            }
        }

        public string OptionsWindow_DataCacheSize_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["OptionsWindow_DataCacheSize_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_DataCacheSize_Unit"] = value;
                }
            }
        }

        public double OptionsWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Signature_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Signature_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Keyword_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Keyword_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Keyword_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        #endregion

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }
    }
}
