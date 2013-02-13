using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Lair.Properties;
using Library;
using Library.Collections;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for ControlSectionControl.xaml
    /// </summary>
    partial class ControlSectionControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        private ObservableCollection<SectionTreeViewItem> _treeViewItemCollection = new ObservableCollection<SectionTreeViewItem>();

        public ControlSectionControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();

            _treeView.ItemsSource = _treeViewItemCollection;

            foreach (var item in Settings.Instance.ControlChannelControl_SectionCategories)
            {
                _treeViewItemCollection.Add(new SectionTreeViewItem(item));
            }
        }

        private void Update()
        {
            Settings.Instance.ControlChannelControl_SectionCategories = _treeViewItemCollection.Cast<SectionTreeViewItem>().Select(n => n.Value).ToLockedList();

            var list = _treeViewItemCollection.OfType<SectionTreeViewItem>().ToList();

            list.Sort((SectionTreeViewItem x, SectionTreeViewItem y) =>
            {
                var cx = x.Value.Section;
                var cy = y.Value.Section;

                int c = cx.Name.CompareTo(cy.Name);
                if (c != 0) return c;
                c = Collection.Compare(cx.Id, cy.Id);
                if (c != 0) return c;

                return 0;
            });
            
            for (int i = 0; i < list.Count; i++)
            {
                var o = _treeViewItemCollection.IndexOf(list[i]);

                if (i != o) _treeViewItemCollection.Move(o, i);
            }

            foreach (var item in _treeViewItemCollection.OfType<SectionTreeViewItem>())
            {
                item.Sort();
            }
        }

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewSectionWindow window = new NewSectionWindow();
            window.Owner = _mainWindow;
            window.ShowDialog();

            _treeViewItemCollection.Add(new SectionTreeViewItem(new SectionCategory() { Section = window.Section, IsExpanded = true }));
            this.Update();
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    class SectionTreeViewItem : TreeViewItem
    {
        private ObservableCollection<object> _listViewItemCollection = new ObservableCollection<object>();
        private SectionCategory _value;

        public SectionTreeViewItem(SectionCategory sectionTreeItem)
            : base()
        {
            this.Value = sectionTreeItem;

            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        protected override void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);

            this.Value.IsExpanded = true;
        }

        protected override void OnCollapsed(RoutedEventArgs e)
        {
            base.OnCollapsed(e);

            this.Value.IsExpanded = false;
        }

        public void Update()
        {
            base.Header = MessageConverter.ToSectionString(this.Value.Section);

            List<dynamic> list = new List<dynamic>();

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in this.Value.Leaders)
            {
                list.Add(new LeaderTreeViewItem(item));
            }

            foreach (var item in this.Value.Managers)
            {
                list.Add(new ManagerTreeViewItem(item));
            }

            foreach (var item in this.Value.Creators)
            {
                list.Add(new CreatorTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.Cast<dynamic>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.Cast<dynamic>().Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<object>().ToList();

            Dictionary<Type, int> typeSortItems = new Dictionary<Type, int>();
            typeSortItems[typeof(Leader)] = 0;
            typeSortItems[typeof(Manager)] = 1;
            typeSortItems[typeof(Creator)] = 2;

            list.Sort(delegate(object x, object y)
            {
                int tx = typeSortItems[x.GetType()];
                int ty = typeSortItems[y.GetType()];

                int c = tx.CompareTo(ty);
                if (c != 0) return c;
                c = ((ICertificate)x).Certificate.ToString().CompareTo(((ICertificate)y).Certificate.ToString());
                if (c != 0) return c;

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }
        }

        public SectionCategory Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }
    }

    abstract class RoleTreeViewItem<T> : TreeViewItem
    {
        private T _value;

        public RoleTreeViewItem(T item)
            : base()
        {
            this.Value = item;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public abstract void Update();

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }
    }

    class LeaderTreeViewItem : RoleTreeViewItem<Leader>
    {
        public LeaderTreeViewItem(Leader leader)
            : base(leader)
        {

        }

        public override void Update()
        {
            base.Header = string.Format("{0} : {1}", LanguagesManager.Instance.ControlSectionControl_Leader, base.Value.Certificate.ToString());
        }
    }

    class ManagerTreeViewItem : RoleTreeViewItem<Manager>
    {
        public ManagerTreeViewItem(Manager leader)
            : base(leader)
        {

        }

        public override void Update()
        {
            base.Header = string.Format("{0} : {1}", LanguagesManager.Instance.ControlSectionControl_Manager, base.Value.Certificate.ToString());
        }
    }

    class CreatorTreeViewItem : RoleTreeViewItem<Creator>
    {
        public CreatorTreeViewItem(Creator leader)
            : base(leader)
        {

        }

        public override void Update()
        {
            base.Header = string.Format("{0} : {1}", LanguagesManager.Instance.ControlSectionControl_Creator, base.Value.Certificate.ToString());
        }
    }

    [DataContract(Name = "SectionCategory", Namespace = "http://Lair/Windows")]
    class SectionCategory : IDeepCloneable<SectionCategory>, IThisLock
    {
        private Section _section;
        private bool _isExpanded = true;

        private LockedList<Leader> _leaders;
        private LockedList<Manager> _managers;
        private LockedList<Creator> _creators;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Section")]
        public Section Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _section;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _section = value;
                }
            }
        }

        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isExpanded;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isExpanded = value;
                }
            }
        }

        [DataMember(Name = "Leaders")]
        public LockedList<Leader> Leaders
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_leaders == null)
                        _leaders = new LockedList<Leader>();

                    return _leaders;
                }
            }
        }

        [DataMember(Name = "Managers")]
        public LockedList<Manager> Managers
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_managers == null)
                        _managers = new LockedList<Manager>();

                    return _managers;
                }
            }
        }

        [DataMember(Name = "Creators")]
        public LockedList<Creator> Creators
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_creators == null)
                        _creators = new LockedList<Creator>();

                    return _creators;
                }
            }
        }

        #region IDeepClone<SectionTreeItem>

        public SectionCategory DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SectionCategory));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SectionCategory)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}
