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
            m_calc = calc;
        }

        #endregion

        #region Public Methods

        public void Scroll(bool scrollUp)
        {
            //If the calc's entry line isn't equal to the currently selected position,
            //then we'll re-use the currently selected position.
            bool atCurrentPosition = false;
            string currentCalcEntryLine = m_calc.EntryLine;
            if (m_position >= 0 && m_position < Count)
            {
                string currentPositionEntryLine = Items[m_position];
                atCurrentPosition = currentCalcEntryLine == currentPositionEntryLine;
            }

            if (scrollUp)
            {
                if ((atCurrentPosition || m_position < 0) && m_position < (Count - 1))
                {
                    m_position++;
                }
            }
            else
            {
                if ((atCurrentPosition || m_position >= Count) && m_position > 0)
                {
                    m_position--;
                }
            }

            if (m_position >= 0 && m_position < Count)
            {
                string newEntryLine = Items[m_position];
                m_calc.EntryLine = newEntryLine;
            }
        }

        public void Clear()
        {
            //Clear the position first, so it will already be
            //reset when the XxxChanged events fire.
            m_position = c_initialPosition;
            Items.Clear();
        }

        #endregion

        #region Internal Methods

        internal void Add(string entryLine)
        {
            //Insert the new entry line, replacing any old instance
            //of it and moving it to the front of the list.
            int previousIndex = Items.IndexOf(entryLine);
            if (previousIndex > 0)
            {
                Items.RemoveAt(previousIndex);
            }
            if (previousIndex != 0)
            {
                Items.Insert(0, entryLine);
            }

            //Reset the position.  This isn't strictly necessary,
            //but it seems like the right thing to do since we're
            //moving the most recent entry to the front of the
            //history.  Once it's moved, there's little point in
            //keeping the position where the item used to be.
            m_position = c_initialPosition;

            //Purge old items, so the history doesn't grow forever.
            RemoveOldItems();
        }

        internal void Load(INode historyNode)
        {
            Clear();

            if (historyNode != null)
            {
                foreach (INode entryLineNode in historyNode.GetNodes())
                {
                    string entryLine = entryLineNode.GetValue("Text", null);
                    if (!string.IsNullOrEmpty(entryLine))
                    {
                        Items.Add(entryLine);
                    }
                }

                m_position = historyNode.GetValue("Position", c_initialPosition);
            }
        }

        internal void Save(INode historyNode)
        {
            historyNode.SetValue("Position", m_position);
			int index = 1;
            foreach (string entryLine in Items)
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
            while (Items.Count > c_maxHistoryItems)
            {
                Items.RemoveAt(Items.Count - 1);
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
