namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Windows;
	using Menees.RpnCalc.Internal;

	#endregion

	public sealed partial class Calculator : DependencyObject
	{
		#region Public Dependency Property Fields

		public static readonly DependencyProperty AngleModeProperty = DependencyProperty.Register(
			nameof(AngleMode),
			typeof(AngleMode),
			typeof(Calculator),
			new PropertyMetadata(AngleMode.Degrees, OnDisplayFormatChanged));

		public static readonly DependencyProperty BinaryFormatProperty = DependencyProperty.Register(
			nameof(BinaryFormat),
			typeof(BinaryFormat),
			typeof(Calculator),
			new PropertyMetadata(BinaryFormat.Decimal, OnDisplayFormatChanged));

		public static readonly DependencyProperty BinaryWordSizeProperty = DependencyProperty.Register(
			nameof(BinaryWordSize),
			typeof(int),
			typeof(Calculator),
			new PropertyMetadata(IntPtr.Size * 8, OnDisplayFormatChanged));

		public static readonly DependencyProperty ComplexFormatProperty = DependencyProperty.Register(
			nameof(ComplexFormat),
			typeof(ComplexFormat),
			typeof(Calculator),
			new PropertyMetadata(ComplexFormat.Rectangular, OnDisplayFormatChanged));

		public static readonly DependencyProperty DecimalFormatProperty = DependencyProperty.Register(
			nameof(DecimalFormat),
			typeof(DecimalFormat),
			typeof(Calculator),
			new PropertyMetadata(DecimalFormat.Standard, OnDisplayFormatChanged));

		public static readonly DependencyProperty EntryLineProperty = DependencyProperty.Register(
			nameof(EntryLine),
			typeof(string),
			typeof(Calculator),
			new PropertyMetadata(string.Empty));

		public static readonly DependencyProperty FractionFormatProperty = DependencyProperty.Register(
			nameof(FractionFormat),
			typeof(FractionFormat),
			typeof(Calculator),
			new PropertyMetadata(FractionFormat.Mixed, OnDisplayFormatChanged));

		public static readonly DependencyProperty FixedDecimalDigitsProperty = DependencyProperty.Register(
			nameof(FixedDecimalDigits),
			typeof(int),
			typeof(Calculator),
			new PropertyMetadata(6, OnDisplayFormatChanged));

		public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.Register(
			nameof(ErrorMessage),
			typeof(string),
			typeof(Calculator),
			new PropertyMetadata(null));

		#endregion

		#region Private Data Members

#if DEBUG
		private const bool ExceptionMessageResourcesAreAvailable = false;
#else
		// Assume that "debugging resource strings" are available if the default message
		// matches the one we expect for DivideByZeroException.
		private static readonly bool ExceptionMessageResourcesAreAvailable = new DivideByZeroException().Message == Resources.Calculator_DivideByZeroException;
#endif

		private readonly ValueStack stack = new();
		private readonly Commands[] commands;
		private readonly EntryLineHistoryCollection history;
		private Command? lastCommand;

		#endregion

		#region Constructors

		public Calculator()
		{
			var stackCommands = new StackCommands(this);

			// Add the Commands in the order they're most likely to be used.
			this.commands = new Commands[]
			{
				stackCommands,
				new EntryCommands(this, stackCommands),
				new MathCommands(this),
				new BinaryCommands(this),
				new FractionCommands(this),
				new TimeSpanCommands(this),
				new DateTimeCommands(this),
				new ConstantCommands(this),
				new ComplexCommands(this),
			};

			this.history = new EntryLineHistoryCollection(this);
		}

		#endregion

		#region Public Events

		public event DependencyPropertyChangedEventHandler? DisplayFormatChanged;

		#endregion

		#region Public Properties

		public AngleMode AngleMode
		{
			get
			{
				AngleMode result = (AngleMode)this.GetValue(AngleModeProperty);
				return result;
			}

			set
			{
				this.SetValue(AngleModeProperty, value);
			}
		}

		public BinaryFormat BinaryFormat
		{
			get
			{
				BinaryFormat result = (BinaryFormat)this.GetValue(BinaryFormatProperty);
				return result;
			}

			set
			{
				this.SetValue(BinaryFormatProperty, value);
			}
		}

		public int BinaryWordSize
		{
			get
			{
				int result = (int)this.GetValue(BinaryWordSizeProperty);
				return result;
			}

			set
			{
				this.SetValue(BinaryWordSizeProperty, value);
			}
		}

		public ComplexFormat ComplexFormat
		{
			get
			{
				ComplexFormat result = (ComplexFormat)this.GetValue(ComplexFormatProperty);
				return result;
			}

			set
			{
				this.SetValue(ComplexFormatProperty, value);
			}
		}

		public DecimalFormat DecimalFormat
		{
			get
			{
				DecimalFormat result = (DecimalFormat)this.GetValue(DecimalFormatProperty);
				return result;
			}

			set
			{
				this.SetValue(DecimalFormatProperty, value);
			}
		}

		public string? EntryLine
		{
			get
			{
				string result = (string)this.GetValue(EntryLineProperty);
				return result;
			}

			set
			{
				this.SetValue(EntryLineProperty, value);
			}
		}

		public FractionFormat FractionFormat
		{
			get
			{
				FractionFormat result = (FractionFormat)this.GetValue(FractionFormatProperty);
				return result;
			}

			set
			{
				this.SetValue(FractionFormatProperty, value);
			}
		}

		public int FixedDecimalDigits
		{
			get
			{
				int result = (int)this.GetValue(FixedDecimalDigitsProperty);
				return result;
			}

			set
			{
				this.SetValue(FixedDecimalDigitsProperty, value);
			}
		}

		public string ErrorMessage
		{
			get
			{
				string result = (string)this.GetValue(ErrorMessageProperty);
				return result;
			}

			internal set
			{
				// Callers can still programmatically change this via the exposed
				// dependency property, but I made this internal to encourage
				// the use of ClearError() instead.
				this.SetValue(ErrorMessageProperty, value);
			}
		}

		public ValueStack Stack
		{
			get
			{
				return this.stack;
			}
		}

		public EntryLineHistoryCollection EntryLineHistory
		{
			get
			{
				return this.history;
			}
		}

		#endregion

		#region Public Methods

		public void Load(INode root)
		{
			this.AngleMode = root.GetValue(nameof(this.AngleMode), this.AngleMode);
			this.BinaryFormat = root.GetValue(nameof(this.BinaryFormat), this.BinaryFormat);
			this.BinaryWordSize = root.GetValue(nameof(this.BinaryWordSize), this.BinaryWordSize);
			this.EntryLine = root.GetValueN(nameof(this.EntryLine), this.EntryLine);
			this.ComplexFormat = root.GetValue(nameof(this.ComplexFormat), this.ComplexFormat);
			this.DecimalFormat = root.GetValue(nameof(this.DecimalFormat), this.DecimalFormat);
			this.FixedDecimalDigits = root.GetValue(nameof(this.FixedDecimalDigits), this.FixedDecimalDigits);
			this.FractionFormat = root.GetValue(nameof(this.FractionFormat), this.FractionFormat);

			// I'm intentionally not loading an error message.

			// Now that all the settings are reloaded, we can reload the values.
			// We need the settings first, so we can parse the inputs the same
			// way we saved them.  This matters in some cases (e.g., if a complex
			// number is saved out with modes Polar & Degrees).
			INode? stackNode = root.TryGetNode(nameof(this.Stack));
			this.Stack.Load(stackNode, this);

			INode? historyNode = root.TryGetNode(nameof(this.EntryLineHistory));
			this.EntryLineHistory.Load(historyNode);
		}

		public void Save(INode root)
		{
			root.SetValue(nameof(this.AngleMode), this.AngleMode);
			root.SetValue(nameof(this.BinaryFormat), this.BinaryFormat);
			root.SetValue(nameof(this.BinaryWordSize), this.BinaryWordSize);
			root.SetValue(nameof(this.EntryLine), this.EntryLine);
			root.SetValue(nameof(this.ComplexFormat), this.ComplexFormat);
			root.SetValue(nameof(this.DecimalFormat), this.DecimalFormat);
			root.SetValue(nameof(this.FixedDecimalDigits), this.FixedDecimalDigits);
			root.SetValue(nameof(this.FractionFormat), this.FractionFormat);

			// I'm intentionally not saving the error message.
			INode stackNode = root.GetNode(nameof(this.Stack));
			this.Stack.Save(stackNode, this);

			INode historyNode = root.GetNode(nameof(this.EntryLineHistory));
			this.EntryLineHistory.Save(historyNode);
		}

		public void ClearError()
		{
			this.ErrorMessage = string.Empty;
		}

		public object? ExecuteCommand(string commandName)
		{
			object? result = this.TryExecuteCommand(commandName, (cmds, name) => cmds.FindCommand(name));
			return result;
		}

		public object? ExecuteCommand(string commandName, int commandParameter)
		{
			object? result = this.TryExecuteCommand(commandName, (cmds, name) => cmds.FindCommand(name, commandParameter));
			return result;
		}

		#endregion

		#region Internal Methods

		internal double ConvertFromRadiansToAngle(double radians)
		{
			double result = radians;

			if (this.AngleMode == AngleMode.Degrees)
			{
				result = MathCommands.ConvertFromRadiansToDegrees(radians);
			}

			return result;
		}

		internal double ConvertFromAngleToRadians(double angle)
		{
			double result = angle;

			if (this.AngleMode == RpnCalc.AngleMode.Degrees)
			{
				result = MathCommands.ConvertFromDegreesToRadians(angle);
			}

			return result;
		}

		internal void PushLastArgs()
		{
			if (this.lastCommand != null)
			{
				this.lastCommand.PushLastArgs();
			}
		}

		#endregion

		#region Private Methods

		private static void OnDisplayFormatChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Calculator calc)
			{
				calc.DisplayFormatChanged?.Invoke(calc, e);
			}
		}

		private static bool IsANoDebugResourcesMessage(string message)
		{
			// SL's mscorlib.dll has a resource string named NoDebugResources with a value of:
			//  [{0}]
			//  Arguments: {1}
			//  Debugging resource strings are unavailable. Often the key and arguments provide sufficient information to
			//  diagnose the problem. See http://go.microsoft.com/fwlink/?linkid=106663&Version={2}&File={3}&Key={4}
			// Try to detect this message pattern to see if we got back a good, custom message or this default message
			// format.  And try to detect it in a culture-neutral manner (i.e., without looking for English words).
			bool result = false;

			// Look for a bracketed resource identifier in the message.
			if (!string.IsNullOrEmpty(message))
			{
				int openBracketPos = message.IndexOf('[');

				// For English, it should be at the beginning of the message, but in other cultures, it may not be.
				bool foundOpenBracket = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en" ?
					openBracketPos == 0 : openBracketPos >= 0;
				if (foundOpenBracket)
				{
					int closedBracketPos = message.IndexOf(']', openBracketPos);

					// Make sure there's at least one character between the brackets.
					if (closedBracketPos > (openBracketPos + 1))
					{
						string textBetweenBrackets = message.Substring(openBracketPos + 1, closedBracketPos - openBracketPos - 1);

						// All of Microsoft's resource names are valid identifiers. Unfortunately,
						// CodeGenerator.IsValidLanguageIndependentIdentifier isn't available
						// in Silverlight, so I have to do the check myself.
						result = IsValidIdentifier(textBetweenBrackets);
					}
				}
			}

			return result;
		}

		private static bool IsValidIdentifier(string textBetweenBrackets)
		{
			// Make sure it's non-empty.
			bool result = !string.IsNullOrEmpty(textBetweenBrackets);
			if (result)
			{
				// Make sure the first character is a letter or digit.
				char ch = textBetweenBrackets[0];
				result = char.IsLetterOrDigit(ch);
				if (result)
				{
					// Make sure the rest of the characters are letters, digits, or underscores.
					int numChars = textBetweenBrackets.Length;
					for (int i = 1; i < numChars; i++)
					{
						ch = textBetweenBrackets[i];
						result = char.IsLetterOrDigit(ch) || ch == '_';
						if (!result)
						{
							break;
						}
					}
				}
			}

			return result;
		}

		private static string GetUserFriendlyExceptionName(Exception ex)
		{
			string originalTypeName = ex.GetType().Name;

			// Remove the "Exception" suffix if present.
			const string c_exception = "Exception";
			if (originalTypeName.EndsWith(c_exception, StringComparison.Ordinal))
			{
				originalTypeName = originalTypeName.Substring(0, originalTypeName.Length - c_exception.Length);
			}

			// Add a space after any upper-case letter in the original type name.
			StringBuilder sb = new();
			foreach (char ch in originalTypeName)
			{
				if (char.IsUpper(ch) && sb.Length > 0)
				{
					sb.Append(' ');
				}

				sb.Append(ch);
			}

			return sb.ToString();
		}

		private static string PrefixMessage(string prefix, string message)
		{
			string result = string.Format(CultureInfo.CurrentCulture, Resources.Calculator_PrefixedErrorFormat, prefix, message);
			return result;
		}

		private object? TryExecuteCommand(
			string commandName,
			Func<Commands, string, Func<Command, object?>?> findCommand)
		{
			Func<Command, object?>? command = null;

			foreach (Commands cmds in this.commands)
			{
				command = findCommand(cmds, commandName);
				if (command != null)
				{
					break;
				}
			}

			object? result = null;
			if (command == null)
			{
				// Include the command name because this is an internal error that I need to know about.
				this.SetError(PrefixMessage(commandName, Resources.Calculator_UnknownCommand));
			}
			else
			{
				result = this.Execute(commandName, command);
			}

			return result;
		}

		private object? Execute(string commandName, Func<Command, object?> executeCommand)
		{
			Conditions.RequireString(commandName, nameof(commandName));
			Conditions.RequireReference(executeCommand, nameof(executeCommand));

			this.ClearError();

			Exception? reportException = null;

			object? result = null;
			Command cmd = new(this);
			try
			{
				result = executeCommand(cmd);

				// Almost every command should call Commit, although a
				// few will cancel (e.g., Enter and Clear if they're executed
				// when the stack is empty).  I'm asserting here so I don't
				// miss cases because I want LastArgs to work properly.
				Debug.Assert(cmd.State != CommandState.None, commandName + ": Didn't call Command.Commit.");

				// If it committed, store it as the last command.
				// Don't do it earlier because EntryCommands.Last
				// needs access to the previous command's args.
				if (cmd.State == CommandState.Committed)
				{
					this.lastCommand = cmd;
				}
			}
			catch (TargetInvocationException invEx)
			{
				reportException = invEx.InnerException ?? invEx;
			}

			if (reportException != null)
			{
				Debug.WriteLine(commandName + ": " + reportException);

				// Don't include the command name in the message displayed to the user.
				// They have no way to enter the command names, so it's meaningless
				// to them.  Unlike the HP48 and RPNCalc2, I used human-friendly
				// labels in the UI this time, but they don't match the command names.
				this.SetError(reportException);
			}

			return result;
		}

		private void SetError(string message)
		{
			this.ErrorMessage = message;
		}

		private void SetError(Exception ex)
		{
			// The end-user, non-developer version of the SL runtime doesn't include all of the exception message resources.
			// So if anyone creates and throws an exception using the default constructor (e.g. new DivideByZeroException()),
			// then I won't be able to get the originally intended message unless the developer runtime of SL is installed.
			// That sucks because Microsoft's code throws exceptions created using default constructors!  For example,
			// both integer division and BigRational throw DivideByZeroExceptions created using the default constructor.
			// So I can't just pass message strings into my exception cases to handle this.  To handle this generically,
			// I'm going to check for all the exception types I know to expect based on user actions and on types I throw.
			// Then I'll make a fallback handler for other unexpected types.
			string message = ex.Message;
			if (!ExceptionMessageResourcesAreAvailable && IsANoDebugResourcesMessage(message))
			{
				// We must check for derived exception types before parent types.
				if (ex is DivideByZeroException)
				{
					message = Resources.Calculator_DivideByZeroException;
				}
				else if (ex is NotFiniteNumberException)
				{
					message = Resources.Calculator_NotFiniteNumberException;
				}
				else if (ex is ArgumentOutOfRangeException)
				{
					message = Resources.Calculator_ArgumentOutOfRangeException;
				}
				else if (ex is ArgumentNullException)
				{
					message = Resources.Calculator_ArgumentNullException;
				}
				else if (ex is ArgumentException)
				{
					message = Resources.Calculator_ArgumentException;
				}
				else if (ex is OverflowException)
				{
					message = Resources.Calculator_OverflowException;
				}
				else if (ex is ArithmeticException)
				{
					message = Resources.Calculator_ArithmeticException;
				}
				else
				{
					// Include a user-friendly exception type name in the output message.
					message = PrefixMessage(GetUserFriendlyExceptionName(ex), message);
				}
			}

			this.SetError(message);
		}

		#endregion
	}
}
