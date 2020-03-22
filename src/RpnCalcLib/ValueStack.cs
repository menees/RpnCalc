namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Linq;

	#endregion

#pragma warning disable CA1710 // Identifiers should have correct suffix. Stack is a better suffix than Collection for this type.
	public sealed class ValueStack : IEnumerable<Value>, ICollection, ICollection<Value>, INotifyCollectionChanged
#pragma warning restore CA1710 // Identifiers should have correct suffix
	{
		#region Private Data Members

		private readonly ObservableCollection<Value> storage = new ObservableCollection<Value>();
		private int resetLevel;

		#endregion

		#region Constructors

		public ValueStack()
		{
			this.storage.CollectionChanged += this.Storage_CollectionChanged;
		}

		#endregion

		#region INotifyCollectionChanged Members

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		#region Public Properties

		public int Count
		{
			get
			{
				return this.storage.Count;
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

		// We only support ICollection<T> for reading not writing.
		bool ICollection<Value>.IsReadOnly => true;

		#endregion

		#region Private Properties

		private int TopIndex
		{
			get
			{
				return this.Count - 1;
			}
		}

		#endregion

		#region Public Methods

		public void Push(Value value)
		{
			this.storage.Add(value);
		}

		public void PushRange(IEnumerable<Value> values)
		{
			foreach (Value value in values)
			{
				this.storage.Add(value);
			}
		}

		public Value Pop()
		{
			Value result = this.Peek();
			this.storage.RemoveAt(this.TopIndex);
			return result;
		}

		public IList<Value> PopRange(int count)
		{
			IList<Value> result = this.PeekRange(count);
			for (int i = 0; i < count; i++)
			{
				this.storage.RemoveAt(this.TopIndex);
			}

			return result;
		}

		public Value Peek()
		{
			Value result = this.storage[this.TopIndex];
			return result;
		}

		public IList<Value> PeekRange(int count)
		{
			IList<Value> result = new List<Value>(count);
			int topIndex = this.TopIndex;
			for (int i = 0; i < count; i++)
			{
				result.Add(this.storage[topIndex - i]);
			}

			return result;
		}

		public Value PeekAt(int offsetFromTop)
		{
			Value result = this.storage[this.TopIndex - offsetFromTop];
			return result;
		}

		#endregion

		#region IEnumerable<Value> and ICollection Members

		public IEnumerator<Value> GetEnumerator()
		{
			// Return the values in reverse order, from top to bottom.
			// That's what Stack<T> does, and it kind of makes sense.
			for (int i = this.TopIndex; i >= 0; i--)
			{
				Value result = this.storage[i];
				yield return result;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public void CopyTo(Value[] array, int index)
		{
			ICollection storageColl = this.storage;
			storageColl.CopyTo(array, index);

			// Return the values in reverse order like GetEnumerator does.
			Array.Reverse(array, index, this.Count);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			this.CopyTo((Value[])array, index);
		}

		void ICollection<Value>.Add(Value item) => throw new NotSupportedException();

		void ICollection<Value>.Clear() => throw new NotSupportedException();

		bool ICollection<Value>.Contains(Value item) => this.storage.Contains(item);

		bool ICollection<Value>.Remove(Value item) => throw new NotSupportedException();

		#endregion

		#region Internal Methods

		internal void Load(INode stackNode, Calculator calc)
		{
			this.BeginReset();
			try
			{
				this.storage.Clear();

				if (stackNode != null)
				{
					// Save saved the values out in the order we need to
					// re-push them, so this part is easy.  Just parse and push.
					foreach (INode valueNode in stackNode.GetNodes())
					{
						// This will return null if one of the values can't be reloaded.
						// That can happen if a regional formatting change was made
						// so that a previously saved value can no longer be parsed.
						// Or a user could edit the saved file in isolated storage to
						// put crapola in a value.
						Value val = Value.Load(valueNode, calc);
						if (val != null)
						{
							this.Push(val);
						}
					}
				}
			}
			finally
			{
				this.EndReset();
			}
		}

		internal void Save(INode stackNode, Calculator calc)
		{
			// Save the items out in reverse order.  That way they'll be
			// saved in the order that we need to re-push them during
			// Load, and they'll be in saved in the same order that the
			// display showed them.
			var values = this.PeekRange(this.Count).Reverse();
			int index = this.Count;
			foreach (Value val in values)
			{
				INode valueNode = stackNode.GetNode(nameof(Value) + index--, true);
				val.Save(valueNode, calc);
			}
		}

		#endregion

		#region Private Methods

		private void Storage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Only send notifications if we're not in the middle of a reset operation.
			if (this.resetLevel == 0)
			{
				var eh = this.CollectionChanged;
				if (eh != null)
				{
					// Always report the changed index as 0 since they can only modify the top.
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
			this.resetLevel++;
		}

		private void EndReset()
		{
			this.resetLevel--;
			if (this.resetLevel == 0)
			{
				this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}

		#endregion
	}
}
