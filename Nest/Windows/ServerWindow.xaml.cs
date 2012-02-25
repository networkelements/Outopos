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
using Library.Net.Nest;
using Library;

namespace Nest.Windows
{
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        ServerManager _serverManager;

        public ServerWindow(ref string name, ref ServerManager serverManager)
        {
            _serverManager = serverManager;

            InitializeComponent();

            _nameTextBox.Text = name;

            using (DeadlockMonitor.Lock(_serverManager.ThisLock))
            {
                //_hostTextBox.Text = serverManager.
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
