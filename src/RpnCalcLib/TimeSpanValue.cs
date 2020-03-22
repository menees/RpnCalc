namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using Menees.RpnCalc.Internal;

	#endregion

	public sealed class TimeSpanValue : Value, IComparable<TimeSpanValue>
	{
		#region Internal Constants

		// Each culture can use different field separators, but we use
		// this in TryParse for some exact format strings, and the
		// EntryLineParser needs to know it for some partial-match cases.
		internal const char FieldSeparator = ':';

		#endregion

		#region Private Data Members

		private static readonly string[] MinSecFormats = new[]
			{
				@"m\:s", @"m\:s\.FFFFFFF",
				@"\-m\:s", @"\-m\:s\.FFFFFFF",
			};

		#endregion

		#region Constructors

		public TimeSpanValue(TimeSpan value)
		{
			this.AsTimeSpan = value;
		}

		#endregion

		#region Public Properties

		public override RpnValueType ValueType
		{
			get
			{
				return RpnValueType.TimeSpan;
			}
		}

		public TimeSpan AsTimeSpan { get; private set; }

		#endregion

		#region Public Operators

		public static TimeSpanValue operator +(TimeSpanValue x, TimeSpanValue y)
		{
			return Add(x, y);
		}

		public static TimeSpanValue operator -(TimeSpanValue x, TimeSpanValue y)
		{
			return Subtract(x, y);
		}

		public static TimeSpanValue operator *(TimeSpanValue x, DoubleValue y)
		{
			return Multiply(x, y);
		}

		public static TimeSpanValue operator *(DoubleValue x, TimeSpanValue y)
		{
			return Multiply(x, y);
		}

		public static DoubleValue operator /(TimeSpanValue x, TimeSpanValue y)
		{
			return Divide(x, y);
		}

		public static TimeSpanValue operator /(TimeSpanValue x, DoubleValue y)
		{
			return Divide(x, y);
		}

		public static TimeSpanValue operator +(TimeSpanValue x)
		{
			return x;
		}

		public static TimeSpanValue operator -(TimeSpanValue x)
		{
			return Negate(x);
		}

		public static TimeSpanValue operator %(TimeSpanValue x, TimeSpanValue y)
		{
			return Modulus(x, y);
		}

		public static bool operator ==(TimeSpanValue x, TimeSpanValue y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(TimeSpanValue x, TimeSpanValue y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(TimeSpanValue x, TimeSpanValue y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(TimeSpanValue x, TimeSpanValue y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(TimeSpanValue x, TimeSpanValue y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(TimeSpanValue x, TimeSpanValue y)
		{
			return Compare(x, y) >= 0;
		}

		#endregion

		#region Public Methods

		public static bool TryParse(string text, out TimeSpanValue timeSpanValue)
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			DateTimeFormatInfo timeFmt = currentCulture.DateTimeFormat;
			NumberFormatInfo numFmt = currentCulture.NumberFormat;

			// Try using our custom TimeSpan parser first.  It differs in a few ways
			// from .NET's standard TimeSpan parsing:
			//  * It defaults to M:S instead of H:M if only two fields are specified.
			//  * It will return negative values if a negative sign is used in the text.
			//  * It handles out-of-bounds values correctly (e.g., 79:30 --> 1 hr 19 min 30 sec).
			//
			// If our custom parser can't parse it, then we'll fall back to the system's
			// parsing logic and see what it says.  It may deal with other cultures
			// better, so I don't want to skip it entirely.
			bool result = TimeSpanParser.TryParse(text, timeFmt, numFmt, out timeSpanValue);
			if (!result)
			{
				result = SystemTryParse(text, timeFmt, numFmt, out timeSpanValue);
			}

			return result;
		}

		public static TimeSpanValue Add(TimeSpanValue x, TimeSpanValue y)
		{
			return new TimeSpanValue(x.AsTimeSpan + y.AsTimeSpan);
		}

		public static TimeSpanValue Subtract(TimeSpanValue x, TimeSpanValue y)
		{
			return new TimeSpanValue(x.AsTimeSpan - y.AsTimeSpan);
		}

		public static TimeSpanValue Multiply(TimeSpanValue x, DoubleValue y)
		{
			return new TimeSpanValue(TimeSpan.FromTicks((long)(x.AsTimeSpan.Ticks * y.AsDouble)));
		}

		public static TimeSpanValue Multiply(DoubleValue x, TimeSpanValue y)
		{
			return Multiply(y, x);
		}

		public static DoubleValue Divide(TimeSpanValue x, TimeSpanValue y)
		{
			return new DoubleValue((double)x.AsTimeSpan.Ticks / (double)y.AsTimeSpan.Ticks);
		}

		public static TimeSpanValue Divide(TimeSpanValue x, DoubleValue y)
		{
			return new TimeSpanValue(TimeSpan.FromTicks((long)(x.AsTimeSpan.Ticks / y.AsDouble)));
		}

		public static TimeSpanValue Negate(TimeSpanValue x)
		{
			return new TimeSpanValue(-x.AsTimeSpan);
		}

		public static TimeSpanValue Modulus(TimeSpanValue x, TimeSpanValue y)
		{
			return new TimeSpanValue(TimeSpan.FromTicks(x.AsTimeSpan.Ticks % y.AsTimeSpan.Ticks));
		}

		public static int Compare(TimeSpanValue x, TimeSpanValue y)
		{
			if (!CompareWithNulls(x, y, out int result))
			{
				result = x.AsTimeSpan.CompareTo(y.AsTimeSpan);
			}

			return result;
		}

		public override string ToString()
		{
			return this.AsTimeSpan.ToString();
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new List<DisplayFormat>(2);

			result.Add(new DisplayFormat(this.ToString(calc)));

			// For English, we'll also show: "- d days h hr m min s.fff sec"
			if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en")
			{
				StringBuilder sb = new StringBuilder();

				TimeSpan displayValue = this.AsTimeSpan;
				if (displayValue.Ticks < 0)
				{
					sb.Append("- ");
					displayValue = displayValue.Negate();
				}

				if (displayValue.Days > 0)
				{
					sb.Append(displayValue.Days).Append(" day").Append(displayValue.Days != 1 ? "s " : " ");
				}

				if (displayValue.Hours > 0)
				{
					sb.Append(displayValue.Hours).Append(" hr ");
				}

				if (displayValue.Minutes > 0)
				{
					sb.Append(displayValue.Minutes).Append(" min ");
				}

				if (displayValue.Seconds > 0 || displayValue.Milliseconds > 0)
				{
					if (displayValue.Milliseconds > 0)
					{
						sb.AppendFormat(CultureInfo.CurrentCulture, "{0}.{1:D3}", displayValue.Seconds, displayValue.Milliseconds);
					}
					else
					{
						sb.Append(displayValue.Seconds);
					}

					sb.Append(" sec");
				}

				result.Add(new DisplayFormat(Resources.DisplayFormat_Formatted, sb.ToString().TrimEnd()));
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			TimeSpanValue value = obj as TimeSpanValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.AsTimeSpan.GetHashCode();
		}

		public int CompareTo(TimeSpanValue other)
		{
			return Compare(this, other);
		}

		#endregion

		#region Private Methods

		private static bool SystemTryParse(
			string text,
			DateTimeFormatInfo timeFmt,
			NumberFormatInfo numFmt,
			out TimeSpanValue timeSpanValue)
		{
			// Give precedence to M:SS formats over H:MM because I enter
			// a lot more minute:second values than hour:minute values.  A
			// user can always enter H:MM:00 to force hour:minute parsing.
			//
			// Note: DateTimeFormatInfo doesn't explicitly give us the time
			// separator character(s), and I don't want to mess with trying
			// to parse its ShortTimePattern property.  So my exact format
			// strings will always assume ':'.  However, I will update them to
			// use the current thread's decimal separator and negative sign
			// if necessary.
			string[] minSecFormats = MinSecFormats;
			if (numFmt.NumberDecimalSeparator != ".")
			{
				minSecFormats = (from fmt in minSecFormats
								select fmt.Replace(".", numFmt.NumberDecimalSeparator)).ToArray();
			}

			if (numFmt.NegativeSign != "-")
			{
				minSecFormats = (from fmt in minSecFormats
								select fmt.Replace("-", numFmt.NegativeSign)).ToArray();
			}

			bool result = false;
			timeSpanValue = null;

			// Try to parse the value using our exact format strings first,
			// and if those fail, then fall back to general case parsing.
			if (TimeSpan.TryParseExact(text, minSecFormats, timeFmt, out TimeSpan value))
			{
				// In SL4, TryParseExact won't return a negative TimeSpan
				// even if it matches the negative sign in the text.  So I
				// have to manually check for that case here.
				//
				// Note: I could call the TryParseExact overload that takes
				// TimeSpanStyles.AssumeNegative, but then I'd have to
				// split my logic into two separate calls (one for positive
				// formats and one for negative formats).  This way seems
				// easier to maintain and understand.
				if (text.StartsWith(numFmt.NegativeSign, StringComparison.CurrentCulture) && value > TimeSpan.Zero)
				{
					value = -value;
				}

				timeSpanValue = new TimeSpanValue(value);
				result = true;
			}
			else if (TimeSpan.TryParse(text, timeFmt, out value))
			{
				timeSpanValue = new TimeSpanValue(value);
				result = true;
			}

			return result;
		}

		#endregion
	}
}
