#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

#endregion

namespace Menees.RpnCalc
{
	public partial class DisplayStack
	{
		#region Public Properties

		public Calculator Calculator
		{
			get
			{
				return m_calc;
			}
			set
			{
				if (m_calc != value)
				{
					if (m_calc != null)
					{
						m_calc.DisplayFormatChanged -= Calc_DisplayFormatChanged;
						m_calc.Stack.CollectionChanged -= Calc_StackChanged;
					}

					m_calc = value;

					if (m_calc != null)
					{
						m_calc.DisplayFormatChanged += Calc_DisplayFormatChanged;
						m_calc.Stack.CollectionChanged += Calc_StackChanged;
					}
				}
			}
		}

		#endregion

		#region Public Events

		public event EventHandler ExecutedCommand;

		#endregion

		#region Internal Methods

		internal void EnsureTopOfStackIsVisible()
		{
			//The help for ScrollIntoView says to call this.  Without it,
			//a newly pushed item won't be able to scroll into view.
			//Note: This can update the number of display items.
			m_listBox.UpdateLayout();

			int numItems = m_displayItems.Count;
			if (numItems > 0)
			{
				//Make sure StackTop is scrolled into view.
				m_listBox.ScrollIntoView(m_displayItems[numItems - 1]);
			}
		}

		#endregion

		#region Private Event Handlers

		void DisplayStack_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateDummyItems();
		}

		void DisplayStack_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Force this to be recalculated using the current size.
			m_numberOfDisplayableItems = 0;

