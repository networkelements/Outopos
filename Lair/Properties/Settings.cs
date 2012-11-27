using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Lair.Windows;
using Library;
using Library.Collections;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using a = Library.Net.Amoeba;
using Library.Net.Amoeba;

namespace Lair.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static Settings _defaultInstance = new Settings();
        private object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingsContext>()
            {
                new Library.Configuration.SettingsContext<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingsContext<DigitalSignature>() { Name = "Global_UploadDigitalSignature", Value = null },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_IsStart", Value = true },
                new Library.Configuration.SettingsContext<LockedHashSet<string>>() { Name = "Global_UrlHistorys", Value = new LockedHashSet<string>() },
                new Library.Configuration.SettingsContext<LockedHashSet<Seed>>() { Name = "Global_SeedHistorys", Value = new LockedHashSet<Seed>() },
                new Library.Configuration.SettingsContext<LockedHashSet<Channel>>() { Name = "Global_ChannelHistorys", Value = new LockedHashSet<Channel>() },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_UrlClearHistory_IsEnabled", Value = false },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Lair" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:28118" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Signature", Value = "Lyrise@iMK5aPkz6n_VLfaQWyXisi6C2yo53VbhMGTwJ4N2yGDTMXZwIdcZb8ayuGIOg-1V" },
                new Library.Configuration.SettingsContext<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Amoeba_Path", Value = "" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Fonts_MessageFontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingsContext<double>() { Name = "Global_Fonts_MessageFontSize", Value = 12 },
                new Library.Configuration.SettingsContext<LockedHashSet<a.Seed>>() { Name = "Global_Seeds", Value = new LockedHashSet<a.Seed>() },
                new Library.Configuration.SettingsContext<int>() { Name = "Global_SeedDelete_Expires", Value = 90 },

                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },

                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ConnectionsSettingsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_OtherNodes_Node_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 150 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width", Value = 160 },

                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ViewSettingsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Signature_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Grid_ColumnDefinitions_Width", Value = 160 },

                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "VersionInformationWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_GridViewColumn_FileName_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_GridViewColumn_Version_Width", Value = -1 },

                new Library.Configuration.SettingsContext<string>() { Name = "ConnectionControl_LastHeaderClicked", Value = "Uri" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "ConnectionControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_Grid_ColumnDefinitions_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Uri_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_ReceivedByteCount_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_SentByteCount_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Name_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Value_Width", Value = 100 },

                new Library.Configuration.SettingsContext<Windows.Category>() { Name = "ChannelControl_Category", Value = new Category() { Name = "Channel" } },
                new Library.Configuration.SettingsContext<string>() { Name = "ChannelControl_LastHeaderClicked", Value = "Uri" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "ChannelControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "ChannelControl_Grid_ColumnDefinitions_Width", Value = 200 },
              
                new Library.Configuration.SettingsContext<double>() { Name = "SignWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SignWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SignWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "SignWindow_WindowState", Value = WindowState.Normal },
              
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MessageEditWindow_WindowState", Value = WindowState.Normal },
              
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "BoardEditWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Word_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Word_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Regex_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Regex_Value_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Signature_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Signature_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Message_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoardEditWindow_GridViewColumn_Message_Value_Width", Value = 600 },
              
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "CategoryEditWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Word_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Word_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Regex_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Regex_Value_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Signature_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_GridViewColumn_Signature_Value_Width", Value = 600 },
              
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "NewChannelWindow_WindowState", Value = WindowState.Normal },
          
                new Library.Configuration.SettingsContext<Windows.SearchTreeItem>() { Name = "SearchControl_SearchTreeItem", Value = new Windows.SearchTreeItem() { SearchItem = new Windows.SearchItem() { Name = "Search" } } },
                new Library.Configuration.SettingsContext<string>() { Name = "SearchControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "SearchControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_Keywords_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_Comment_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchControl_GridViewColumn_Hash_Width", Value = 120 },
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

        public DigitalSignature Global_UploadDigitalSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (DigitalSignature)this["Global_UploadDigitalSignature"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UploadDigitalSignature"] = value;
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

        public LockedHashSet<Seed> Global_SeedHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (LockedHashSet<Seed>)this["Global_SeedHistorys"];
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

        public LockedHashSet<Channel> Global_ChannelHistorys
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (LockedHashSet<Channel>)this["Global_ChannelHistorys"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_ChannelHistorys"] = value;
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

        public LockedHashSet<a.Seed> Global_Seeds
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (LockedHashSet<a.Seed>)this["Global_Seeds"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Seeds"] = value;
                }
            }
        }

        public int Global_SeedDelete_Expires
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (int)this["Global_SeedDelete_Expires"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_SeedDelete_Expires"] = value;
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


        public double ConnectionsSettingsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Top"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Left"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Height"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Width"] = value;
                }
            }
        }

        public WindowState ConnectionsSettingsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ConnectionsSettingsWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_WindowState"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_OtherNodes_Node_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }


        public double ViewSettingsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Top"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Left"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Height"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Width"] = value;
                }
            }
        }

        public WindowState ViewSettingsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ViewSettingsWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_WindowState"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Signature_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Signature_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Grid_ColumnDefinitions_Width"] = value;
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


        public Windows.Category ChannelControl_Category
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (Windows.Category)this["ChannelControl_Category"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ChannelControl_Category"] = value;
                }
            }
        }

        public string ChannelControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["ChannelControl_LastHeaderClicked"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ChannelControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection ChannelControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["ChannelControl_ListSortDirection"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ChannelControl_ListSortDirection"] = value;
                }
            }
        }

        public double ChannelControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ChannelControl_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ChannelControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }


        public double SignWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SignWindow_Top"] = value;
                }
            }
        }

        public double SignWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SignWindow_Left"] = value;
                }
            }
        }

        public double SignWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SignWindow_Width"] = value;
                }
            }
        }

        public WindowState SignWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["SignWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SignWindow_WindowState"] = value;
                }
            }
        }


        public double MessageEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MessageEditWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MessageEditWindow_Top"] = value;
                }
            }
        }

        public double MessageEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MessageEditWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MessageEditWindow_Left"] = value;
                }
            }
        }

        public double MessageEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MessageEditWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MessageEditWindow_Height"] = value;
                }
            }
        }

        public double MessageEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["MessageEditWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MessageEditWindow_Width"] = value;
                }
            }
        }

        public WindowState MessageEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["MessageEditWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["MessageEditWindow_WindowState"] = value;
                }
            }
        }


        public double BoardEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_Top"] = value;
                }
            }
        }

        public double BoardEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_Left"] = value;
                }
            }
        }

        public double BoardEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_Height"] = value;
                }
            }
        }

        public double BoardEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_Width"] = value;
                }
            }
        }

        public WindowState BoardEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["BoardEditWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_WindowState"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Word_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Word_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Word_Contains_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Word_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Word_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Word_Value_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Regex_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Regex_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Regex_Contains_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Regex_Value_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Regex_Value_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Regex_Value_Value_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Signature_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Signature_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Signature_Contains_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Signature_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Signature_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Signature_Value_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Message_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Message_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Message_Contains_Width"] = value;
                }
            }
        }

        public double BoardEditWindow_GridViewColumn_Message_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["BoardEditWindow_GridViewColumn_Message_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoardEditWindow_GridViewColumn_Message_Value_Width"] = value;
                }
            }
        }


        public double CategoryEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_Top"] = value;
                }
            }
        }

        public double CategoryEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_Left"] = value;
                }
            }
        }

        public double CategoryEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_Height"] = value;
                }
            }
        }

        public double CategoryEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_Width"] = value;
                }
            }
        }

        public WindowState CategoryEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["CategoryEditWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_WindowState"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Word_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Word_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Word_Contains_Width"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Word_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Word_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Word_Value_Width"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Regex_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Regex_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Regex_Contains_Width"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Regex_Value_IsIgnoreCase_Width"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Regex_Value_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Regex_Value_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Regex_Value_Value_Width"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Signature_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Signature_Contains_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Signature_Contains_Width"] = value;
                }
            }
        }

        public double CategoryEditWindow_GridViewColumn_Signature_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CategoryEditWindow_GridViewColumn_Signature_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CategoryEditWindow_GridViewColumn_Signature_Value_Width"] = value;
                }
            }
        }


        public double NewChannelWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["NewChannelWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["NewChannelWindow_Top"] = value;
                }
            }
        }

        public double NewChannelWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["NewChannelWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["NewChannelWindow_Left"] = value;
                }
            }
        }

        public double NewChannelWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["NewChannelWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["NewChannelWindow_Width"] = value;
                }
            }
        }

        public WindowState NewChannelWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["NewChannelWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["NewChannelWindow_WindowState"] = value;
                }
            }
        }


        public Windows.SearchTreeItem SearchControl_SearchTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (Windows.SearchTreeItem)this["SearchControl_SearchTreeItem"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_SearchTreeItem"] = value;
                }
            }
        }

        public string SearchControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["SearchControl_LastHeaderClicked"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection SearchControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["SearchControl_ListSortDirection"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_ListSortDirection"] = value;
                }
            }
        }

        public double SearchControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_Name_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Signature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_Signature_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Signature_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_State_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_State_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_State_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Keywords_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_Keywords_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Keywords_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_CreationTime_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_CreationTime_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_CreationTime_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_Length_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Length_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Comment_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_Comment_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Comment_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Hash_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchControl_GridViewColumn_Hash_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Hash_Width"] = value;
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
