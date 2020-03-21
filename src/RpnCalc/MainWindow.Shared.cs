namespace Menees.RpnCalc
{
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
	using System.Text;
	using System.Diagnostics;
	using System.Reflection;
	using System.Security;
	using System.Globalization;

	#endregion

	public partial class MainWindow
	{
		#region Private Types

		private enum ShortcutKey
		{
			None,
			Enter,
			Back,
			Negate,
			Up,
			Down,
			Swap,
			SquareRoot,
			Power,

			// These are for when we need to insert the character only
			// and not invoke the associated arithmetic operation.
			Dash,
			ForwardSlash,

			// Add..Divide are grouped together for ease
			// of comparison in GetShortcutKey.
			Add,
			Subtract,
			Multiply,
			Divide,
		}

		#endregion

		#region Private Properties

		private bool HasEntryLineText
		{
			get
			{
				// We don't really have to call this.GetEntryLineTextBox() here.
				bool result = !string.IsNullOrEmpty(this.m_entryLine.Text);
				return result;
			}
		}

		#endregion

		#region Private Event Handlers

		private void ErrorInfoButton_Click(object sender, RoutedEventArgs e)
		{
			this.ClearError();
			this.m_entryLine.Focus();
		}

		private void Command_Click(object sender, RoutedEventArgs e)
		{
			// Most commands come to this event handler.
			Control control = sender as Control;
			if (control != null)
			{
				string commandName = control.Tag as string;
				if (!string.IsNullOrEmpty(commandName))
				{
					this.ExecuteCommand(commandName);
				}
			}
		}

		private void Enter_Click(object sender, RoutedEventArgs e)
		{
			// This is special because the Enter key never needs an implicit enter,
			// and if Enter gets an error we need to highlight the error.
			this.HandleEnter();
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			// This is special because the Back "command" actually
			// operates on the entry line TextBox not the bound value.
			this.HandleBack();
		}

		private void Negate_Click(object sender, RoutedEventArgs e)
		{
			// This is special because sometimes we need to negate
			// a value on the entry line as text without "entering" it.
			this.HandleNegate();
		}

		private void Subtract_Click(object sender, RoutedEventArgs e)
		{
			// This is special because in some modes we need to allow
			// the '-' key to be treated as text and not an operator.
			this.HandleSubtract();
		}

		private void DisplayChar_Click(object sender, RoutedEventArgs e)
		{
			this.ClearError();

			ContentControl control = sender as ContentControl;
			if (control != null)
			{
				string text = control.Content as string;
				if (string.IsNullOrEmpty(text))
				{
					// If the content wasn't text, then try the Tag.
					// This is necessary for keys like "Space" that
					// use images as content.
					text = control.Tag as string;
				}

				if (!string.IsNullOrEmpty(text))
				{
					this.InsertInEntryLine(text);
				}
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			// Pressing any key should clear the error (like on the HP48).
			this.ClearError();

			Key key = this.TranslateKey(e);
			ShortcutKey shortcutKey = this.GetShortcutKey(key, Keyboard.Modifiers);
			switch (shortcutKey)
			{
				case ShortcutKey.Add:
					this.ExecuteCommand("Add");
					break;
				case ShortcutKey.Back:
					this.HandleBack();
					break;
				case ShortcutKey.Divide:
					this.ExecuteCommand("Divide");
					break;
				case ShortcutKey.Enter:
					this.HandleEnter();
					break;
				case ShortcutKey.Multiply:
					this.ExecuteCommand("Multiply");
					break;
				case ShortcutKey.Negate:
					this.HandleNegate();
					break;
				case ShortcutKey.Subtract:
					this.HandleSubtract();
					break;
				case ShortcutKey.Up:
					this.ScrollEntryLineHistory(true);
					break;
				case ShortcutKey.Down:
					this.ScrollEntryLineHistory(false);
					break;
				case ShortcutKey.Dash:
					this.InsertInEntryLine("-");
					break;
				case ShortcutKey.ForwardSlash:
					this.InsertInEntryLine("/");
					break;
				case ShortcutKey.Power:
					this.ExecuteCommand("Power");
					break;
				case ShortcutKey.SquareRoot:
					this.ExecuteCommand("Sqrt");
					break;
				case ShortcutKey.Swap:
					this.ExecuteCommand("Swap");
					break;
			}

			e.Handled = shortcutKey != ShortcutKey.None;
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			// Pressing any key should clear the error (like on the HP48).
			// However, the TextBox eats the KeyDown event for some
			// keys such as the left and right arrow keys and the backspace
			// key when the text is non-empty.  So we have to clear the
			// error when the key comes up instead.
			//
			// Note: KeyDown causes commands to execute, and they
			// can raise errors.  So we can't clear the error here for any
			// keys that cause commands to execute, otherwise we'd
			// immediately clear the error when the key came up.
			//
			// Technically, Back executes the "Drop" command when the
			// entry line is empty, but I coded Drop to not raise an error
			// for the common case of dropping while the stack is empty.
			switch (e.Key)
			{
				case Key.Left:
				case Key.Right:
				case Key.Back:
					this.ClearError();
					break;
			}
		}

		private void EntryLineCut_Click(object sender, RoutedEventArgs e)
		{
			string selectedText = this.GetEntryLineTextBox().SelectedText;
			if (!string.IsNullOrEmpty(selectedText))
			{
				try
				{
					Clipboard.SetText(selectedText);
					this.InsertInEntryLine(string.Empty);
				}
				catch (SecurityException)
				{
					// The user disallowed the clipboard operation.
				}
			}
		}

		private void EntryLineCopy_Click(object sender, RoutedEventArgs e)
		{
			string selectedText = this.GetEntryLineTextBox().SelectedText;
			if (!string.IsNullOrEmpty(selectedText))
			{
				try
				{
					Clipboard.SetText(selectedText);
				}
				catch (SecurityException)
				{
					// The user disallowed the clipboard operation.
				}
			}
		}

		private void EntryLinePaste_Click(object sender, RoutedEventArgs e)
		{
			if (Clipboard.ContainsText())
			{
				try
				{
					string text = Clipboard.GetText();
					if (!string.IsNullOrEmpty(text))
					{
						// Only paste the first line if the clipboard contains multiline text.
						int newlineIndex = text.IndexOfAny(new[] { '\r', '\n' });
						if (newlineIndex >= 0)
						{
							text = text.Substring(0, newlineIndex);
						}

						this.InsertInEntryLine(text);
					}
				}
				catch (SecurityException)
				{
					// The user disallowed the clipboard operation.
				}
			}
		}

		private void EntryLineSelectAll_Click(object sender, RoutedEventArgs e)
		{
			this.GetEntryLineTextBox().SelectAll();
		}

		private void ClearHistory_Click(object sender, RoutedEventArgs e)
		{
			this.m_calc.EntryLineHistory.Clear();
		}

		private void DisplayStack_ExecutedCommand(object sender, EventArgs e)
		{
			// When a right-click action from the display stack
			// finishes we need to update the UI.
			this.FinishCommandUI();
		}

		private void EntryLineDelete_Click(object sender, RoutedEventArgs e)
		{
			this.InsertInEntryLine(string.Empty);
		}

		private void ErrorCopy_Click(object sender, RoutedEventArgs e)
		{
			string message = this.m_calc.ErrorMessage;
			if (!string.IsNullOrEmpty(message))
			{
				try
				{
					Clipboard.SetText(message);
				}
				catch (SecurityException)
				{
					// The user disallowed the clipboard operation.
				}
			}
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			ContextMenu menu = sender as ContextMenu;
			if (menu != null)
			{
				bool entryLineHasSelection = this.GetEntryLineTextBox().SelectionLength > 0;

				foreach (MenuItem item in menu.Items.OfType<MenuItem>())
				{
					string header = Convert.ToString(item.Tag, CultureInfo.CurrentCulture);
					switch (header)
					{
						case "Undo":
							item.IsEnabled = this.CanEntryLineUndo();
							break;
						case "Cut":
						case "Copy":
						case "Delete":
							item.IsEnabled = entryLineHasSelection;
							break;
						case "Paste":
							item.IsEnabled = Clipboard.ContainsText();
							break;
						case "SelectAll":
							item.IsEnabled = this.HasEntryLineText;
							break;
						case "ClearHistory":
							item.IsEnabled = this.m_calc.EntryLineHistory.Count > 0;
							break;
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private bool HandleImplicitEnter()
		{
			bool result = true;

			if (this.HasEntryLineText)
			{
				result = this.HandleEnter();
			}

			return result;
		}

		private bool HandleEnter()
		{
			bool result = true;

			// Make sure that m_calc.EntryLine is updated to match
			// the text that's currently in the entry line TextBox.
			this.UpdateEntryLineBindingSource();

			EntryLineParser parser = (EntryLineParser)this.ExecuteCommand("Enter", needsImplicitEnter: false);
			if (parser != null && parser.HasError)
			{
				result = false;
				int start, length;
				if (parser.GetErrorLocation(out start, out length))
				{
					this.GetEntryLineTextBox().Select(start, length);
				}
			}

			return result;
		}

		private void HandleBack()
		{
			if (this.HasEntryLineText)
			{
				this.ClearError();

				TextBox textBox = this.GetEntryLineTextBox();
				int selectionStart = textBox.SelectionStart;
				if (selectionStart > 0 && textBox.SelectionLength == 0)
				{
					textBox.Select(selectionStart - 1, 1);
				}

				this.InsertInEntryLine(string.Empty);
			}
			else
			{
				this.ExecuteCommand("Drop");
			}
		}

		private void HandleNegate()
		{
			EntryLineParser parser = this.ParseEntryLineToCaret();
			if (parser.InNegatableScalarValue)
			{
				this.ClearError();
				this.HandleEntryLineNegation(parser);
			}
			else
			{
				this.ExecuteCommand("Negate");
			}
		}

		private void HandleSubtract()
		{
			EntryLineParser parser = this.ParseEntryLineToCaret();
			if (parser.InComplex || parser.InDateTime)
			{
				this.ClearError();
				this.InsertInEntryLine("-");
			}
			else
			{
				this.ExecuteCommand("Subtract");
			}
		}

		private object ExecuteCommand(string commandName, bool needsImplicitEnter = true)
		{
			object result = null;

			if (!needsImplicitEnter || this.HandleImplicitEnter())
			{
				result = this.m_calc.ExecuteCommand(commandName);
				this.FinishCommandUI();
			}

			return result;
		}

		private void FinishCommandUI()
		{
			// Put the caret at the end of the line.  This is nice after
			// some entry commands like Edit and AppendToEntryLine.
			this.MoveEntryLineCaretToEnd();
			this.m_displayStack.EnsureTopOfStackIsVisible();
		}

		private void InsertInEntryLine(string text)
		{
			TextBox textBox = this.GetEntryLineTextBox();

			int start = FocusTextBox(textBox);

			// After inserting or overwriting the selected text we'll put the caret at the end
			// of the new text, which may be in the middle of the entry line.
			textBox.SelectedText = text;
			textBox.Select(start + (text ?? string.Empty).Length, 0);

			this.UpdateEntryLineBindingSource();
		}

		private static int FocusTextBox(TextBox textBox)
		{
			// In WPF the TextBox in the ComboBox has a SelectAll done whenever Focus is called.
			// In SL that doesn't happen with the TextBox.  So we'll explicitly recreate the selection
			// after setting focus.  Otherwise, clicking the number buttons would overwrite each
			// letter rather than insert.
			int start = textBox.SelectionStart;
			int length = textBox.SelectionLength;
			textBox.Focus();
			textBox.Select(start, length);
			return start;
		}

		private void MoveEntryLineCaretToEnd()
		{
			// The control has to be focused for the selection to change.
			TextBox textBox = this.GetEntryLineTextBox();
			textBox.Select(textBox.Text.Length, 0);
			FocusTextBox(textBox);
		}

		private EntryLineParser ParseEntryLineToCaret()
		{
			TextBox textBox = this.GetEntryLineTextBox();
			string entryLineToCaret = textBox.Text.Substring(0, this.GetEntryLineTextBox().SelectionStart);
			EntryLineParser parser = new EntryLineParser(this.m_calc, entryLineToCaret);
			return parser;
		}

		private void HandleEntryLineNegation(EntryLineParser parser)
		{
			string lastToken = parser.Tokens[parser.Tokens.Count - 1];
			int lastTokenLength = lastToken.Length;

			int tokenStart = this.GetEntryLineTextBox().SelectionStart - lastTokenLength;
			string entryLine = parser.EntryLine;

			if (entryLine[tokenStart] == '+')
			{
				// Change the unary plus to a negative sign.
				var sb = new StringBuilder(entryLine);
				sb[tokenStart] = '-';
				entryLine = sb.ToString();
			}
			else if (entryLine[tokenStart] == '-')
			{
				// Remove the negative sign.
				entryLine = entryLine.Remove(tokenStart, 1);
			}
			else
			{
				// Insert a negative sign.
				entryLine = entryLine.Insert(tokenStart, "-");
			}

			this.m_entryLine.Text = entryLine;
			this.MoveEntryLineCaretToEnd();
			this.UpdateEntryLineBindingSource();
		}

		private void UpdateEntryLineBindingSource()
		{
			// Force the entry line's binding to update its source (the calculator).
			// When we update the entry line programmatically through the Text
			// or SelectedText properties, it doesn't automatically update the source.
			// We have to force the source to be updated, so the calculator won't
			// be out of sync with what the display is showing.
			var bindingExpression = this.GetEntryLineBindingExpression();
			bindingExpression.UpdateSource();
		}

		private void ClearError()
		{
			this.m_calc.ClearError();
		}

		private ShortcutKey GetShortcutKey(Key key, ModifierKeys modifiers)
		{
			ShortcutKey result = ShortcutKey.None;

			// Debug.WriteLine(string.Format("Key: {0}, Modifiers: {1}", key, modifiers));

			if (modifiers == ModifierKeys.None)
			{
				switch (key)
				{
					case Key.Enter:
						result = ShortcutKey.Enter;
						break;
					case Key.Add:
						result = ShortcutKey.Add;
						break;
					case Key.Subtract:
						result = ShortcutKey.Subtract;
						break;
					case Key.Multiply:
						result = ShortcutKey.Multiply;
						break;
					case Key.Divide:
						result = ShortcutKey.Divide;
						break;
					case Key.Back:
						result = ShortcutKey.Back;
						break;
					case Key.Up:
						result = ShortcutKey.Up;
						break;
					case Key.Down:
						result = ShortcutKey.Down;
						break;
				}
			}
			else if (modifiers == ModifierKeys.Shift)
			{
				switch (key)
				{
					case Key.Add: // Shift+= -> '+'
						result = ShortcutKey.Add;
						break;

					case Key.D8: // Shift+8 -> '*'
						result = ShortcutKey.Multiply;
						break;
				}
			}
			else if (modifiers == ModifierKeys.Control)
			{
				switch (key)
				{
					case Key.Subtract:
						result = ShortcutKey.Negate;
						break;
				}
			}
			else if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
			{
				// IE uses Ctrl+? and Ctrl+Shift+? key bindings for a lot of things,
				// so I had to settle for second and third choices to find shortcuts
				// it would allow.  I don't know if these will work in other browsers
				// though, but they should always work out-of-browser.
				//
				// Note: SL4 won't raise ALT key events because IE doesn't pass
				// them on to ActiveX plug-ins, so ALT isn't even a possibility.
				switch (key)
				{
					case Key.Subtract:
						result = ShortcutKey.Dash;
						break;
					case Key.Divide:
						result = ShortcutKey.ForwardSlash;
						break;
					case Key.R:
						result = ShortcutKey.Power;
						break;
					case Key.U:
						result = ShortcutKey.SquareRoot;
						break;
					case Key.W:
						result = ShortcutKey.Swap;
						break;
				}
			}

			// If we're editing a DateTime value, then don't
			// treat arithmetic key characters as operators.
			if (result >= ShortcutKey.Add && result <= ShortcutKey.Divide)
			{
				EntryLineParser parser = this.ParseEntryLineToCaret();
				if (parser.InDateTime)
				{
					result = ShortcutKey.None;
				}
			}

			return result;
		}

		private void ScrollEntryLineHistory(bool scrollUp)
		{
			// The entry line history needs to know what we're currently displaying,
			// so it can more intelligently scroll forward and backward through history.
			this.UpdateEntryLineBindingSource();
			this.m_calc.EntryLineHistory.Scroll(scrollUp);
			this.FinishCommandUI();
		}

		#endregion
	}
}