			UpdateDummyItems();
		}

		void Calc_DisplayFormatChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			foreach (DisplayStackItem item in m_displayItems)
			{
				item.RefreshDisplayValues();
			}
		}

		void Calc_StackChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					DisplayStackItem item = new DisplayStackItem(m_calc, (Value)e.NewItems[0], 0);
					m_displayItems.Add(item);
					ResetStackPositions();
					break;
				case NotifyCollectionChangedAction.Remove:
					m_displayItems.RemoveAt(m_displayItems.Count - 1);
					ResetStackPositions();
					break;
				case NotifyCollectionChangedAction.Replace:
				//A replace action shouldn't happen, so we'll just fall through to Reset.
				case NotifyCollectionChangedAction.Reset:
					ResetDisplayItems();
					break;
			}

			UpdateDummyItems();
			EnsureTopOfStackIsVisible();
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			ContextMenu menu = sender as ContextMenu;
			if (menu != null)
			{
				int index = m_listBox.SelectedIndex;
				int numDisplayItems = m_displayItems.Count;

				bool isDummyItem = true;
				if (index >= 0 && index < numDisplayItems)
				{
					isDummyItem = m_displayItems[index].IsDummyItem;
				}

				bool hasSelectedValue = !isDummyItem;

				//They can roll if they have a non-dummy item selected, and it's not
				//the last/bottom-most/lowest-numbered-stack-position item.
				bool canRollSelectedValue = hasSelectedValue && index < (numDisplayItems - 1);

				//Note: I originally tried binding IsEnabled to a property on the
				//DisplayStack, but I couldn't get SL4's binding to point to an
				//ancestor.
				//
				//Note 2: SL4 doesn't repaint correctly when IsEnabled changes
				//for context menu items.  It's a bug in the April 2010 Silverlight
				//Toolkit, and they now provide MenuItemIsEnabledWorkaround.
				//
				// Note 3: SL5 and the December 2011 Silverlight Toolkit still have
				// the same bug, so MenuItemIsEnabledWorkaround is still needed.
				foreach (MenuItem item in menu.Items.OfType<MenuItem>())
				{
					string tag = Convert.ToString(item.Tag, CultureInfo.CurrentCulture);
					if (!string.IsNullOrEmpty(tag))
					{
						if (tag.StartsWith("Roll", StringComparison.CurrentCulture))
						{
							item.IsEnabled = canRollSelectedValue;
						}
						else
						{
							item.IsEnabled = hasSelectedValue;
						}
					}
				}
			}
		}

		private void IndexCommand_Click(object sender, RoutedEventArgs e)
		{
			//Commands using this event handler expect to pass an offset
			//from StackTop, which is DisplayStackItem.Position.
			ExecuteCommandForSelectedPosition(sender as MenuItem, 0);
		}

		private void CountCommand_Click(object sender, RoutedEventArgs e)
		{
			//Commands using this event handler expect to pass a Count
			//of items in a range, which is DisplayStackItem.Position+1.
			ExecuteCommandForSelectedPosition(sender as MenuItem, 1);
		}

		private void ListBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
			{
				ExecuteCommandForSelectedPosition("CopyToClipboard", 0);
				e.Handled = true;
			}
		}

		#endregion

		#region Private Methods

		private void FinishInitialize()
		{
			Loaded += DisplayStack_Loaded;
			SizeChanged += DisplayStack_SizeChanged;

			//Hook the list box we're wrapping to our private list of DisplayStackItems.
			m_listBox.ItemsSource = m_displayItems;
		}

		private void UpdateDummyItems()
		{
			int minItems = Math.Max(1, m_numberOfDisplayableItems);
			while (m_displayItems.Count < minItems)
			{
				m_displayItems.Insert(0, new DisplayStackItem(m_calc, null, m_displayItems.Count));
			}
			while (m_displayItems.Count > minItems)
			{
				if (m_displayItems[0].IsDummyItem)
				{
					m_displayItems.RemoveAt(0);
				}
				else
				{
					//If we hit a real item, then we have to leave it and everything "below" it.
					break;
				}
			}

			if (m_numberOfDisplayableItems == 0)
			{
				//Silverlight rendering is asynchronous, so this will calculate on
				//another thread and then call back into here when finished.
				//
				//This is the last call in the current method, so I can avoid any
				//race conditions with the invoked method.
				Dispatcher.BeginInvoke((Action)CalculateNumberOfDisplayableItems);
			}

			//NOTE: I tried doing the main UpdateDummyItems logic in an
			//else block (i.e., only if numDispItems != 0), but that caused
			//more scrollbar flicker when the error message control popped
			//up and resized the stack.
		}

		private void CalculateNumberOfDisplayableItems()
		{
			if (this.IsInDesignMode())
			{
				// In the VS 2013 designer for WPF this method would occasionally throw a
				// NullReferenceException so we'll just use a hardcoded number of items.
				m_numberOfDisplayableItems = 15;
			}
			else
			{
				//Use a default size in case the display stack is empty
				//(i.e., there are no real or dummy items on it yet).
				//Typically, that won't happen because UpdateDummyItems
				//will always put in at least one item before it calls us.
				double averageItemHeight = 22;

				int numItems = m_displayItems.Count;
				if (numItems > 0)
				{
					double totalItemHeight = 0;
					for (int i = 0; i < numItems; i++)
					{
						ListBoxItem item = GetListBoxItem(i);
						double itemHeight = item.ActualHeight;
						totalItemHeight += itemHeight;
					}
					averageItemHeight = totalItemHeight / numItems;
				}

				//Now calculate how many items can fit into the list box's client height.
				Thickness pad = m_listBox.Padding;
				Thickness border = m_listBox.BorderThickness;
				double listBoxClientHeight = m_listBox.ActualHeight - pad.Top - pad.Bottom - border.Top - border.Bottom;
				int numberOfDisplayableItems = (int)(listBoxClientHeight / averageItemHeight);
				if (numberOfDisplayableItems != m_numberOfDisplayableItems)
				{
					m_numberOfDisplayableItems = numberOfDisplayableItems;

					//Since the number of displayable items changed, we may
					//need to update the number of dummy items we're using.
					Dispatcher.BeginInvoke((Action)UpdateDummyItems);
				}
			}
		}

		private ListBoxItem GetListBoxItem(int itemIndex)
		{
			ListBoxItem result = (ListBoxItem)(m_listBox.ItemContainerGenerator.ContainerFromIndex(itemIndex));
			return result;
		}

		private void ResetDisplayItems()
		{
			m_displayItems.Clear();

			//The stack gives us its items from StackTop to StackBottom,
			//which visually is from down to up, so we'll add them in reverse
			//order and number them by their stack index.
			ValueStack stack = m_calc.Stack;
			var values = stack.PeekRange(stack.Count);
			for (int i = values.Count-1; i >= 0; i--)
			{
				Value value = values[i];
				var item = new DisplayStackItem(m_calc, value, i);
				m_displayItems.Add(item);
			}
		}

		private void ResetStackPositions()
		{
			//The display items are ordered just like you see
			//them, so we have to update the positions in
			//reverse order to have "1:" at the bottom.
			int position = m_displayItems.Count;
			foreach (DisplayStackItem item in m_displayItems)
			{
				item.Position = --position;
			}
		}

		private void ExecuteCommandForSelectedPosition(MenuItem menuItem, int positionAdjustment)
		{
			if (menuItem != null && menuItem.Tag != null)
			{
				string command = menuItem.Tag.ToString();
				ExecuteCommandForSelectedPosition(command, positionAdjustment);
			}
		}

		private void ExecuteCommandForSelectedPosition(string command, int positionAdjustment)
		{
			int index = m_listBox.SelectedIndex;
			if (index >= 0 && index < m_displayItems.Count)
			{
				DisplayStackItem displayItem = m_displayItems[index];
				if (!displayItem.IsDummyItem)
				{
					int commandParameter = displayItem.Position + positionAdjustment;
					m_calc.ExecuteCommand(command, commandParameter);

					var eh = ExecutedCommand;
					if (eh != null)
					{
						eh(this, EventArgs.Empty);
					}
				}
			}
		}

		#endregion

		#region Private Data Members

		private Calculator m_calc;
		private ObservableCollection<DisplayStackItem> m_displayItems = new ObservableCollection<DisplayStackItem>();
		private int m_numberOfDisplayableItems;

		#endregion
	}
}
