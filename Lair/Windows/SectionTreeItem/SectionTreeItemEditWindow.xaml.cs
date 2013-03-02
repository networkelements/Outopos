using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Lair.Properties;
using Library;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    delegate void LeaderUploadEventHandler(object sender, LeaderInfo info);
    delegate void CreatorUploadEventHandler(object sender, CreatorInfo info);
    delegate void ManagerUploadEventHandler(object sender, ManagerInfo info);

    /// <summary>
    /// Interaction logic for SectionTreeItemEditWindow.xaml
    /// </summary>
    partial class SectionTreeItemEditWindow : Window
    {
        private LairManager _lairManager;
        private BufferManager _bufferManager;
        private SectionTreeItem _sectionTreeItem;

        private LeaderControl _leaderControl;
        private CreatorControl _creatorControl;
        private ManagerControl _managerControl;

        public SectionTreeItemEditWindow(SectionTreeItem sectionTreeItem, LairManager lairManager, BufferManager bufferManager)
        {
            _sectionTreeItem = sectionTreeItem;
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _signatureComboBox.ItemsSource = digitalSignatureCollection;

            for (int index = 0; index < Settings.Instance.Global_DigitalSignatureCollection.Count; index++)
            {
                if (Settings.Instance.Global_DigitalSignatureCollection[index].ToString() == _sectionTreeItem.UploadSignature)
                {
                    _signatureComboBox.SelectedIndex = index + 1;

                    break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _leaderControl = new LeaderControl(_sectionTreeItem.LeaderInfo, _bufferManager);
            _leaderControl.Height = Double.NaN;
            _leaderControl.Width = Double.NaN;
            _leaderTabItem.Content = _leaderControl;

            _creatorControl = new CreatorControl(_sectionTreeItem.CreatorInfo, _bufferManager);
            _creatorControl.Height = Double.NaN;
            _creatorControl.Width = Double.NaN;
            _creatorTabItem.Content = _creatorControl;

            _managerControl = new ManagerControl(_sectionTreeItem.ManagerInfo, _bufferManager);
            _managerControl.Height = Double.NaN;
            _managerControl.Width = Double.NaN;
            _managerTabItem.Content = _managerControl;

            _leaderControl.LeaderUploadEvent += new LeaderUploadEventHandler(_leaderControl_LeaderUploadEvent);
            _creatorControl.CreatorUploadEvent += new CreatorUploadEventHandler(_creatorControl_CreatorUploadEvent);
            _managerControl.ManagerUploadEvent += new ManagerUploadEventHandler(_managerControl_ManagerUploadEvent);
        }

        void _leaderControl_LeaderUploadEvent(object sender, LeaderInfo info)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _lairManager.Upload(new Leader(_sectionTreeItem.Section, info.Comment, info.CreatorSignatures, info.ManagerSignatures, digitalSignature));
        }

        void _creatorControl_CreatorUploadEvent(object sender, CreatorInfo info)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _lairManager.Upload(new Creator(_sectionTreeItem.Section, info.Comment, info.Channels, digitalSignature));
        }

        void _managerControl_ManagerUploadEvent(object sender, ManagerInfo info)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _lairManager.Upload(new Manager(_sectionTreeItem.Section, info.Comment, info.TrustSignatures, digitalSignature));
        }

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isUploadEnabled = (_signatureComboBox.SelectedIndex != 0);

            _leaderControl.IsUploadEnabled = isUploadEnabled;
            _creatorControl.IsUploadEnabled = isUploadEnabled;
            _managerControl.IsUploadEnabled = isUploadEnabled;
        }
    }
}
