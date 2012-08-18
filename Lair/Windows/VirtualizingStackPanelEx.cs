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
            }
            catch (Exception)
            {

            }
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            return base.ArrangeOverride(new Size(arrangeSize.Width, arrangeSize.Height * 3));
        }

        public override void MouseWheelUp()
        {
            this.ScrollOwner.LineUp();
        }

        public override void MouseWheelDown()
        {
            this.ScrollOwner.LineDown();
        }
    }
}
