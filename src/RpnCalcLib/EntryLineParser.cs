﻿namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using Menees.RpnCalc.Internal;

	#endregion

	public class EntryLineParser
	{
		#region Private Data Members

		private const char NULL = '\0';

		// These are listed in the order we want to try them.
		private static readonly RpnValueType[] AmbiguousValueTypes =
		{
			// Try as Integer first because TimeSpan will parse
			// an integer as a number of days.  That's not what
			// we want normally. Also, integer must come before
			// double since every sub-15-digit integer would parse
			// as a double.
			RpnValueType.Integer,

			// TimeSpans can use culture-specific separators,
			// but they shouldn't conflict with a Double's format.
			RpnValueType.TimeSpan,
			RpnValueType.Double,
		};

		private readonly Calculator calc;

		// If you add a member here, add it to the Reset() method too.
		private readonly List<Token> tokens = new();
		private readonly List<Value> values = new();
		private int position;
		private ParseStates states;
		private string? errorMessage;

		private string entryLine = string.Empty;
		private int entryLineLength;

		#endregion

		#region Constructors

		public EntryLineParser(Calculator calc)
		{
			this.calc = calc;
		}

		public EntryLineParser(Calculator calc, string entryLine)
			: this(calc)
		{
			this.EntryLine = entryLine;
		}

		#endregion

		#region ParseStates

		[Flags]
		private enum ParseStates
		{
			None = 0,

			// We were able to completely parse all the tokens
			// (although the last one may not have an ending delimiter).
			Complete = 1,

			// The last token begins with '(' but doesn't end with ')'.
			InComplex = 2,

			// The last token begins with '"' but doesn't end with '"'.
			InDateTime = 4,

			// Last token appears to be a Fraction, TimeSpan, Integer, or Double.
			// Might be incomplete (e.g., 1_2_, 3:12:, 1.23e-).
			InNegatableScalarValue = 8,

			// Note: We don't need an Error state because
			// it's really just the lack of the Complete state,
			// and we have an ErrorMessage member for it too.
		}

		#endregion

		#region Public Properties

		public string EntryLine
		{
			get
			{
				return this.entryLine;
			}

			set
			{
				this.entryLine = value ?? string.Empty;
				this.entryLineLength = this.entryLine.Length;

				// Reset all members before we start tokenizing.
				this.Reset();

				// Split the entry line into tokens (i.e., text, positions, and type hints).
				this.Tokenize();

				// Try to parse the tokens into values.
				this.Parse();

				// Setting state flags in one method is a lot simpler than trying to
				// redetermine the states whenever each state property is invoked.
				this.SetStates();
			}
		}

		public IList<string> Tokens
		{
			get
			{
				IList<string> result = (from t in this.tokens
										select t.Text).ToList();
				return result;
			}
		}

		public IList<Value> Values
		{
			get
			{
				return this.values.AsReadOnly();
			}
		}

		public bool IsEntryLineComplete
		{
			get
			{
				return this.states.HasFlag(ParseStates.Complete);
			}
		}

		public bool InComplex
		{
			get
			{
				return this.states.HasFlag(ParseStates.InComplex);
			}
		}

		public bool InDateTime
		{
			get
			{
				return this.states.HasFlag(ParseStates.InDateTime);
			}
		}

		public bool InNegatableScalarValue
		{
			get
			{
				return this.states.HasFlag(ParseStates.InNegatableScalarValue);
			}
		}

		public bool HasError
		{
			get
			{
				return !string.IsNullOrEmpty(this.errorMessage);
			}
		}

		public string ErrorMessage
		{
			get
			{
				return this.errorMessage ?? string.Empty;
			}
		}

		#endregion

		#region Public Methods

		public bool GetErrorLocation(out int start, out int length)
		{
			bool result = false;
			start = this.entryLineLength;
			length = 0;

			// Assume that the first unparsed token is the one with the error
			// since that's where then Parse() method stops processing.
			int valueCount = this.values.Count;
			if (this.HasError && valueCount < this.tokens.Count)
			{
				Token errorToken = this.tokens[valueCount];
				start = errorToken.StartPosition;
				length = errorToken.Text.Length;
				result = true;
			}

			return result;
		}

		#endregion

		#region Private Methods

		private static bool ContainsFractionValueSeparator(string text)
		{
			// Look for '_' and '/' because FractionValue can parse them both.
			// However, we have to be a little careful to avoid ambiguities with
			// dates, which also use '/'.  But everywhere in this class, I've made
			// sure to look for dates before dealing with fractions.
			bool result = text.Contains(FractionValue.EntrySeparator)
				|| text.Contains(FractionValue.DisplaySeparator);
			return result;
		}

		private void Reset()
		{
			this.states = ParseStates.None;
			this.tokens.Clear();
			this.values.Clear();
			this.errorMessage = null;
			this.position = 0;
		}

		private void Tokenize()
		{
			if (!string.IsNullOrWhiteSpace(this.entryLine))
			{
				char ch = this.SkipWhitespace();
				while (ch != NULL)
				{
					int tokenStartPosition = this.position - 1;
					RpnValueType? tokenValueType = null;

					string tokenText;
					if (ch == ComplexValue.StartDelimiter)
					{
						tokenText = this.ReadToEndDelimiter(ch, ComplexValue.EndDelimiter);
						tokenValueType = RpnValueType.Complex;
					}
					else if (ch == DateTimeValue.StartDelimiter)
					{
						tokenText = this.ReadToEndDelimiter(ch, DateTimeValue.EndDelimiter);
						tokenValueType = RpnValueType.DateTime;
					}
					else if ((ch == BinaryValue.Prefix) || (ch == '0' && this.PeekChar() == 'x'))
					{
						// Ignore whitespace between the '#' prefix and the digits.
						if (ch == BinaryValue.Prefix && char.IsWhiteSpace(this.PeekChar()))
						{
							this.SkipWhitespace();
							this.UngetChar();
						}

						tokenText = this.ReadToWhitespace(ch);
						tokenValueType = RpnValueType.Binary;
					}
					else
					{
						// It could be a fraction, timespan, integer, double, or junk.
						// We'll let the Parse method figure that out.
						tokenText = this.ReadToWhitespace(ch);
					}

					if (!string.IsNullOrEmpty(tokenText))
					{
						this.tokens.Add(new Token(tokenText, tokenStartPosition, tokenValueType));
					}

					ch = this.SkipWhitespace();
				}
			}
		}

		private char GetChar()
		{
			char ch = this.PeekChar();
			if (ch != NULL)
			{
				this.position++;
			}

			return ch;
		}

		private void UngetChar()
		{
			if (this.position > 0)
			{
				this.position--;
			}
		}

		private char PeekChar() => this.position < this.entryLineLength ? this.entryLine[this.position] : NULL;

		private char SkipWhitespace()
		{
			char ch;
			do
			{
				ch = this.GetChar();
			}
			while (ch != NULL && char.IsWhiteSpace(ch));
			return ch;
		}

		private string ReadToEndDelimiter(char ch, char endDelimiter)
		{
			StringBuilder sb = new();
			sb.Append(ch);
			while ((ch = this.GetChar()) != NULL && ch != endDelimiter)
			{
				sb.Append(ch);
			}

			if (ch == endDelimiter)
			{
				sb.Append(ch);
			}

			return sb.ToString();
		}

		private string ReadToWhitespace(char ch)
		{
			StringBuilder sb = new();
			sb.Append(ch);
			while ((ch = this.GetChar()) != NULL && !char.IsWhiteSpace(ch))
			{
				sb.Append(ch);
			}

			return sb.ToString();
		}

		private void Parse()
		{
			foreach (Token token in this.tokens)
			{
				string text = token.Text;
				Debug.Assert(!string.IsNullOrEmpty(text), "Token.Text should always be non-empty.");

				int startingValueCount = this.values.Count;
				if (token.ValueType.HasValue)
				{
					// Handles: Complex, DateTime, Binary
					if (Value.TryParse(token.ValueType.Value, text, this.calc, out Value? value))
					{
						this.values.Add(value);
					}
				}
				else if (ContainsFractionValueSeparator(text))
				{
					// Handles: Fraction
					if (FractionValue.TryParse(text, out FractionValue? value))
					{
						this.values.Add(value);
					}
				}
				else
				{
					// Handles: TimeSpan, Integer, Double
					foreach (RpnValueType type in AmbiguousValueTypes)
					{
						if (Value.TryParse(type, text, this.calc, out Value? value))
						{
							this.values.Add(value);
							break; // Quit the inner c_ambiguousValueTypes loop.
						}
					}
				}

				// If we weren't able to parse the current token, then stop.
				// The GetErrorLocation() method depends on this behavior.
				if (startingValueCount == this.values.Count)
				{
					// Use the same "helpful" error message that the HP48 uses.
					this.errorMessage = Resources.EntryLineParser_InvalidSyntax;
					break; // Quit the outer this.tokens loop.
				}
			}
		}

		private void SetStates()
		{
			int tokenCount = this.tokens.Count;
			if (tokenCount == this.values.Count)
			{
				// We were able to completely parse all the tokens
				// (although the last one may not have an ending delimiter).
				this.states |= ParseStates.Complete;
			}

			if (tokenCount > 0)
			{
				Token lastToken = this.tokens[tokenCount - 1];

				string text = lastToken.Text;
				char firstChar = text[0];
				char lastChar = text[text.Length - 1];
				if (firstChar == ComplexValue.StartDelimiter && lastChar != ComplexValue.EndDelimiter)
				{
					// The last token begins with '(' but doesn't end with ')'.
					this.states |= ParseStates.InComplex;
				}
				else if (firstChar == DateTimeValue.StartDelimiter && lastChar != DateTimeValue.EndDelimiter)
				{
					// The last token begins with '"' but doesn't end with '"'.
					this.states |= ParseStates.InDateTime;
				}
				else if (!char.IsWhiteSpace(this.entryLine[this.entryLineLength - 1]))
				{
					// The entry line doesn't end with whitespace, so assume the
					// caret position is at the end of the last token.  Then try to
					// determine if the last value or token is a negatable scalar.
					if (this.IsEntryLineComplete)
					{
						// We have a parsed entry line, so we'll use the last value's type.
						Value lastValue = this.values[this.values.Count - 1];
						RpnValueType type = lastValue.ValueType;
						if (type == RpnValueType.Integer || type == RpnValueType.Double ||
							type == RpnValueType.Fraction || type == RpnValueType.TimeSpan)
						{
							this.states |= ParseStates.InNegatableScalarValue;
						}
					}
					else
					{
						// The entry line is incomplete.  This can happen for inputs
						// like "1_2_", "3:12:", and "1.23e-".  Check if the last token
						// appears to be a Fraction or TimeSpan.  I don't know a
						// good, safe way to determine if they've entered the start
						// of a double value that currently can't be parsed, so we
						// just won't return this state in that case.
						if (ContainsFractionValueSeparator(text) ||
							text.Contains(TimeSpanValue.FieldSeparator))
						{
							this.states |= ParseStates.InNegatableScalarValue;
						}
					}
				}
			}
		}

		#endregion

		#region Private Types

		#region Token

		private sealed class Token
		{
			#region Constructors

			public Token(string text, int startPos, RpnValueType? type)
			{
				this.Text = text;
				this.StartPosition = startPos;
				this.ValueType = type;
			}

			#endregion

			#region Public Properties

			public string Text { get; private set; }

			public int StartPosition { get; private set; }

			public RpnValueType? ValueType { get; private set; }

			#endregion
		}

		#endregion

		#endregion
	}
}
