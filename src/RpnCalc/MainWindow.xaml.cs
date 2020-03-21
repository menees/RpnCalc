namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Threading;
	using Menees.Shell;
	using Menees.Windows.Presentation;

	#endregion

	public partial class MainWindow
	{
		#region Private Data Members

		private Calculator m_calc;
		private WindowSaver saver;

		#endregion

		#region Constructors

		public MainWindow()
		{
			this.InitializeComponent();
			m_calc = (Calculator)this.FindResource("m_calc");

			this.saver = new WindowSaver(this);
			this.saver.LoadSettings += Saver_LoadSettings;
			this.saver.SaveSettings += Saver_SaveSettings;
		}

		#endregion

		#region Private Methods

		private static INode GetCalcNode(SettingsEventArgs e, bool clearNode)
		{
			const string CalcNodeName = "Calc";

			// The easiest way to clear a node for saving is to delete it because that removes all
			// values and sub-nodes.  Then the GetSubNode call below will create a new node.
			if (clearNode)
			{
				e.SettingsNode.DeleteSubNode(CalcNodeName);
			}

			ISettingsNode calcNode = e.SettingsNode.GetSubNode(CalcNodeName, true);
			SettingsNode result = new SettingsNode(calcNode);
			return result;
		}

		private Key TranslateKey(KeyEventArgs e)
		{
			Key result = e.Key;

			switch (e.Key)
			{
				case Key.OemMinus:
					// Minus is an unshifted value, so GetShortcutKey can handle it correctly.
					result = Key.Subtract;
					break;

				case Key.OemPlus:
					// OemPlus also fires when the '=' key is pressed (unshifted), so we only want to do
					// Add when Shift+'=' is pressed.  This is necessary so GetShortcutKey can behave
					// correctly when the numeric keypad's unshifted '+' is pressed as well as the main
					// keyboard's Shift+'='.  We don't have this problem with '-', '/', or '*'.
					if (Keyboard.Modifiers == ModifierKeys.Shift)
					{
						result = Key.Add;
					}
					break;

				case Key.OemQuestion:
					// Forward slash is an unshifted value, so GetShortcutKey can handle it correctly.
					result = Key.Divide;
					break;
			}

			return result;
		}

		private bool CanEntryLineUndo()
		{
			return GetEntryLineTextBox().CanUndo;
		}

		private TextBox GetEntryLineTextBox()
		{
			// This is a bit of a hack, but it's necessary to do text selection operations.
			// It's a TemplatePartAttribute declared publically on the ComboBox class,
			// so it should be reliable and stable between WPF versions.
			// http://stackoverflow.com/questions/3169328/how-to-get-combobox-selectedtext-in-wpf
			// http://msdn.microsoft.com/en-us/library/system.windows.controls.combobox.aspx
			TextBox result = (TextBox)m_entryLine.Template.FindName("PART_EditableTextBox", m_entryLine);
			return result;
		}

		private BindingExpression GetEntryLineBindingExpression()
		{
			return m_entryLine.GetBindingExpression(ComboBox.TextProperty);
		}

		private void ExecuteWhenIdle(Action whenIdle)
		{
			// Let the system pump any pending messages first.
			Dispatcher.BeginInvoke((Action)(() => whenIdle()), DispatcherPriority.ApplicationIdle);
		}

		#endregion

		#region Private Event Handlers

		private void Saver_LoadSettings(object sender, SettingsEventArgs e)
		{
			INode calcNode = GetCalcNode(e, false);
			m_calc.Load(calcNode);

			GetEntryLineTextBox().ContextMenu = m_entryLine.ContextMenu;

			//Put the caret at the end of the entry line.
			this.FinishCommandUI();
		}

		private void Saver_SaveSettings(object sender, SettingsEventArgs e)
		{
			// Ensure the current entry line value is pushed into the calculator before we save the state.
			// Unfortunately, clicking the app's Close button won't update the source automatically.
			UpdateEntryLineBindingSource();

			// Note: This is technically also a problem for our other input boxes (e.g., fixed decimal size
			// and binary word size), but they are rarely edited compared to the entry line.  Also, if they're
			// changed using the keyboard arrow keys or on-screen arrow buttons, then they update the
			// source immediately.  So they'd only miss a keyboard-entered value if someone immediately hit
			// Close.  That's rare enough that I'm willing to live with it and not add the overhead of forcing
			// those controls to update their source every time we close the app.
			INode calcNode = GetCalcNode(e, true);
			m_calc.Save(calcNode);
		}

		private void About_Click(object sender, RoutedEventArgs e)
		{
			WindowsUtility.ShowAboutBox(this, Assembly.GetExecutingAssembly());
		}

		private void EntryLineUndo_Click(object sender, RoutedEventArgs e)
		{
			GetEntryLineTextBox().Undo();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			CommandLine parser = new CommandLine(false);
			parser.AddHeader(CommandLine.ExecutableFileName + " [/BringToFront]");
			bool bringToFront = false;
			parser.AddSwitch("BringToFront", "Makes the app attempt to force its way to the foreground when launched.", value => bringToFront = value);

			switch (parser.Parse())
			{
				case CommandLineParseResult.Valid:
					if (bringToFront)
					{
						this.ExecuteWhenIdle(() => WindowsUtility.BringToFront(this));
					}

					break;

				case CommandLineParseResult.HelpRequested:
					this.ExecuteWhenIdle(() => WindowsUtility.ShowInfo(this, parser.CreateMessage()));
					break;

				default:
					this.ExecuteWhenIdle(() => WindowsUtility.ShowError(this, parser.CreateMessage()));
					break;
			}
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			// WPF's TextBox eats the KeyDown event for the backspace key, so we have to handle it here.
			// This makes the backspace key work in the entry line TextBox just like it will when other controls
			// have the focus (e.g., the stack) because we use Window_KeyDown for normal processing.
			if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
			{
				Window_KeyDown(sender, e);
				e.Handled = true;
			}
		}

		#endregion
	}
}
