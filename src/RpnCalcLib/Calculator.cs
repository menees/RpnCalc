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

namespace Menees.RpnCalc
{
    public sealed partial class Calculator : DependencyObject
    {
        #region Constructors

        public Calculator()
        {
            var stackCommands = new StackCommands(this);

            //Add the Commands in the order they're most likely to be used.
            m_commands = new Commands[]
            {
                stackCommands,
                new EntryCommands(this, stackCommands),
                new MathCommands(this),
                new BinaryCommands(this),
                new FractionCommands(this),
                new TimeSpanCommands(this),
                new DateTimeCommands(this),
                new ConstantCommands(this),
                new ComplexCommands(this)
            };

            m_history = new EntryLineHistoryCollection(this);
        }

        #endregion

        #region Public Properties

        public AngleMode AngleMode
        {
            get
            {
                AngleMode result = (AngleMode)GetValue(AngleModeProperty);
                return result;
            }
            set
            {
                SetValue(AngleModeProperty, value);
            }
        }

        public static readonly DependencyProperty AngleModeProperty = DependencyProperty.Register(
            "AngleMode", typeof(AngleMode), typeof(Calculator),
            new PropertyMetadata(AngleMode.Degrees, OnDisplayFormatChanged));

        public BinaryFormat BinaryFormat
        {
            get
            {
                BinaryFormat result = (BinaryFormat)GetValue(BinaryFormatProperty);
                return result;
            }
            set
            {
                SetValue(BinaryFormatProperty, value);
            }
        }

        public static readonly DependencyProperty BinaryFormatProperty = DependencyProperty.Register(
            "BinaryFormat", typeof(BinaryFormat), typeof(Calculator),
            new PropertyMetadata(BinaryFormat.Decimal, OnDisplayFormatChanged));

        public int BinaryWordSize
        {
            get
            {
                int result = (int)GetValue(BinaryWordSizeProperty);
                return result;
            }
            set
            {
                SetValue(BinaryWordSizeProperty, value);
            }
        }

        public static readonly DependencyProperty BinaryWordSizeProperty = DependencyProperty.Register(
            "BinaryWordSize", typeof(int), typeof(Calculator),
            new PropertyMetadata(IntPtr.Size * 8, OnDisplayFormatChanged));

        public ComplexFormat ComplexFormat
        {
            get
            {
                ComplexFormat result = (ComplexFormat)GetValue(ComplexFormatProperty);
                return result;
            }
            set
            {
                SetValue(ComplexFormatProperty, value);
            }
        }

        public static readonly DependencyProperty ComplexFormatProperty = DependencyProperty.Register(
            "ComplexFormat", typeof(ComplexFormat), typeof(Calculator),
            new PropertyMetadata(ComplexFormat.Rectangular, OnDisplayFormatChanged));

        public DecimalFormat DecimalFormat
        {
            get
            {
                DecimalFormat result = (DecimalFormat)GetValue(DecimalFormatProperty);
                return result;
            }
            set
            {
                SetValue(DecimalFormatProperty, value);
            }
        }

        public static readonly DependencyProperty DecimalFormatProperty = DependencyProperty.Register(
            "DecimalFormat", typeof(DecimalFormat), typeof(Calculator),
            new PropertyMetadata(DecimalFormat.Standard, OnDisplayFormatChanged));

        public string EntryLine
        {
            get
            {
                string result = (string)GetValue(EntryLineProperty);
                return result;
            }
            set
            {
                SetValue(EntryLineProperty, value);
            }
        }

        public static readonly DependencyProperty EntryLineProperty = DependencyProperty.Register(
            "EntryLine", typeof(string), typeof(Calculator),
            new PropertyMetadata(""));

        public FractionFormat FractionFormat
        {
            get
            {
                FractionFormat result = (FractionFormat)GetValue(FractionFormatProperty);
                return result;
            }
            set
            {
                SetValue(FractionFormatProperty, value);
            }
        }

        public static readonly DependencyProperty FractionFormatProperty = DependencyProperty.Register(
            "FractionFormat", typeof(FractionFormat), typeof(Calculator),
            new PropertyMetadata(FractionFormat.Mixed, OnDisplayFormatChanged));

        public int FixedDecimalDigits
        {
            get
            {
                int result = (int)GetValue(FixedDecimalDigitsProperty);
                return result;
            }
            set
            {
                SetValue(FixedDecimalDigitsProperty, value);
            }
        }

        public static readonly DependencyProperty FixedDecimalDigitsProperty = DependencyProperty.Register(
            "FixedDecimalDigits", typeof(int), typeof(Calculator),
            new PropertyMetadata(6, OnDisplayFormatChanged));

        public string ErrorMessage
        {
            get
            {
                string result = (string)GetValue(ErrorMessageProperty);
                return result;
            }
            internal set
            {
                //Callers can still programmatically change this via the exposed
                //dependency property, but I made this internal to encourage
                //the use of ClearError() instead.
                SetValue(ErrorMessageProperty, value);
            }
        }

