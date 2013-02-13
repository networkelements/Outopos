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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Library;
using Library.Net.Lair;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for ControlControl.xaml
    /// </summary>
    partial class ControlControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        public ControlControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ControlChartControl _controlChartControl = new ControlChartControl();
            _controlChartControl.Height = Double.NaN;
            _controlChartControl.Width = Double.NaN;
            _chartTabItem.Content = _controlChartControl;

            ControlSectionControl _controlSectionControl = new ControlSectionControl(_mainWindow, _lairManager, _bufferManager);
            _controlSectionControl.Height = Double.NaN;
            _controlSectionControl.Width = Double.NaN;
            _sectionTabItem.Content = _controlSectionControl;

            ControlChannelControl _controlChannelControl = new ControlChannelControl(_mainWindow, _lairManager, _bufferManager);
            _controlChannelControl.Height = Double.NaN;
            _controlChannelControl.Width = Double.NaN;
            _channelTabItem.Content = _controlChannelControl;
        }
    }
}
