using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library;
using System.Windows;
using Library.Net;
using Lair.Windows;
using Library.Security;
using Library.Net.Lair;
using System.ComponentModel;
using System.Windows.Media;

namespace Lair.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static Settings _defaultInstance = new Settings();
        private object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingsContext>()
            {
                new Library.Configuration.SettingsContext<List<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new List<DigitalSignature>() },
                new Library.Configuration.SettingsContext<DigitalSignature>() { Name = "Global_UploadDigitalSignature", Value = null },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_IsStart", Value = true },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Lair" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:8118" },
                new Library.Configuration.SettingsContext<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Amoeba_Path", Value = "" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Fonts_MessageFontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingsContext<double>() { Name = "Global_Fonts_MessageFontSize", Value = 12 },

                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },

                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ConnectionWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_BaseNode_Uris_Uri_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_OtherNodes_Node_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 300 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 600 },

                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "UserInterfaceWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Signature_GridViewColumn_Value_Width", Value = 600 },

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
                new Library.Configuration.SettingsContext<double>() { Name = "SignWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "SignWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "SignWindow_WindowState", Value = WindowState.Normal },
              
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MessageEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MessageEditWindow_WindowState", Value = WindowState.Normal },
              
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "CategoryEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "CategoryEditWindow_WindowState", Value = WindowState.Normal },
              
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "NewChannelWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "NewChannelWindow_WindowState", Value = WindowState.Normal },
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

        public List<DigitalSignature> Global_DigitalSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (List<DigitalSignature>)this["Global_DigitalSignatureCollection"];
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


        public double ConnectionWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Top"] = value;
                }
            }
        }

        public double ConnectionWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Left"] = value;
                }
            }
        }

        public double ConnectionWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Height"] = value;
                }
            }
        }

        public double ConnectionWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Width"] = value;
                }
            }
        }

        public WindowState ConnectionWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ConnectionWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_WindowState"] = value;
                }
            }
        }

        public double ConnectionWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_BaseNode_Uris_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_OtherNodes_Node_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }


        public double UserInterfaceWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Top"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Left"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Height"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Width"] = value;
                }
            }
        }

        public WindowState UserInterfaceWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["UserInterfaceWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_WindowState"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Signature_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Signature_GridViewColumn_Value_Width"] = value;
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

        public double SignWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SignWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SignWindow_Height"] = value;
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

        public double NewChannelWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["NewChannelWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["NewChannelWindow_Height"] = value;
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
