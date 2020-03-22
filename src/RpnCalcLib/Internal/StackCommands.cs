#region Using Directives

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Security;

#endregion

namespace Menees.RpnCalc.Internal
{
	internal class StackCommands : Commands
	{
		#region Constructors

		public StackCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Public Methods

		public void Clear(Command cmd)
		{
			int count = this.Stack.Count;
			if (count > 0)
			{
				cmd.UseTopValues(count);
				cmd.Commit();
			}
			else
			{
				// Don't report an error if the user clicks Clear
				// on an empty stack, AND don't clear the previous
				// command because we might as well keep its
				// LastArgs.
				cmd.Cancel();
			}
		}

		public void CopyToClipboard(Command cmd, int offsetFromTop)
		{
			Value value = this.Stack.PeekAt(offsetFromTop);
			string valueText = value.ToString(this.Calc);
			try
			{
				Utility.SetClipboardText(valueText);
			}
			catch (SecurityException)
			{
				// One of two things happened.  We either invoked this method
				// from outside a user-initiated context (e.g., not in response to
				// a Control.Click event), or the Clipboard access user dialog box
				// was not confirmed.  The first case is unlikely if I programmed
				// things right, so I'm assuming we're in the second case.  If the
				// user said don't allow access immediately after they selected the
				// "Copy To Clipboard" command, then we might as well eat the
				// exception.
			}

			// Since this command didn't pop or push or change anything,
			// we might as well cancel it, so we preserve the LastArgs from
			// the previous command.
			cmd.Cancel();
		}

		public void Drop(Command cmd)
		{
			if (this.Stack.Count > 0)
			{
				this.DropN(cmd, 1);
			}
			else
			{
				// Ignore it if the user hits Drop on an empty stack.
				// I do it a lot, and I hated that error on the HP48.
				// This will also preserve the previous LastArgs.
				cmd.Cancel();
			}
		}

		public void Drop2(Command cmd)
		{
			this.DropN(cmd, 2);
		}

		public void DropN(Command cmd)
		{
			int count = this.GetTopItemAsCount();

			// Pass N+1 since we have to remove the count arg too.
			this.DropN(cmd, count + 1);
		}

		public void DropN(Command cmd, int count)
		{
			this.RequireArgs(count);
			cmd.UseTopValues(count);
			cmd.Commit();
		}

		public void Dup(Command cmd)
		{
			if (this.Stack.Count > 0)
			{
				this.DupN(cmd, 1);
			}
			else
			{
				// Don't throw an error if the stack is empty.  I hated it when the
				// HP48 did that.  Just cancel because we don't want to blow away
				// the LastArgs from the previous command.
				cmd.Cancel();
			}
		}

		public void Dup2(Command cmd)
		{
			this.DupN(cmd, 2);
		}

		public void DupN(Command cmd)
		{
			int count = this.GetTopItemAsCount();

			// Require N+1 items, but skip duplicating the first item.
			this.DupN(cmd, count + 1, 1);
		}

		public void DupN(Command cmd, int count)
		{
			this.RequireArgs(count);
			this.DupN(cmd, count, 0);
		}

		public void KeepN(Command cmd)
		{
			int count = this.GetTopItemAsCount();

			// Require N+1 items, but skip keeping the first item.
			this.KeepN(cmd, count + 1, 1);
		}

		public void KeepN(Command cmd, int count)
		{
			this.KeepN(cmd, count, 0);
		}

		public void Pick(Command cmd)
		{
			this.RequireArgs(1);
			int displayStackPosition = this.GetTopItemAsInteger();
			RequirePositiveStackPosition(displayStackPosition);
			this.RequireArgs(displayStackPosition + 1);
			cmd.UseTopValues(1);

			// Stack is 0-based, and displayStackPosition is 1-based.
			// But the stack currently includes the offset item, so this
			// works out.
			var value = this.Stack.PeekAt(displayStackPosition);
			cmd.Commit(value);
		}

		public void Pick(Command cmd, int offsetFromTop)
		{
			this.RequireArgs(offsetFromTop + 1);
			var value = this.Stack.PeekAt(offsetFromTop);
			cmd.Commit(value);
		}

		public void Remove(Command cmd, int offsetFromTop)
		{
			if (offsetFromTop == 0)
			{
				this.Drop(cmd);
			}
			else
			{
				this.RequireArgs(offsetFromTop + 1);

				// Internally, pop the values we want to keep.
				var valuesToKeep = this.Stack.PopRange(offsetFromTop);

				// Then call UseTopValues, so the command will pop the
				// item we want to remove and store it in LastArgs.
				cmd.UseTopValues(1);

				// Then re-push the values we want to keep.
				cmd.Commit(valuesToKeep.Reverse().ToArray());
			}
		}

		public void RollDownN(Command cmd)
		{
			int count = this.GetTopItemAsCount();

			// Require N+1 items, but skip the first item.
			this.RollN(cmd, count + 1, 1, false);
		}

		public void RollDownN(Command cmd, int count)
		{
			this.RollN(cmd, count, 0, false);
		}

		public void RollUp3(Command cmd)
		{
			this.RollN(cmd, 3, 0, true);
		}

		public void RollUpN(Command cmd)
		{
			int count = this.GetTopItemAsCount();

			// Require N+1 items, but skip the first item.
			this.RollN(cmd, count + 1, 1, true);
		}

		public void RollUpN(Command cmd, int count)
		{
			this.RollN(cmd, count, 0, true);
		}

