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
    delegate void UploadEventHandler(object sender);

    /// <summary>
    /// Interaction logic for SectionTreeItemEditWindow.xaml
    /// </summary>
    partial class SectionTreeItemEditWindow : Window
    {
        private SectionTreeItem _sectionTreeItem;
        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private LeaderControl _leaderControl;
        private CreatorControl _creatorControl;
        private ManagerControl _managerControl;

        private string _uploadSignature;

        private LeaderInfo _leaderInfo;
        private CreatorInfo _creatorInfo;
        private ManagerInfo _managerInfo;

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

            lock (_sectionTreeItem.ThisLock)
            {
                _sectionTextBox.Text = MessageConverter.ToSectionString(_sectionTreeItem.Section);
                _sectionLeaderSignatureTextBox.Text = _sectionTreeItem.LeaderSignature;
                _uploadSignature = _sectionTreeItem.UploadSignature;

                _leaderInfo = _sectionTreeItem.LeaderInfo.DeepClone();
                _creatorInfo = _sectionTreeItem.CreatorInfo.DeepClone();
                _managerInfo = _sectionTreeItem.ManagerInfo.DeepClone();
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _leaderControl = new LeaderControl(_leaderInfo, _bufferManager);
            _leaderControl.Height = Double.NaN;
            _leaderControl.Width = Double.NaN;
            _leaderTabItem.Content = _leaderControl;

            _creatorControl = new CreatorControl(_creatorInfo, _bufferManager);
            _creatorControl.Height = Double.NaN;
            _creatorControl.Width = Double.NaN;
            _creatorTabItem.Content = _creatorControl;

            _managerControl = new ManagerControl(_managerInfo, _bufferManager);
            _managerControl.Height = Double.NaN;
            _managerControl.Width = Double.NaN;
            _managerTabItem.Content = _managerControl;

            _leaderControl.UploadEvent += new UploadEventHandler(_leaderControl_UploadEvent);
            _creatorControl.UploadEvent += new UploadEventHandler(_creatorControl_UploadEvent);
            _managerControl.UploadEvent += new UploadEventHandler(_managerControl_UploadEvent);

            for (int index = 0; index < Settings.Instance.Global_DigitalSignatureCollection.Count; index++)
            {
                if (Settings.Instance.Global_DigitalSignatureCollection[index].ToString() == _uploadSignature)
                {
                    _signatureComboBox.SelectedIndex = index + 1;

                    break;
                }
            }
        }

        void _leaderControl_UploadEvent(object sender)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            if (MessageBox.Show(this, LanguagesManager.Instance.MainWindow_Upload_Message, "Leader", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var info = _leaderControl.LeaderInfo;

            _lairManager.Upload(new Leader(_sectionTreeItem.Section, info.Comment, info.CreatorSignatures, info.ManagerSignatures, digitalSignature));
            _sectionTreeItem.LeaderInfo = info;
        }

        void _creatorControl_UploadEvent(object sender)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            if (MessageBox.Show(this, LanguagesManager.Instance.MainWindow_Upload_Message, "Creator", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var info = _creatorControl.CreatorInfo;

            _lairManager.Upload(new Creator(_sectionTreeItem.Section, info.Comment, info.Channels, digitalSignature));
            _sectionTreeItem.CreatorInfo = info;
        }

        void _managerControl_UploadEvent(object sender)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            if (MessageBox.Show(this, LanguagesManager.Instance.MainWindow_Upload_Message, "Manager", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var info = _managerControl.ManagerInfo;

            _lairManager.Upload(new Manager(_sectionTreeItem.Section, info.Comment, info.TrustSignatures, digitalSignature));
            _sectionTreeItem.ManagerInfo = info;
        }

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isUploadEnabled = (_signatureComboBox.SelectedIndex != 0);

            _leaderControl.IsUploadEnabled = isUploadEnabled;
            _creatorControl.IsUploadEnabled = isUploadEnabled;
            _managerControl.IsUploadEnabled = isUploadEnabled;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            lock (_sectionTreeItem.ThisLock)
            {
                _sectionTreeItem.LeaderSignature = Signature.HasSignature(_sectionLeaderSignatureTextBox.Text) ? _sectionLeaderSignatureTextBox.Text : null;
                _sectionTreeItem.UploadSignature = (digitalSignature == null) ? null : digitalSignature.ToString();

                _sectionTreeItem.LeaderInfo = _leaderControl.LeaderInfo;
                _sectionTreeItem.CreatorInfo = _creatorControl.CreatorInfo;
                _sectionTreeItem.ManagerInfo = _managerControl.ManagerInfo;
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void _signatureComboBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            if (digitalSignatureComboBoxItem == null) return;

            Clipboard.SetText(digitalSignatureComboBoxItem.Value.ToString());
        }
    }
}