        public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.Register(
            "ErrorMessage", typeof(string), typeof(Calculator),
            new PropertyMetadata(null));

        public ValueStack Stack
        {
            get
            {
                return m_stack;
            }
        }

        public EntryLineHistoryCollection EntryLineHistory
        {
            get
            {
                return m_history;
            }
        }

        #endregion

        #region Public Methods

		public void Load(INode root)
		{
			AngleMode = root.GetValue("AngleMode", AngleMode);
			BinaryFormat = root.GetValue("BinaryFormat", BinaryFormat);
			BinaryWordSize = root.GetValue("BinaryWordSize", BinaryWordSize);
			EntryLine = root.GetValue("EntryLine", EntryLine);
			ComplexFormat = root.GetValue("ComplexFormat", ComplexFormat);
			DecimalFormat = root.GetValue("DecimalFormat", DecimalFormat);
			FixedDecimalDigits = root.GetValue("FixedDecimalDigits", FixedDecimalDigits);
			FractionFormat = root.GetValue("FractionFormat", FractionFormat);
			//I'm intentionally not loading an error message.

			//Now that all the settings are reloaded, we can reload the values.
			//We need the settings first, so we can parse the inputs the same
			//way we saved them.  This matters in some cases (e.g., if a complex
			//number is saved out with modes Polar & Degrees).
			INode stackNode = root.GetNode("Stack", false);
			Stack.Load(stackNode, this);

			INode historyNode = root.GetNode("EntryLineHistory", false);
			EntryLineHistory.Load(historyNode);
		}

		public void Save(INode root)
		{
			root.SetValue("AngleMode", AngleMode);
			root.SetValue("BinaryFormat", BinaryFormat);
			root.SetValue("BinaryWordSize", BinaryWordSize);
			root.SetValue("EntryLine", EntryLine);
			root.SetValue("ComplexFormat", ComplexFormat);
			root.SetValue("DecimalFormat", DecimalFormat);
			root.SetValue("FixedDecimalDigits", FixedDecimalDigits);
			root.SetValue("FractionFormat", FractionFormat);
			//I'm intentionally not saving the error message.

			INode stackNode = root.GetNode("Stack", true);
			Stack.Save(stackNode, this);

			INode historyNode = root.GetNode("EntryLineHistory", true);
			EntryLineHistory.Save(historyNode);
		}

        public void ClearError()
        {
            ErrorMessage = "";
        }

        public object ExecuteCommand(string commandName)
        {
            object result = TryExecuteCommand(commandName, (cmds, name) => cmds.FindCommand(name));
            return result;
        }

        public object ExecuteCommand(string commandName, int commandParameter)
        {
            object result = TryExecuteCommand(commandName, (cmds, name) => cmds.FindCommand(name, commandParameter));
            return result;
        }

        #endregion

        #region Public Events

        public event DependencyPropertyChangedEventHandler DisplayFormatChanged;

        #endregion

        #region Internal Methods

        internal double ConvertFromRadiansToAngle(double radians)
        {
            double result = radians;

            if (AngleMode == AngleMode.Degrees)
            {
                result = MathCommands.ConvertFromRadiansToDegrees(radians);
            }

            return result;
        }

        internal double ConvertFromAngleToRadians(double angle)
        {
            double result = angle;

            if (AngleMode == RpnCalc.AngleMode.Degrees)
            {
                result = MathCommands.ConvertFromDegreesToRadians(angle);
            }

            return result;
        }

        internal void PushLastArgs()
        {
            if (m_lastCommand != null)
            {
                m_lastCommand.PushLastArgs();
            }
        }

        #endregion

        #region Private Methods

        private static void OnDisplayFormatChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Calculator calc = sender as Calculator;
            if (calc != null)
            {
                var eh = calc.DisplayFormatChanged;
                if (eh != null)
                {
                    eh(calc, e);
                }
            }
        }

        private object TryExecuteCommand(string commandName,
            Func<Commands, string, Func<Command, object>> findCommand)
        {
            Func<Command, object> command = null;

            foreach (Commands cmds in m_commands)
            {
                command = findCommand(cmds, commandName);
                if (command != null)
                {
                    break;
                }
            }

            object result = null;
            if (command == null)
            {
                //Include the command name because this is an internal error that I need to know about.
                SetError(PrefixMessage(commandName, Resources.Calculator_UnknownCommand));
            }
            else
            {
                result = Execute(commandName, command);
            }

            return result;
        }

        private object Execute(string commandName, Func<Command, object> executeCommand)
        {
            Debug.Assert(!string.IsNullOrEmpty(commandName));
            Debug.Assert(executeCommand != null);

            ClearError();

            Exception reportException = null;

