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

namespace Outopos.Windows
{
    class VirtualizingStackPanelEx : VirtualizingStackPanel
    {
        public VirtualizingStackPanelEx()
        {

        }

        public override void MouseWheelUp()
        {
            try
            {
                base.ScrollOwner.LineUp();
            }
            catch (Exception)
            {

            }
        }

        public override void MouseWheelDown()
        {
            try
            {
                base.ScrollOwner.LineDown();
            }
            catch (Exception)
            {

            }
        }
    }
}