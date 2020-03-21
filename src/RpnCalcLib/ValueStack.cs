#region Using Directives

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;

#endregion

namespace Menees.RpnCalc
{
    public sealed class ValueStack : IEnumerable<Value>, ICollection, INotifyCollectionChanged
    {
        #region Constructors

        public ValueStack()
        {
            m_storage.CollectionChanged += Storage_CollectionChanged;
        }

        #endregion

        #region Public Methods

        public void Push(Value value)
        {
            m_storage.Add(value);
        }

        public void PushRange(IEnumerable<Value> values)
        {
            foreach (Value value in values)
            {
                m_storage.Add(value);
            }
        }

        public Value Pop()
        {
            Value result = Peek();
            m_storage.RemoveAt(TopIndex);
            return result;
        }

        public IList<Value> PopRange(int count)
        {
            IList<Value> result = PeekRange(count);
            for (int i = 0; i < count; i++)
            {
                m_storage.RemoveAt(TopIndex);
            }
            return result;
        }

        public Value Peek()
        {
            Value result = m_storage[TopIndex];
            return result;
        }

        public IList<Value> PeekRange(int count)
        {
            IList<Value> result = new List<Value>(count);
            int topIndex = TopIndex;
            for (int i = 0; i < count; i++)
            {
                result.Add(m_storage[topIndex - i]);
            }
            return result;
        }

        public Value PeekAt(int offsetFromTop)
        {
            Value result = m_storage[TopIndex - offsetFromTop];
            return result;
        }

        #endregion

        #region IEnumerable<Value> and ICollection Members

        public IEnumerator<Value> GetEnumerator()
        {
            //Return the values in reverse order, from top to bottom.
            //That's what Stack<T> does, and it kind of makes sense.
            for (int i = TopIndex; i >= 0; i--)
            {
                Value result = m_storage[i];
                yield return result;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Value[] array, int index)
        {
            ICollection storageColl = m_storage;
            storageColl.CopyTo(array, index);
            //Return the values in reverse order like GetEnumerator does.
            Array.Reverse(array, index, Count);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            CopyTo((Value[])array, index);
        }

        public int Count
        {
            get
            {
                return m_storage.Count;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Internal Methods

        internal void Load(INode stackNode, Calculator calc)
        {
            BeginReset();
            try
            {
                m_storage.Clear();

                if (stackNode != null)
                {
                    //Save saved the values out in the order we need to
                    //re-push them, so this part is easy.  Just parse and push.
                    foreach (INode valueNode in stackNode.GetNodes())
                    {
                        //This will return null if one of the values can't be reloaded.
                        //That can happen if a regional formatting change was made
                        //so that a previously saved value can no longer be parsed.
                        //Or a user could edit the saved file in isolated storage to
                        //put crapola in a value.
                        Value val = Value.Load(valueNode, calc);
                        if (val != null)
                        {
                            Push(val);
                        }
                    }
                }
            }
            finally
            {
                EndReset();
            }
        }

        internal void Save(INode stackNode, Calculator calc)
        {
            //Save the items out in reverse order.  That way they'll be
            //saved in the order that we need to re-push them during
            //Load, and they'll be in saved in the same order that the
            //display showed them.
            var values = PeekRange(Count).Reverse();
			int index = Count;
            foreach (Value val in values)
            {
				INode valueNode = stackNode.GetNode("Value" + index--, true);
				val.Save(valueNode, calc);
            }
        }

        #endregion

        #region Private Properties

        private int TopIndex
        {
            get
            {
                return Count - 1;
            }
        }

        #endregion

        #region Private Methods

        private void Storage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //Only send notifications if we're not in the middle of a reset operation.
            if (m_resetLevel == 0)
            {
                var eh = CollectionChanged;
                if (eh != null)
                {
                    //Always report the changed index as 0 since they can only modify the top.
                    const int c_index = 0;
                    var newItem = e.NewItems != null && e.NewItems.Count > 0 ? e.NewItems[0] : null;
                    var oldItem = e.OldItems != null && e.OldItems.Count > 0 ? e.OldItems[0] : null;
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            eh(this, new NotifyCollectionChangedEventArgs(e.Action, newItem, c_index));
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            eh(this, new NotifyCollectionChangedEventArgs(e.Action, oldItem, c_index));
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            eh(this, new NotifyCollectionChangedEventArgs(e.Action, newItem, oldItem, c_index));
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            eh(this, new NotifyCollectionChangedEventArgs(e.Action));
                            break;
                    }
                }
            }
        }

        private void BeginReset()
        {
            m_resetLevel++;
        }

        private void EndReset()
        {
            m_resetLevel--;
            if (m_resetLevel == 0)
            {
                var eh = CollectionChanged;
                if (eh != null)
                {
                    eh(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        #endregion

        #region Private Data Members

        private ObservableCollection<Value> m_storage = new ObservableCollection<Value>();
        private int m_resetLevel;

        #endregion
    }
}
