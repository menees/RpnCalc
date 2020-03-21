#region Using Directives

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using Menees.RpnCalc.Internal;

#endregion

namespace Menees.RpnCalc
{
	public sealed class EntryLineHistoryCollection : ReadOnlyObservableCollection<string>
	{
		#region Constructors

		internal EntryLineHistoryCollection(Calculator calc)
			: base(new ObservableCollection<string>())
		{
			this.m_calc = calc;
		}

		#endregion

		#region Public Methods

		public void Scroll(bool scrollUp)
		{
			// If the calc's entry line isn't equal to the currently selected position,
			// then we'll re-use the currently selected position.
			bool atCurrentPosition = false;
			string currentCalcEntryLine = this.m_calc.EntryLine;
			if (this.m_position >= 0 && this.m_position < this.Count)
			{
				string currentPositionEntryLine = this.Items[this.m_position];
				atCurrentPosition = currentCalcEntryLine == currentPositionEntryLine;
			}

			if (scrollUp)
			{
				if ((atCurrentPosition || this.m_position < 0) && this.m_position < (this.Count - 1))
				{
					this.m_position++;
				}
			}
			else
			{
				if ((atCurrentPosition || this.m_position >= this.Count) && this.m_position > 0)
				{
					this.m_position--;
				}
			}

			if (this.m_position >= 0 && this.m_position < this.Count)
			{
				string newEntryLine = this.Items[this.m_position];
				this.m_calc.EntryLine = newEntryLine;
			}
		}

		public void Clear()
		{
			// Clear the position first, so it will already be
			// reset when the XxxChanged events fire.
			this.m_position = c_initialPosition;
			this.Items.Clear();
		}

		#endregion

		#region Internal Methods

		internal void Add(string entryLine)
		{
			// Insert the new entry line, replacing any old instance
			// of it and moving it to the front of the list.
			int previousIndex = this.Items.IndexOf(entryLine);
			if (previousIndex > 0)
			{
				this.Items.RemoveAt(previousIndex);
			}

			if (previousIndex != 0)
			{
				this.Items.Insert(0, entryLine);
			}

			// Reset the position.  This isn't strictly necessary,
			// but it seems like the right thing to do since we're
			// moving the most recent entry to the front of the
			// history.  Once it's moved, there's little point in
			// keeping the position where the item used to be.
			this.m_position = c_initialPosition;

			// Purge old items, so the history doesn't grow forever.
			this.RemoveOldItems();
		}

		internal void Load(INode historyNode)
		{
			this.Clear();

			if (historyNode != null)
			{
				foreach (INode entryLineNode in historyNode.GetNodes())
				{
					string entryLine = entryLineNode.GetValue("Text", null);
					if (!string.IsNullOrEmpty(entryLine))
					{
						this.Items.Add(entryLine);
					}
				}

				this.m_position = historyNode.GetValue("Position", c_initialPosition);
			}
		}

		internal void Save(INode historyNode)
		{
			historyNode.SetValue("Position", this.m_position);
			int index = 1;
			foreach (string entryLine in this.Items)
			{
				INode entryLineNode = historyNode.GetNode("EntryLine" + index++, true);
				entryLineNode.SetValue("Text", entryLine);
			}
		}

		#endregion

		#region Private Methods

		private bool RemoveOldItems()
		{
			bool result = false;

			const int c_maxHistoryItems = 10;
			while (this.Items.Count > c_maxHistoryItems)
			{
				this.Items.RemoveAt(this.Items.Count - 1);
				result = true;
			}

			return result;
		}

		#endregion

		#region Private Data Members

		private Calculator m_calc;
		private int m_position = c_initialPosition;

		private const int c_initialPosition = -1;

		#endregion
	}
}
