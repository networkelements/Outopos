using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Lair.Windows
{
    class VirtualizingStackPanelEx : VirtualizingStackPanel
    {
        public VirtualizingStackPanelEx()
        {
       
        }

        protected override void OnItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);

            try
            {
                this.ScrollOwner.ScrollToBottom();
                this.ScrollOwner.PageDown();
            }
            catch (Exception)
            {

            }
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            try
            {
                if (e.Delta > 0) this.ScrollOwner.LineUp();
                if (e.Delta < 0) this.ScrollOwner.LineDown();

                e.Handled = true;
            }
            catch (Exception)
            {

            }
        }
    }
}
