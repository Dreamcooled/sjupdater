using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

//Copied from: http://www.codeproject.com/Articles/28306/Working-with-Checkboxes-in-the-WPF-TreeView

namespace SjUpdater.Utils
{
	public class TreeViewExtensions : DependencyObject
	{
		public static bool GetEnableMultiSelect(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableMultiSelectProperty);
		}

		public static void SetEnableMultiSelect(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableMultiSelectProperty, value);
		}

		// Using a DependencyProperty as the backing store for EnableMultiSelect.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty EnableMultiSelectProperty =
			DependencyProperty.RegisterAttached("EnableMultiSelect", typeof(bool), typeof(TreeViewExtensions), new FrameworkPropertyMetadata(false)
			{
				PropertyChangedCallback = EnableMultiSelectChanged,
				BindsTwoWayByDefault = true
			});

		public static IList GetSelectedItems(DependencyObject obj)
		{
			return (IList)obj.GetValue(SelectedItemsProperty);
		}

		public static void SetSelectedItems(DependencyObject obj, IList value)
		{
			obj.SetValue(SelectedItemsProperty, value);
		}

		// Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(TreeViewExtensions), new PropertyMetadata(null));


		static TreeViewItem GetAnchorItem(DependencyObject obj)
		{
			return (TreeViewItem)obj.GetValue(AnchorItemProperty);
		}

		static void SetAnchorItem(DependencyObject obj, TreeViewItem value)
		{
			obj.SetValue(AnchorItemProperty, value);
		}

		// Using a DependencyProperty as the backing store for AnchorItem.  This enables animation, styling, binding, etc...
		static readonly DependencyProperty AnchorItemProperty =
			DependencyProperty.RegisterAttached("AnchorItem", typeof(TreeViewItem), typeof(TreeViewExtensions), new PropertyMetadata(null));


		static void EnableMultiSelectChanged(DependencyObject s, DependencyPropertyChangedEventArgs args)
		{
			TreeView tree = (TreeView)s;
			var wasEnable = (bool)args.OldValue;
			var isEnabled = (bool)args.NewValue;
			if(wasEnable)
			{
				tree.RemoveHandler(TreeViewItem.MouseDownEvent, new MouseButtonEventHandler(ItemClicked));
				tree.RemoveHandler(TreeView.PreviewKeyDownEvent, new KeyEventHandler(KeyDown));
			}
			if(isEnabled)
			{
				tree.AddHandler(TreeViewItem.MouseDownEvent, new MouseButtonEventHandler(ItemClicked), true);
                tree.AddHandler(TreeView.PreviewKeyDownEvent, new KeyEventHandler(KeyDown));
			}
		}

		static TreeView GetTree(TreeViewItem item)
		{
			Func<DependencyObject, DependencyObject> getParent = VisualTreeHelper.GetParent;
			FrameworkElement currentItem = item;
			while(!(getParent(currentItem) is TreeView))
				currentItem = (FrameworkElement)getParent(currentItem);
			return (TreeView)getParent(currentItem);
		}

        static TreeViewItem GetParentTreeViewItem(TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem))
            {
                if (parent is TreeView) return null;
                parent = VisualTreeHelper.GetParent(parent);

            }
            return parent as TreeViewItem;
        }


        static void SetIsChecked(TreeViewItem item, bool? value, bool updateChildren, bool updateParent)
        {

            if (value == GetIsSelected(item))
                return;

            item.SetValue(IsSelectedProperty, value);

          
            if (updateChildren && value.HasValue)
            {
                foreach (var treeViewItem in GetChildren(item))
                {
                    SetIsChecked(treeViewItem,value, true, false);
                }
            }
               
            if (updateParent && GetParentTreeViewItem(item)!= null)
                VerifyCheckState(GetParentTreeViewItem(item));

        }

        static void VerifyCheckState(TreeViewItem item)
        {
            bool? state = null;

            bool first = true;
            foreach (var treeViewItem in GetChildren(item))
            {
                bool? current = GetIsSelected(treeViewItem);
                if (first)
                {
                    first = false;
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            SetIsChecked(item,state, false, true);
        }

		static void RealSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			TreeViewItem item = (TreeViewItem)sender;
            
		    var isSelected = (bool?)args.NewValue;
 
			var selectedItems = GetSelectedItems(GetTree(item));
			if(selectedItems != null)
			{
				if(isSelected==true)
					try
					{
						selectedItems.Add(item.Header);
					}
					catch(ArgumentException)
					{
					}
				else
					selectedItems.Remove(item.Header);
			}
		}

		static void KeyDown(object sender, KeyEventArgs e)
		{
            var tree = (TreeView)sender;
            var selectedItems = GetSelectedTreeViewItems(tree);
			
		    if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Control) //Ctrl + A
		    {
                foreach (var item in GetTreeViewItems(tree).ToList())
		        {
		            SetIsSelected(item, true);
		        }
		        e.Handled = true;
		    }
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.None && selectedItems.Count>0)
            {
                var items = GetTreeViewItems(tree, true).ToList(); //get expanded items
                switch (e.Key)
                {
                    case Key.Space:
                        if (selectedItems.Count == 1)
                        {
                            selectedItems.First().IsExpanded ^= true;
                        }
                        break;
                    case Key.Up:
                        int ind = items.IndexOf(selectedItems.First());
                        if (ind > 0)
                        {
                            MakeSingleSelection(tree, items[ind - 1]);
                        }
                        else
                        {
                            MakeSingleSelection(tree, items.First());
                        }

                        break;
                    case Key.Down:
                        int ind2 = items.IndexOf(selectedItems.Last());
                        if (ind2 < items.Count - 1)
                        {
                            MakeSingleSelection(tree, items[ind2 + 1]);
                        }
                        else
                        {
                            MakeSingleSelection(tree, items.Last());
                        }

                        break;
                }
            } 
		}

		static void ItemClicked(object sender, MouseButtonEventArgs e)
		{
			TreeViewItem item = FindTreeViewItem(e.OriginalSource);
			if(item == null)
				return;
			TreeView tree = (TreeView)sender;
            
            //We dont need this
			//var mouseButton = e.ChangedButton;
			/*if(mouseButton != MouseButton.Left)
			{
				if((mouseButton == MouseButton.Right) && ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None))
				{
					if(GetIsSelected(item)==true)
					{
						UpdateAnchorAndActionItem(tree, item);
						return;
					}
					MakeSingleSelection(tree, item);
				}
				return;
			}
			if(mouseButton != MouseButton.Left)
			{
				if((mouseButton == MouseButton.Right) && ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None))
				{
                    if (GetIsSelected(item) == true)
					{
						UpdateAnchorAndActionItem(tree, item);
						return;
					}
					MakeSingleSelection(tree, item);
				}
				return;
			}*/
			if((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != (ModifierKeys.Shift | ModifierKeys.Control))
			{
				if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
				{
					MakeToggleSelection(tree, item);
					return;
				}
				if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
				{
					MakeAnchorSelection(tree, item, true);
					return;
				}
				MakeSingleSelection(tree, item);
				return;
			}
			//MakeAnchorSelection(item, false);


			//SetIsSelected(tree.SelectedItem
		}

		private static TreeViewItem FindTreeViewItem(object obj)
		{
			DependencyObject dpObj = obj as DependencyObject;
			if(dpObj == null)
				return null;
			if(dpObj is TreeViewItem)
				return (TreeViewItem)dpObj;
			return FindTreeViewItem(VisualTreeHelper.GetParent(dpObj));
		}


	    private static IEnumerable<TreeViewItem> GetChildren(TreeViewItem treeViewItem)
	    {
            for (int i = 0; i < treeViewItem.Items.Count; i++)
	        {
                var item = (TreeViewItem)treeViewItem.ItemContainerGenerator.ContainerFromIndex(i);
                if (item == null)
                    continue;
                yield return item;
	        }
	    } 

		private static IEnumerable<TreeViewItem> GetTreeViewItems(ItemsControl tree, bool only_expanded=false)
		{
			for(int i = 0; i < tree.Items.Count; i++)
			{
				var item = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(i);
				if(item == null)
					continue;
				yield return item;
				if(item.IsExpanded || !only_expanded)
				foreach(var subItem in GetTreeViewItems(item))
				    yield return subItem;
			}
		}

		private static void MakeAnchorSelection(TreeView tree, TreeViewItem actionItem, bool clearCurrent)
		{
			if(GetAnchorItem(tree) == null)
			{
				var selectedItems = GetSelectedTreeViewItems(tree);
				if(selectedItems.Count > 0)
				{
					SetAnchorItem(tree, selectedItems[selectedItems.Count - 1]);
				}
				else
				{
					SetAnchorItem(tree, GetTreeViewItems(tree).Skip(3).FirstOrDefault());
				}
				if(GetAnchorItem(tree) == null)
				{
					return;
				}
			}

			var anchor = GetAnchorItem(tree);

			var items = GetTreeViewItems(tree);
			bool betweenBoundary = false;
			foreach(var item in items)
			{
				bool isBoundary = item == anchor || item == actionItem;
				if(isBoundary)
				{
					betweenBoundary = !betweenBoundary;
				}
				if(betweenBoundary || isBoundary)
					SetIsSelected(item, true);
				else
					if(clearCurrent)
						SetIsSelected(item, false);
					else
						break;

			}
		}

		private static List<TreeViewItem> GetSelectedTreeViewItems(TreeView tree)
		{
			return GetTreeViewItems(tree).Where(i => GetIsSelected(i)==true).ToList();
		}

		private static void MakeSingleSelection(TreeView tree, TreeViewItem item)
		{
			foreach(TreeViewItem selectedItem in GetTreeViewItems(tree))
			{
				if(selectedItem == null)
					continue;
				if(selectedItem != item)
                     selectedItem.SetValue(IsSelectedProperty, false);

				else
				{
                    selectedItem.SetValue(IsSelectedProperty, true);
				}
			}
			UpdateAnchorAndActionItem(tree, item);
		}


	    public static void ClearSelection(TreeView tree)
	    {
            foreach (TreeViewItem selectedItem in GetTreeViewItems(tree))
            {
                selectedItem.SetValue(IsSelectedProperty, false);
            }
	    }

		private static void MakeToggleSelection(TreeView tree, TreeViewItem item)
		{
		    bool? state = GetIsSelected(item);
		    if (state == false || !state.HasValue)
		    {
		        SetIsSelected(item, true);
            }
            else 
            {
                SetIsSelected(item, false);
            }
       
			UpdateAnchorAndActionItem(tree, item);
		}

		private static void UpdateAnchorAndActionItem(TreeView tree, TreeViewItem item)
		{
			SetAnchorItem(tree, item);
		}
		public static bool? GetIsSelected(DependencyObject obj)
		{
			return (bool?)obj.GetValue(IsSelectedProperty);
		}

		public static void SetIsSelected(DependencyObject obj, bool? value)
		{
			//obj.SetValue(IsSelectedProperty, value);
            SetIsChecked(obj as TreeViewItem, value, true, true);
		}

		// Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.RegisterAttached("IsSelected", typeof(bool?), typeof(TreeViewExtensions), new PropertyMetadata(false)
			 {
				PropertyChangedCallback = RealSelectedChanged
			});

	}
}
