using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace SjUpdater.Utils
{

    //Copied from http://stackoverflow.com/questions/20985005/get-parent-for-gridviewcolumn
    public static class DependencyObjectExtensions
    {
        private static readonly PropertyInfo InheritanceContextProp = typeof(DependencyObject).GetProperty("InheritanceContext", BindingFlags.NonPublic | BindingFlags.Instance);

        public static IEnumerable<DependencyObject> GetParents(this DependencyObject child)
        {
            while (child != null)
            {
                var parent = LogicalTreeHelper.GetParent(child);
                if (parent == null)
                {
                    if (child is FrameworkElement)
                    {
                        parent = VisualTreeHelper.GetParent(child);
                    }
                    if (parent == null && child is ContentElement)
                    {
                        parent = ContentOperations.GetParent((ContentElement)child);
                    }
                    if (parent == null)
                    {
                        parent = InheritanceContextProp.GetValue(child, null) as DependencyObject;
                    }
                }
                child = parent;
                yield return parent;
            }
        }
    }




    //Adapted from http://stackoverflow.com/a/9634769/2606757
    public class GridViewColumnVisibilityManager
    {
        static void UpdateListView(ListView lv)
        {
            GridView gridview = lv.View as GridView;
            if (gridview == null || gridview.Columns == null) return;
            List<GridViewColumn> toRemove = new List<GridViewColumn>();
            foreach (GridViewColumn gc in gridview.Columns)
            {
                if (GetIsVisible(gc) == false)
                {
                    toRemove.Add(gc);
                }
            }
            foreach (GridViewColumn gc in toRemove)
            {
                gridview.Columns.Remove(gc);
            }
        }

        public static bool GetIsVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsVisibleProperty, value);
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(GridViewColumnVisibilityManager), new UIPropertyMetadata(true,new PropertyChangedCallback(OnVisibleChanged)));

        private static void OnVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var parents = dependencyObject.GetParents();
            foreach (DependencyObject parent in parents)
            {
                var l = parent as ListView;
                if (l != null)
                {
                    UpdateListView(l);
                    return;
                }
            }
        }
    }
}
