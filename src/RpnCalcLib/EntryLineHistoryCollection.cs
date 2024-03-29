﻿namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using Menees.RpnCalc.Internal;

	#endregion

	public sealed class EntryLineHistoryCollection : ReadOnlyObservableCollection<string>
	{
		#region Private Data Members

		private const int DefaultInitialPosition = -1;

		private readonly Calculator calc;
		private int position = DefaultInitialPosition;

		#endregion

		#region Constructors

		internal EntryLineHistoryCollection(Calculator calc)
			: base(new ObservableCollection<string>())
		{
			this.calc = calc;
		}

		#endregion

		#region Public Methods

		public void Scroll(bool scrollUp)
		{
			// If the calc's entry line isn't equal to the currently selected position,
			// then we'll re-use the currently selected position.
			bool atCurrentPosition = false;
			string? currentCalcEntryLine = this.calc.EntryLine;
			if (this.position >= 0 && this.position < this.Count)
			{
				string currentPositionEntryLine = this.Items[this.position];
				atCurrentPosition = currentCalcEntryLine == currentPositionEntryLine;
			}

			if (scrollUp)
			{
				if ((atCurrentPosition || this.position < 0) && this.position < (this.Count - 1))
				{
					this.position++;
				}
			}
			else
			{
				if ((atCurrentPosition || this.position >= this.Count) && this.position > 0)
				{
					this.position--;
				}
			}

			if (this.position >= 0 && this.position < this.Count)
			{
				string newEntryLine = this.Items[this.position];
				this.calc.EntryLine = newEntryLine;
			}
		}

		public void Clear()
		{
			// Clear the position first, so it will already be
			// reset when the XxxChanged events fire.
			this.position = DefaultInitialPosition;
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
			this.position = DefaultInitialPosition;

			// Purge old items, so the history doesn't grow forever.
			this.RemoveOldItems();
		}

		internal void Load(INode? historyNode)
		{
			this.Clear();

			if (historyNode != null)
			{
				foreach (INode entryLineNode in historyNode.GetNodes())
				{
					string? entryLine = entryLineNode.GetValueN("Text", null);
					if (entryLine.IsNotEmpty())
					{
						this.Items.Add(entryLine);
					}
				}

				this.position = historyNode.GetValue("Position", DefaultInitialPosition);
			}
		}

		internal void Save(INode historyNode)
		{
			historyNode.SetValue("Position", this.position);
			int index = 1;
			foreach (string entryLine in this.Items)
			{
				INode entryLineNode = historyNode.GetNode("EntryLine" + index++);
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
	}
}
