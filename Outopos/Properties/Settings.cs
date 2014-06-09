using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Outopos.Windows;
using Library;
using Library.Collections;
using Library.Net;
using Library.Net.Outopos;
using Library.Security;
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
                new Library.Configuration.SettingContent<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingContent<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsStart", Value = true },
                new Library.Configuration.SettingContent<LockedHashSet<string>>() { Name = "Global_UrlHistorys", Value = new LockedHashSet<string>() },
                new Library.Configuration.SettingContent<LockedHashSet<Section>>() { Name = "Global_SectionHistorys", Value = new LockedHashSet<Section>() },
                new Library.Configuration.SettingContent<LockedHashSet<Wiki>>() { Name = "Global_WikiHistorys", Value = new LockedHashSet<Wiki>() },
                new Library.Configuration.SettingContent<LockedHashSet<Chat>>() { Name = "Global_ChatHistorys", Value = new LockedHashSet<Chat>() },
                new Library.Configuration.SettingContent<LockedHashSet<A.Seed>>() { Name = "Global_SeedHistorys", Value = new LockedHashSet<A.Seed>() },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_UrlClearHistory_IsEnabled", Value = false },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_I2p_SamBridge_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Outopos" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:28118" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Signature", Value = "Lyrise@7seiSbhOCkls6gPxjJYjptxskzlSulgIe3dSfj1KxnJJ6eejKjuJ3R1Ec8yFuKpr4uNcwF7bFh5OrmxnY25y7A" },
                new Library.Configuration.SettingContent<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Amoeba_Path", Value = "" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Fonts_MessageFontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingContent<double>() { Name = "Global_Fonts_MessageFontSize", Value = 12 },

                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },

                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "CoreOptionsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_BaseNode_Uris_Uri_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_OtherNodes_Node_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = -1 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Client_Filters_GridViewColumn_Option_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "CoreOptionsWindow_Grid_ColumnDefinitions_Width", Value = 160 },
                new Library.Configuration.SettingContent<string>() { Name = "CoreOptionsWindow_BandwidthLimit_Unit", Value = "Byte" },
                new Library.Configuration.SettingContent<string>() { Name = "CoreOptionsWindow_DataCacheSize_Unit", Value = "GB" },

                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ViewOptionsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Signature_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Keyword_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "ViewOptionsWindow_Grid_ColumnDefinitions_Width", Value = 160 },

                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "VersionInformationWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_GridViewColumn_FileName_Width", Value = -1 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_GridViewColumn_Version_Width", Value = -1 },

                new Library.Configuration.SettingContent<string>() { Name = "ConnectionControl_LastHeaderClicked", Value = "Uri" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "ConnectionControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_Grid_ColumnDefinitions_Width", Value = -1 },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_GridViewColumn_Uri_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_GridViewColumn_ReceivedByteCount_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_GridViewColumn_SentByteCount_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_GridViewColumn_Name_Width", Value = -1 },
                new Library.Configuration.SettingContent<double>() { Name = "ConnectionControl_GridViewColumn_Value_Width", Value = 100 },
   
                new Library.Configuration.SettingContent<double>() { Name = "SectionControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<SectionCategorizeTreeItem>() { Name = "SectionControl_SectionCategorizeTreeItem", Value = new SectionCategorizeTreeItem(){ Name = "Category", IsExpanded = true } },

                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SectionTreeItemEditWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_GridViewColumn_TrustSignature_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_GridViewColumn_Wiki_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionTreeItemEditWindow_GridViewColumn_Chat_Width", Value = 600 },

                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "TrustSignaturesPreview_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_GridViewColumn_TrustSignature_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_GridViewColumn_Wiki_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "TrustSignaturesPreview_GridViewColumn_Chat_Width", Value = 600 },

                new Library.Configuration.SettingContent<double>() { Name = "ChatControl_Grid_ColumnDefinitions_Width", Value = 200 },
             
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ChatListWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<string>() { Name = "ChatListWindow_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "ChatListWindow_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatListWindow_GridViewColumn_Id_Width", Value = 120 },
          
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "ChatMessageEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ChatMessageEditWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingContent<double>() { Name = "MailControl_Grid_ColumnDefinitions_Width", Value = 200 },
             
                new Library.Configuration.SettingContent<double>() { Name = "SignatureListWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureListWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureListWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureListWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SignatureListWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<string>() { Name = "SignatureListWindow_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "SignatureListWindow_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureListWindow_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureListWindow_GridViewColumn_Id_Width", Value = 120 },
          
                new Library.Configuration.SettingContent<double>() { Name = "SectionMessageEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionMessageEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionMessageEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SectionMessageEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SectionMessageEditWindow_WindowState", Value = WindowState.Normal },
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

        public bool Global_IsStart
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (bool)this["Global_IsStart"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_IsStart"] = value;
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

        public LockedHashSet<Section> Global_SectionHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (LockedHashSet<Section>)this["Global_SectionHistorys"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_SectionHistorys"] = value;
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

        public bool Global_UrlClearHistory_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (bool)this["Global_UrlClearHistory_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UrlClearHistory_IsEnabled"] = value;
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


        public double CoreOptionsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Top"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Left"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Height"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Width"] = value;
                }
            }
        }

        public WindowState CoreOptionsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["CoreOptionsWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_WindowState"] = value;
                }
            }
        }

        public double CoreOptionsWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_BaseNode_Uris_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_OtherNodes_Node_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Client_Filters_GridViewColumn_Option_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Client_Filters_GridViewColumn_Option_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Client_Filters_GridViewColumn_Option_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double CoreOptionsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CoreOptionsWindow_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public string CoreOptionsWindow_BandwidthLimit_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["CoreOptionsWindow_BandwidthLimit_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_BandwidthLimit_Unit"] = value;
                }
            }
        }

        public string CoreOptionsWindow_DataCacheSize_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["CoreOptionsWindow_DataCacheSize_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["CoreOptionsWindow_DataCacheSize_Unit"] = value;
                }
            }
        }


        public double ViewOptionsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Top"] = value;
                }
            }
        }

        public double ViewOptionsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Left"] = value;
                }
            }
        }

        public double ViewOptionsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Height"] = value;
                }
            }
        }

        public double ViewOptionsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Width"] = value;
                }
            }
        }

        public WindowState ViewOptionsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ViewOptionsWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_WindowState"] = value;
                }
            }
        }

        public double ViewOptionsWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Signature_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Signature_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double ViewOptionsWindow_Keyword_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Keyword_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Keyword_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double ViewOptionsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewOptionsWindow_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ViewOptionsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }


        public double VersionInformationWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["VersionInformationWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Top"] = value;
                }
            }
        }

        public double VersionInformationWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["VersionInformationWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Left"] = value;
                }
            }
        }

        public double VersionInformationWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["VersionInformationWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Height"] = value;
                }
            }
        }

        public double VersionInformationWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["VersionInformationWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Width"] = value;
                }
            }
        }

        public WindowState VersionInformationWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["VersionInformationWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_WindowState"] = value;
                }
            }
        }

        public double VersionInformationWindow_GridViewColumn_FileName_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["VersionInformationWindow_GridViewColumn_FileName_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_GridViewColumn_FileName_Width"] = value;
                }
            }
        }

        public double VersionInformationWindow_GridViewColumn_Version_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["VersionInformationWindow_GridViewColumn_Version_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_GridViewColumn_Version_Width"] = value;
                }
            }
        }


        public string ConnectionControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["ConnectionControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection ConnectionControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["ConnectionControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_ListSortDirection"] = value;
                }
            }
        }

        public double ConnectionControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Priority_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Priority_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Priority_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_ReceivedByteCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_ReceivedByteCount_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_ReceivedByteCount_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_SentByteCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_SentByteCount_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_SentByteCount_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Value_Width"] = value;
                }
            }
        }


        public double SectionControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public SectionCategorizeTreeItem SectionControl_SectionCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (SectionCategorizeTreeItem)this["SectionControl_SectionCategorizeTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionControl_SectionCategorizeTreeItem"] = value;
                }
            }
        }


        public double SectionTreeItemEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_Top"] = value;
                }
            }
        }

        public double SectionTreeItemEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_Left"] = value;
                }
            }
        }

        public double SectionTreeItemEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_Height"] = value;
                }
            }
        }

        public double SectionTreeItemEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_Width"] = value;
                }
            }
        }

        public WindowState SectionTreeItemEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["SectionTreeItemEditWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_WindowState"] = value;
                }
            }
        }

        public double SectionTreeItemEditWindow_GridViewColumn_TrustSignature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_GridViewColumn_TrustSignature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_GridViewColumn_TrustSignature_Width"] = value;
                }
            }
        }

        public double SectionTreeItemEditWindow_GridViewColumn_Wiki_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_GridViewColumn_Wiki_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_GridViewColumn_Wiki_Width"] = value;
                }
            }
        }

        public double SectionTreeItemEditWindow_GridViewColumn_Chat_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionTreeItemEditWindow_GridViewColumn_Chat_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionTreeItemEditWindow_GridViewColumn_Chat_Width"] = value;
                }
            }
        }


        public double TrustSignaturesPreview_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_Top"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_Left"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_Height"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_Width"] = value;
                }
            }
        }

        public WindowState TrustSignaturesPreview_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["TrustSignaturesPreview_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_WindowState"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_GridViewColumn_TrustSignature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_GridViewColumn_TrustSignature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_GridViewColumn_TrustSignature_Width"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_GridViewColumn_Wiki_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_GridViewColumn_Wiki_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_GridViewColumn_Wiki_Width"] = value;
                }
            }
        }

        public double TrustSignaturesPreview_GridViewColumn_Chat_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["TrustSignaturesPreview_GridViewColumn_Chat_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["TrustSignaturesPreview_GridViewColumn_Chat_Width"] = value;
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


        public double SignatureListWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignatureListWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_Top"] = value;
                }
            }
        }

        public double SignatureListWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignatureListWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_Left"] = value;
                }
            }
        }

        public double SignatureListWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignatureListWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_Height"] = value;
                }
            }
        }

        public double SignatureListWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignatureListWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_Width"] = value;
                }
            }
        }

        public WindowState SignatureListWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["SignatureListWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_WindowState"] = value;
                }
            }
        }

        public string SignatureListWindow_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["SignatureListWindow_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection SignatureListWindow_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["SignatureListWindow_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_ListSortDirection"] = value;
                }
            }
        }

        public double SignatureListWindow_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignatureListWindow_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double SignatureListWindow_GridViewColumn_Id_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignatureListWindow_GridViewColumn_Id_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureListWindow_GridViewColumn_Id_Width"] = value;
                }
            }
        }


        public double SectionMessageEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionMessageEditWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionMessageEditWindow_Top"] = value;
                }
            }
        }

        public double SectionMessageEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionMessageEditWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionMessageEditWindow_Left"] = value;
                }
            }
        }

        public double SectionMessageEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionMessageEditWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionMessageEditWindow_Height"] = value;
                }
            }
        }

        public double SectionMessageEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SectionMessageEditWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionMessageEditWindow_Width"] = value;
                }
            }
        }

        public WindowState SectionMessageEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["SectionMessageEditWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SectionMessageEditWindow_WindowState"] = value;
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
