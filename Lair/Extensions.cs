using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace Lair
{
    static class ListViewExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static int GetCurrentIndex(this ListView myListView, GetPositionDelegate getPosition)
        {
            try
            {
                for (int i = 0; i < myListView.Items.Count; i++)
                {
                    ListViewItem item = ListViewExtensions.GetListViewItem(myListView, i);

                    if (ListViewExtensions.IsMouseOverTarget(myListView, item, getPosition))
                    {
                        return i;
                    }
                }
            }
            catch (Exception)
            {

            }

            return -1;
        }

        private static ListViewItem GetListViewItem(ListView myListView, int index)
        {
            if (myListView.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return myListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        private static bool IsMouseOverTarget(ListView myListView, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }

    static class ListBoxExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static int GetCurrentIndex(this ListBox myListBox, GetPositionDelegate getPosition)
        {
            try
            {
                for (int i = 0; i < myListBox.Items.Count; i++)
                {
                    ListBoxItem item = ListBoxExtensions.GetListBoxItem(myListBox, i);

                    if (ListBoxExtensions.IsMouseOverTarget(myListBox, item, getPosition))
                    {
                        return i;
                    }
                }
            }
            catch (Exception)
            {

            }

            return -1;
        }

        private static ListBoxItem GetListBoxItem(ListBox myListBox, int index)
        {
            if (myListBox.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return myListBox.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
        }

        private static bool IsMouseOverTarget(ListBox myListBox, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }
    }
         
    static class TreeViewExtensions
    {
        public delegate Point GetPositionDelegate(IInputElement element);

        public static object GetCurrentItem(this TreeView myTreeView, GetPositionDelegate getPosition)
        {
            try
            {
                var items = new List<TreeViewItem>();
                items.AddRange(myTreeView.Items.OfType<TreeViewItem>());

                for (int i = 0; i < items.Count; i++)
                {
                    if (!items[i].IsExpanded) continue;

                    foreach (TreeViewItem item in items[i].Items)
                    {
                        items.Add(item);
                    }
                }

                items.Reverse();

                foreach (var item in items)
                {
                    if (TreeViewExtensions.IsMouseOverTarget(myTreeView, item, getPosition))
                    {
                        return item;
                    }
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        private static bool IsMouseOverTarget(TreeView myTreeView, Visual target, GetPositionDelegate getPosition)
        {
            if (target == null) return false;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }

        public static IEnumerable<TreeViewItem> GetLineage(this TreeView parentView, TreeViewItem childItem)
        {
            var list = new List<TreeViewItem>();
            list.AddRange(parentView.Items.Cast<TreeViewItem>());

            for (int i = 0; i < list.Count; i++)
            {
                foreach (TreeViewItem item in list[i].Items)
                {
                    list.Add(item);
                }
            }

            var targetList = new List<TreeViewItem>();
            targetList.Add(childItem);

            try
            {
                for (; ; )
                {
                    var item = targetList.Last();
                    if (parentView.Items.Contains(item)) break;

                    targetList.Add(list.First(n => n.Items.Contains(item)));
                }
            }
            catch (Exception)
            {

            }

            targetList.Reverse();

            return targetList;
        }
    }

    static class TreeViewItemExtensions
    {
        public static IEnumerable<TreeViewItem> GetLineage(this TreeViewItem parentItem, TreeViewItem childItem)
        {
            var list = new List<TreeViewItem>();
            list.Add(parentItem);

            for (int i = 0; i < list.Count; i++)
            {
                foreach (TreeViewItem item in list[i].Items)
                {
                    list.Add(item);
                }
            }

            var targetList = new List<TreeViewItem>();
            targetList.Add(childItem);

            try
            {
                for (; ; )
                {
                    var item = targetList.Last();
                    if (item == parentItem) break;

                    targetList.Add(list.First(n => n.Items.Contains(item)));
                }
            }
            catch (Exception)
            {

            }

            targetList.Reverse();

            return targetList;
        }
    }

    //http://geekswithblogs.net/sonam/archive/2009/03/02/listview-dragdrop-in-wpfmultiselect.aspx

    /// <summary>
    /// Provides access to the mouse location by calling unmanaged code.
    /// </summary>
    /// <remarks>
    /// This class was written by Dan Crevier (Microsoft). 
    /// http://blogs.msdn.com/llobo/archive/2006/09/06/Scrolling-Scrollviewer-on-Mouse-Drag-at-the-boundaries.aspx
    /// </remarks>
    public class MouseUtilities
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hwnd, ref Win32Point pt);

        /// <summary>
        /// Returns the mouse cursor location.  This method is necessary during
        /// a drag-drop operation because the WPF mechanisms for retrieving the
        /// cursor coordinates are unreliable.
        /// </summary>
        /// <param name="relativeTo">The Visual to which the mouse coordinates will be relative.</param>
        public static Point GetMousePosition(Visual relativeTo)
        {
            Win32Point mouse = new Win32Point();
            GetCursorPos(ref mouse);
            return relativeTo.PointFromScreen(new Point((double)mouse.X, (double)mouse.Y));
        }
    }

    // http://pro.art55.jp/?eid=1160884
    public static class ItemsControlUtilities
    {
        public static void GoBottom(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.SetVerticalOffset(double.PositiveInfinity);
        }

        public static void GoTop(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.SetVerticalOffset(0);
        }

        public static void GoRight(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.SetHorizontalOffset(double.PositiveInfinity);
        }

        public static void GoLeft(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.SetHorizontalOffset(0);
        }

        public static void PageDown(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.PageDown();
        }

        public static void PageUp(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.PageUp();
        }

        public static void PageRight(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.PageRight();
        }

        public static void PageLeft(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.PageLeft();
        }

        public static void LineDown(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.LineDown();
        }

        public static void LineUp(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.LineUp();
        }

        public static void LineRight(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.LineRight();
        }

        public static void LineLeft(this ItemsControl itemsControl)
        {
            var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
            panel.LineLeft();
        }

        public static Panel FindItemsHostPanel(this ItemsControl itemsControl)
        {
            return Find(itemsControl.ItemContainerGenerator, itemsControl);
        }

        private static Panel Find(this IItemContainerGenerator generator, DependencyObject control)
        {
            int count = VisualTreeHelper.GetChildrenCount(control);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);
                if (IsItemsHostPanel(generator, child))
                    return (Panel)child;

                Panel panel = Find(generator, child);
                if (panel != null)
                    return panel;
            }
            return null;
        }

        private static bool IsItemsHostPanel(IItemContainerGenerator generator, DependencyObject target)
        {
            var panel = target as Panel;
            return panel != null && panel.IsItemsHost && generator == generator.GetItemContainerGeneratorForPanel(panel);
        }
    }
}