            object result = null;
            Command cmd = new Command(this);
            try
            {
                result = executeCommand(cmd);

                //Almost every command should call Commit, although a
                //few will cancel (e.g., Enter and Clear if they're executed
                //when the stack is empty).  I'm asserting here so I don't
                //miss cases because I want LastArgs to work properly.
                Debug.Assert(cmd.State != CommandState.None, commandName + ": Didn't call Command.Commit.");

                //If it committed, store it as the last command.
                //Don't do it earlier because EntryCommands.Last
                //needs access to the previous command's args.
                if (cmd.State == CommandState.Committed)
                {
                    m_lastCommand = cmd;
                }
            }
            catch (TargetInvocationException invEx)
            {
                reportException = invEx.InnerException != null ? invEx.InnerException : invEx;
            }

            if (reportException != null)
            {
                Debug.WriteLine(commandName + ": " + reportException.ToString());

                //Don't include the command name in the message displayed to the user.
                //They have no way to enter the command names, so it's meaningless
                //to them.  Unlike the HP48 and RPNCalc2, I used human-friendly
                //labels in the UI this time, but they don't match the command names.
                SetError(reportException);
            }

            return result;
        }

        private void SetError(string message)
        {
            ErrorMessage = message;
        }

        private void SetError(Exception ex)
        {
            //The end-user, non-developer version of the SL runtime doesn't include all of the exception message resources.
            //So if anyone creates and throws an exception using the default constructor (e.g. new DivideByZeroException()),
            //then I won't be able to get the originally intended message unless the developer runtime of SL is installed.
            //That sucks because Microsoft's code throws exceptions created using default constructors!  For example,
            //both integer division and BigRational throw DivideByZeroExceptions created using the default constructor.
            //So I can't just pass message strings into my exception cases to handle this.  To handle this generically,
            //I'm going to check for all the exception types I know to expect based on user actions and on types I throw.
            //Then I'll make a fallback handler for other unexpected types.
            string message = ex.Message;
            if (!c_exceptionMessageResourcesAreAvailable && IsANoDebugResourcesMessage(message))
            {
                //We must check for derived exception types before parent types.
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
                    //Include a user-friendly exception type name in the output message.
                    message = PrefixMessage(GetUserFriendlyExceptionName(ex), message);
                }
            }

            SetError(message);
        }

        private static bool IsANoDebugResourcesMessage(string message)
        {
            //SL's mscorlib.dll has a resource string named NoDebugResources with a value of:
            //  [{0}]
            //  Arguments: {1}
            //  Debugging resource strings are unavailable. Often the key and arguments provide sufficient information to
            //  diagnose the problem. See http://go.microsoft.com/fwlink/?linkid=106663&Version={2}&File={3}&Key={4}
            //Try to detect this message pattern to see if we got back a good, custom message or this default message
            //format.  And try to detect it in a culture-neutral manner (i.e., without looking for English words).
            bool result = false;

            //Look for a bracketed resource identifier in the message.
            if (!string.IsNullOrEmpty(message))
            {
                int openBracketPos = message.IndexOf('[');
                //For English, it should be at the beginning of the message, but in other cultures, it may not be.
                bool foundOpenBracket = CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en" ?
                    openBracketPos == 0 : openBracketPos >= 0;
                if (foundOpenBracket)
                {
                    int closedBracketPos = message.IndexOf(']', openBracketPos);
                    //Make sure there's at least one character between the brackets.
                    if (closedBracketPos > (openBracketPos+1))
                    {
                        string textBetweenBrackets = message.Substring(openBracketPos + 1, closedBracketPos - openBracketPos - 1);
                        //All of Microsoft's resource names are valid identifiers. Unfortunately,
                        //CodeGenerator.IsValidLanguageIndependentIdentifier isn't available
                        //in Silverlight, so I have to do the check myself.
                        result = IsValidIdentifier(textBetweenBrackets);
                    }
                }
            }

            return result;
        }

        private static bool IsValidIdentifier(string textBetweenBrackets)
        {
            //Make sure it's non-empty.
            bool result = !string.IsNullOrEmpty(textBetweenBrackets);
            if (result)
            {
                //Make sure the first character is a letter or digit.
                char ch = textBetweenBrackets[0];
                result = char.IsLetterOrDigit(ch);
                if (result)
                {
                    //Make sure the rest of the characters are letters, digits, or underscores.
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

            //Remove the "Exception" suffix if present.
            const string c_exception = "Exception";
            if (originalTypeName.EndsWith(c_exception, StringComparison.Ordinal))
            {
                originalTypeName = originalTypeName.Substring(0, originalTypeName.Length - c_exception.Length);
            }

            //Add a space after any upper-case letter in the original type name.
            StringBuilder sb = new StringBuilder();
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

        #endregion

        #region Private Data Members

        private ValueStack m_stack = new ValueStack();
        private Command m_lastCommand;
        private Commands[] m_commands;
        private EntryLineHistoryCollection m_history;

        private const string c_storageFileName = "RpnCalc.xml";

#if DEBUG
        private const bool c_exceptionMessageResourcesAreAvailable = false;
#else
        //Assume that "debugging resource strings" are available if the default message
        //matches the one we expect for DivideByZeroException.
        private static readonly bool c_exceptionMessageResourcesAreAvailable = new DivideByZeroException().Message == Resources.Calculator_DivideByZeroException;
#endif

        #endregion
    }
}
