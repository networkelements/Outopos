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

namespace Lair.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static Settings _defaultInstance = new Settings();
        private readonly object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingsContext>()
            {
                new Library.Configuration.SettingsContext<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_IsStart", Value = true },
                new Library.Configuration.SettingsContext<LockedHashSet<string>>() { Name = "Global_UrlHistorys", Value = new LockedHashSet<string>() },
                new Library.Configuration.SettingsContext<LockedHashSet<a.Seed>>() { Name = "Global_SeedHistorys", Value = new LockedHashSet<a.Seed>() },
                new Library.Configuration.SettingsContext<LockedHashSet<Section>>() { Name = "Global_SectionHistorys", Value = new LockedHashSet<Section>() },
                new Library.Configuration.SettingsContext<LockedHashSet<Chat>>() { Name = "Global_ChatHistorys", Value = new LockedHashSet<Chat>() },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_UrlClearHistory_IsEnabled", Value = false },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Lair" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:28118" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Signature", Value = "Lyrise@7seiSbhOCkls6gPxjJYjptxskzlSulgIe3dSfj1KxnJJ6eejKjuJ3R1Ec8yFuKpr4uNcwF7bFh5OrmxnY25y7A" },
                new Library.Configuration.SettingsContext<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Amoeba_Path", Value = "" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Fonts_MessageFontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingsContext<double>() { Name = "Global_Fonts_MessageFontSize", Value = 12 },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Fonts_DocumentFontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingsContext<double>() { Name = "Global_Fonts_DocumentFontSize", Value = 12 },

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
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_Option_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width", Value = 160 },
                new Library.Configuration.SettingsContext<string>() { Name = "ConnectionsSettingsWindow_BandwidthLimit_Unit", Value = "Byte" },
                new Library.Configuration.SettingsContext<string>() { Name = "ConnectionsSettingsWindow_DataCacheSize_Unit", Value = "GB" },

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

                new Library.Configuration.SettingsContext<SectionCategorizeTreeItem>() { Name = "SectionControl_SectionCategorizeTreeItem", Value = new SectionCategorizeTreeItem() },
                new Library.Configuration.SettingsContext<double>() { Name = "SectionControl_Grid_ColumnDefinitions_Width", Value = 200 },

                new Library.Configuration.SettingsContext<double>() { Name = "ChatControl_Grid_ColumnDefinitions_Width", Value = 200 },
                
                new Library.Configuration.SettingsContext<double>() { Name = "DocumentControl_Grid_ColumnDefinitions_Width", Value = 200 },

                new Library.Configuration.SettingsContext<double>() { Name = "MailMessageControl_Grid_ColumnDefinitions_Width", Value = 200 },
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
