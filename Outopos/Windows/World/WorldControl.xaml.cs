using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Outopos;
using Outopos.Properties;
using Library;
using Library.Collections;
using Library.Net.Outopos;
using Library.Security;
using A = Library.Net.Amoeba;
using System.Windows.Documents;

namespace Outopos.Windows
{
    /// <summary>
    /// Interaction logic for WorldControl.xaml
    /// </summary>
    partial class WorldControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private OutoposManager _outoposManager;
        private BufferManager _bufferManager;

        private static Random _random = new Random();

        private Thread _watchThread;

        public WorldControl(OutoposManager outoposManager, BufferManager bufferManager)
        {
            _outoposManager = outoposManager;
            _bufferManager = bufferManager;

            InitializeComponent();

            _watchThread = new Thread(this.WatchThread);
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "WorldControl_WatchThread";
            _watchThread.Start();

            this.Check_Cost();

            this.Update();
        }

        private void WatchThread()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();

                for (; ; )
                {
                    Thread.Sleep(1000);

                    if (!stopwatch.IsRunning || stopwatch.Elapsed.TotalSeconds >= 120)
                    {
                        stopwatch.Restart();

                        this.Refresh_Profiles();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Refresh_Profiles()
        {
            var higherProfiles = new HashSet<Profile>();
            var generalProfiles = new HashSet<Profile>();

            foreach (var leaderSignature in Settings.Instance.Global_TrustSignatures.ToArray())
            {
                var targetProfiles = new List<Profile>();

                var targetSignatures = new HashSet<string>();
                var checkedSignatures = new HashSet<string>();

                targetSignatures.Add(leaderSignature);

                for (int i = 0; i < 32; i++)
                {
                    var profiles = this.GetProfiles(targetSignatures).ToList();
                    if (profiles.Count == 0) break;

                    checkedSignatures.UnionWith(profiles.SelectMany(n => n.DeleteSignatures));

                    targetSignatures.Clear();
                    targetSignatures.UnionWith(profiles.SelectMany(n => n.TrustSignatures).Where(n => !checkedSignatures.Contains(n)));

                    targetProfiles.AddRange(profiles);

                    if (targetProfiles.Count > 32 * 1024) goto End;
                }

            End: ;

                higherProfiles.UnionWith(targetProfiles.Take(32));
                generalProfiles.UnionWith(targetProfiles.Take(32 * 1024));
            }

            var trustSignatures = new SignatureCollection(generalProfiles.Select(n => n.Certificate.ToString()));

            lock (Settings.Instance.ThisLock)
            {
                lock (Settings.Instance.Global_Profiles.ThisLock)
                {
                    Settings.Instance.Global_Profiles.Clear();

                    foreach (var profile in generalProfiles)
                    {
                        Settings.Instance.Global_Profiles.Add(profile.Certificate.ToString(), profile);
                    }
                }
            }

            try
            {
                if (higherProfiles.Count > 0)
                {
                    int sum = 0;
                    int count = 0;

                    foreach (var profile in higherProfiles)
                    {
                        if (profile.Cost <= 0) continue;

                        sum += profile.Cost;
                        count++;
                    }

                    Trust.SetLimit(sum / count);
                }
            }
            catch (Exception)
            {

            }

            _outoposManager.SetTrustSignatures(trustSignatures);

            Trust.SetSignatures(trustSignatures);
        }

        private IEnumerable<Profile> GetProfiles(IEnumerable<string> trustSignatures)
        {
            var profiles = new List<Profile>();

            foreach (var trustSignature in trustSignatures)
            {
                var profile = _outoposManager.GetProfile(trustSignature);
                if (profile == null && !Settings.Instance.Global_Profiles.TryGetValue(trustSignature, out profile)) continue;

                profiles.Add(profile);
            }

            return profiles;
        }

        private void Check_Cost()
        {
            var profileItem = Settings.Instance.Global_ProfileItem;

            if (profileItem.Cost == 0)
            {
                ThreadPool.QueueUserWorkItem((object wstate) =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    try
                    {
                        profileItem.Cost = Miner.Sample(new TimeSpan(0, 3, 0));
                    }
                    catch (Exception)
                    {

                    }
                });
            }
        }

        public void Update()
        {
            _treeView.Items.Clear();

            _trustSignatureListView.Items.Clear();
            _wikiListView.Items.Clear();
            _chatListView.Items.Clear();

            foreach (var leaderSignature in Settings.Instance.Global_TrustSignatures)
            {
                var item = this.GetSignatureTreeViewItem(leaderSignature);
                if (item == null) continue;

                _treeView.Items.Add(new SignatureTreeViewItem(item));
            }
        }

        private SignatureTreeItem GetSignatureTreeViewItem(string leaderSignature)
        {
            List<SignatureTreeItem> signatureTreeItems = new List<SignatureTreeItem>();
            List<SignatureTreeItem> workSignatureTreeItems = new List<SignatureTreeItem>();

            HashSet<string> checkedSignatures = new HashSet<string>();
            HashSet<string> workCheckedSignatures = new HashSet<string>();

            {
                Profile leaderProfile;
                if (!Settings.Instance.Global_Profiles.TryGetValue(leaderSignature, out leaderProfile)) return null;

                signatureTreeItems.Add(new SignatureTreeItem(leaderProfile));
                checkedSignatures.Add(leaderSignature);
            }

            {
                int index = 0;

                for (; ; )
                {
                    for (; index < signatureTreeItems.Count && index < 32 * 1024; index++)
                    {
                        var sortedList = signatureTreeItems[index].Profile.TrustSignatures.ToList();
                        sortedList.Sort();

                        foreach (var trustSignature in sortedList)
                        {
                            if (checkedSignatures.Contains(trustSignature)) continue;

                            Profile tempProfile;
                            if (!Settings.Instance.Global_Profiles.TryGetValue(trustSignature, out tempProfile)) continue;

                            var tempItem = new SignatureTreeItem(tempProfile);
                            signatureTreeItems[index].Children.Add(tempItem);

                            workSignatureTreeItems.Add(tempItem);
                            workCheckedSignatures.Add(trustSignature);
                        }
                    }

                    if (workSignatureTreeItems.Count == 0) break;

                    signatureTreeItems.AddRange(workSignatureTreeItems);
                    workSignatureTreeItems.Clear();

                    checkedSignatures.UnionWith(workCheckedSignatures);
                    workCheckedSignatures.Clear();
                }
            }

            return signatureTreeItems[0];
        }

        #region _treeView

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SignatureTreeViewItem;
            if (selectTreeViewItem == null) return;

            _trustSignatureListView.Items.Clear();
            _trustSignatureListView.Items.AddRange(selectTreeViewItem.Value.Profile.TrustSignatures);

            _wikiListView.Items.Clear();
            _wikiListView.Items.AddRange(selectTreeViewItem.Value.Profile.Wikis);

            _chatListView.Items.Clear();
            _chatListView.Items.AddRange(selectTreeViewItem.Value.Profile.Chats);
        }

        private void _treeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _treeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var signatureTreeViewItem = _treeView.SelectedItem as SignatureTreeViewItem;
            if (signatureTreeViewItem == null) return;

            Clipboard.SetText(signatureTreeViewItem.Value.Profile.Certificate.ToString());
        }

        #endregion

        #region _trustSignature

        private void _trustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _trustSignatureListView.SelectedItems;

            _trustSignatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _trustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _wikiListView

        private void _wikiListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _wikiListView.SelectedItems;

            _wikiListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _wikiListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _wikiListView.SelectedItems.OfType<Wiki>())
            {
                sb.AppendLine(OutoposConverter.ToWikiString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _chatListView

        private void _chatListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _chatListView.SelectedItems;

            _chatListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _chatListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _chatListView.SelectedItems.OfType<Chat>())
            {
                sb.AppendLine(OutoposConverter.ToChatString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {

            }
        }
    }
}