		public void SigmaN(Command cmd)
		{
			int count = this.GetTopItemAsCount();
			this.SigmaN(cmd, count + 1, 1);
		}

		public void SigmaN(Command cmd, int count)
		{
			this.SigmaN(cmd, count, 0);
		}

		public void SortN(Command cmd)
		{
			int count = this.GetTopItemAsCount();
			this.SortN(cmd, count + 1, 1);
		}

		public void SortN(Command cmd, int count)
		{
			this.SortN(cmd, count, 0);
		}

		public void Swap(Command cmd)
		{
			this.RequireArgs(2);
			var args = cmd.UseTopValues(2);
			cmd.Commit(args[0], args[1]);
		}

		#endregion

		#region Private Methods

		private int GetTopItemAsInteger()
		{
			this.RequireType(0, RpnValueType.Integer);
			IntegerValue countValue = (IntegerValue)this.Stack.PeekAt(0);
			int result = (int)countValue.AsInteger;
			return result;
		}

		private int GetTopItemAsCount()
		{
			this.RequireArgs(1);
			int result = this.GetTopItemAsInteger();
			RequireNonNegativeCount(result);
			return result;
		}

		private void DupN(Command cmd, int dupCount, int skipCount)
		{
			this.RequireArgs(dupCount);

			// Just peek at everything, so all the values won't get popped.
			var lastArgs = this.Stack.PeekRange(dupCount);

			// Call UseTopValues so that Commit will pop the number
			// we're supposed to skip.
			cmd.UseTopValues(skipCount);

			var values = lastArgs.Skip(skipCount).ToArray();
			Array.Reverse(values);

			// Pop the items we said to skip (just the item count if any),
			// and then push the values we're supposed to duplicate.
			cmd.Commit(values);

			// Manually set the last args to include everything we used.
			cmd.SetLastArgs(lastArgs);
		}

		private void KeepN(Command cmd, int keepCount, int skipCount)
		{
			this.RequireArgs(keepCount);

			// Use all the values on the stack, so they'll all be popped by Commit.
			var allValues = cmd.UseTopValues(this.Stack.Count);

			// We'll re-push the first N, so get them in reverse order.
			var keepValues = allValues.Take(keepCount).Skip(skipCount).Reverse().ToArray();

			// We'll set the LastArgs to the stack arg count (if any) plus the removed values.
			var lastArgValues = allValues.Take(skipCount).Concat(allValues.Skip(keepCount)).ToList();

			cmd.Commit(keepValues);
			cmd.SetLastArgs(lastArgValues);
		}

		private void RollN(Command cmd, int rollCount, int skipCount, bool rollUp)
		{
			this.RequireArgs(rollCount);

			// If the count came from the stack, then that's all we need to store for LastArgs.
			var lastArgs = this.Stack.PeekRange(skipCount);

			// Call UseTopValues so that Commit will pop the correct number
			// of values, then skip the count arg if necessary.
			var values = cmd.UseTopValues(rollCount).Skip(skipCount).ToList();

			// A user may have said N = 0, so make sure we have values to roll.
			if (values.Count > 0)
			{
				// Roll the specified item.
				int insertAt, removeAt;
				if (rollUp)
				{
					insertAt = 0;
					removeAt = values.Count - 1;
				}
				else
				{
					insertAt = values.Count - 1;
					removeAt = 0;
				}

				var rolledValue = values[removeAt];
				values.RemoveAt(removeAt);
				values.Insert(insertAt, rolledValue);
				values.Reverse();
			}

			cmd.Commit(values.ToArray());

			cmd.SetLastArgs(lastArgs);
		}

		private void SortN(Command cmd, int requiredArgCount, int skipCount)
		{
			this.RequireArgs(requiredArgCount);

			// If the count came from the stack, then that's all we need to store for LastArgs.
			var lastArgs = this.Stack.PeekRange(skipCount);

			// Call UseTopValues so that Commit will pop the correct number
			// of values, then skip the count arg if necessary.
			var values = cmd.UseTopValues(requiredArgCount).Skip(skipCount).ToList();

			try
			{
				// This can fail if all the args aren't implicitly convertible
				// to compatible types.  For example, this will fail if an
				// Integer and a TimeSpan are in the list.
				values.Sort((x, y) => Value.Compare(x, y, this.Calc));
			}
			catch (InvalidOperationException ex)
			{
				// If the SL Dev Runtime isn't installed, then the original exception won't
				// contain an error message because the standard (non-dev) SL runtime
				// doesn't include all of the exception message resources.  So we have to
				// wrap the original exception with our own, so we can always display a
				// message to the user if the sort fails.
				// See Calculator.SetError(Exception) for more info.
				// http://www.microsoft.com/GetSilverlight/resources/readme.aspx?v=2.0+target
				throw new InvalidOperationException(Resources.StackCommands_UnableToCompare, ex);
			}

			cmd.Commit(values.ToArray());

			cmd.SetLastArgs(lastArgs);
		}

		private void SigmaN(Command cmd, int requiredArgCount, int skipCount)
		{
			this.RequireArgs(requiredArgCount);
			var values = cmd.UseTopValues(requiredArgCount).Skip(skipCount).ToList();
			Value result;
			int count = values.Count;
			if (count >= 1)
			{
				result = values[0];
				for (int i = 1; i < count; i++)
				{
					result = Value.Add(result, values[i], this.Calc);
				}
			}
			else
			{
				result = new IntegerValue(0);
			}

			cmd.Commit(result);
		}

		#endregion
	}
}
