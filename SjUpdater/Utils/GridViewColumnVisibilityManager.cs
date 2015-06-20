using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SjUpdater.Utils
{
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
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(GridViewColumnVisibilityManager), new UIPropertyMetadata(true));


        public static bool GetEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(EnabledProperty, value);
        }

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(GridViewColumnVisibilityManager), new UIPropertyMetadata(false,
                new PropertyChangedCallback(OnEnabledChanged)));

        private static void OnEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ListView view = obj as ListView;
            if (view != null)
            {
                bool enabled = (bool)e.NewValue;
                if (enabled)
                {
                    view.Loaded += (sender, e2) =>
                    {
                        UpdateListView((ListView)sender);
                    };
                    view.TargetUpdated += (sender, e2) =>
                    {
                        UpdateListView((ListView)sender);
                    };
                    view.DataContextChanged += (sender, e2) =>
                    {
                        UpdateListView((ListView)sender);
                    };
                }
            }
        }
    }
}
