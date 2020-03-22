#region Using Directives

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace Menees.RpnCalc.Internal
{
	internal class EntryCommands : Commands
	{
		#region Constructors

		public EntryCommands(Calculator calc, StackCommands stackCommands)
			: base(calc)
		{
			this.stackCommands = stackCommands;
		}

		#endregion

		#region Public Methods

		public void AppendToEntryLine(Command cmd, int offsetFromTop)
		{
			this.RequireArgs(offsetFromTop + 1);

			// We can't call UseTopValues() or it will pop everything off
			// when we Commit.  So we'll just Peek here and manually
			// set the LastArgs below.
			var value = this.Stack.PeekAt(offsetFromTop);

			// I'm using GetEntryValue here even though its text may not
			// match what's displayed exactly (e.g., GetEntryValue always
			// returns the full precision for a double value).  This is necessary
			// because some types like Fractions and DateTimes use different
			// entry formats than display formats.  We normally don't want the
			// display formats appended to the entry line.  (If someone really
			// wants that they can use Copy To Clipboard followed by Paste.)
			// With the current behavior, a user can immediately hit Enter,
			// and the new value will be parsed and pushed correctly.
			string valueText = value.GetEntryValue(this.Calc);

			string entryLine = this.Calc.EntryLine;
			if (string.IsNullOrEmpty(entryLine))
			{
				this.Calc.EntryLine = valueText;
			}
			else
			{
				this.Calc.EntryLine += " " + valueText;
			}

			cmd.Commit();
			cmd.SetLastArgs(new[] { value });
		}

		public void Edit(Command cmd)
		{
			string entryLine = this.Calc.EntryLine;
			if (string.IsNullOrEmpty(entryLine))
			{
				this.RequireArgs(1);
				var value = cmd.UseTopValue();
				this.Calc.EntryLine = value.GetEntryValue(this.Calc);
				cmd.Commit();
			}
			else
			{
				// If there's already something on the entry line,
				// then the user is already editing something, so
				// cancel this command.
				cmd.Cancel();
			}
		}

		public EntryLineParser Enter(Command cmd)
		{
			string entryLine = this.Calc.EntryLine;
			if (string.IsNullOrEmpty(entryLine))
			{
				// If they hit Enter with an empty entry line, then duplicate the top item.
				this.stackCommands.Dup(cmd);

				return null;
			}
			else
			{
				EntryLineParser parser = new EntryLineParser(this.Calc, entryLine);
				if (parser.HasError)
				{
					this.Calc.ErrorMessage = parser.ErrorMessage;
					cmd.Cancel();
				}
				else if (parser.IsEntryLineComplete)
				{
					this.Calc.EntryLine = null;

					// Push the values to the stack (which allows them to be value
					// type reduced if necessary), but then cancel the Enter command.
					// This will preserve the LastArgs from the previous command.
					// That's always preferable to having the LastArgs from the Enter
					// command since they're null and useless.  This is also important
					// because every command does an implicit Enter through the UI
					// (including the "Last" command), and we don't want implicitly
					// entered values to blow away the LastArgs (especially if "Last"
					// is the command we're trying to execute)!
					cmd.PushResults(CommandState.Cancelled, parser.Values.ToArray());
				}

				// Save the entry line in the history regardless of whether there was an error.
				this.Calc.EntryLineHistory.Add(entryLine);

				return parser;
			}
		}

		public void Last(Command cmd)
		{
			this.Calc.PushLastArgs();

			// Call cancel so this command won't be stored as the last command
			// (since that would blow away the previous command's LastArgs).
			cmd.Cancel();
		}

		#endregion

		#region Private Data Members

		private StackCommands stackCommands;

		#endregion
	}
}
