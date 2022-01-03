namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using Menees.RpnCalc.Internal;

	#endregion

	public sealed class DateTimeValue : Value, IComparable<DateTimeValue>
	{
		#region Internal Constants

		internal const char StartDelimiter = '"';
		internal const char EndDelimiter = '"';

		#endregion

		#region Constructors

		public DateTimeValue(DateTime value)
		{
			this.AsDateTime = value;
		}

		#endregion

		#region Public Properties

		public override RpnValueType ValueType
		{
			get
			{
				return RpnValueType.DateTime;
			}
		}

		public DateTime AsDateTime { get; private set; }

		#endregion

		#region Public Operators

		public static DateTimeValue operator +(TimeSpanValue x, DateTimeValue y)
		{
			return Add(y, x);
		}

		public static DateTimeValue operator +(DateTimeValue x, TimeSpanValue y)
		{
			return Add(x, y);
		}

		public static TimeSpanValue operator -(DateTimeValue x, DateTimeValue y)
		{
			return Subtract(x, y);
		}

		public static DateTimeValue operator -(DateTimeValue x, TimeSpanValue y)
		{
			return Subtract(x, y);
		}

		public static bool operator ==(DateTimeValue x, DateTimeValue y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(DateTimeValue x, DateTimeValue y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(DateTimeValue x, DateTimeValue y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(DateTimeValue x, DateTimeValue y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(DateTimeValue x, DateTimeValue y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(DateTimeValue x, DateTimeValue y)
		{
			return Compare(x, y) >= 0;
		}

		#endregion

		#region Public Methods

		public static bool TryParse(string text, [MaybeNullWhen(false)] out DateTimeValue dateTimeValue)
		{
			bool result = false;
			dateTimeValue = null;

			if (!string.IsNullOrWhiteSpace(text))
			{
				// Remove the delimiters if both are present.
				text = Utility.StripDelimiters(text, StartDelimiter, EndDelimiter);

				if (DateTime.TryParse(
					text,
					DateTimeFormatInfo.CurrentInfo,
					DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault,
					out DateTime value))
				{
					dateTimeValue = new DateTimeValue(value);
					result = true;
				}
			}

			return result;
		}

		public static DateTimeValue Add(DateTimeValue x, TimeSpanValue y)
		{
			return new DateTimeValue(x.AsDateTime + y.AsTimeSpan);
		}

		public static TimeSpanValue Subtract(DateTimeValue x, DateTimeValue y)
		{
			return new TimeSpanValue(x.AsDateTime - y.AsDateTime);
		}

		public static DateTimeValue Subtract(DateTimeValue x, TimeSpanValue y)
		{
			return new DateTimeValue(x.AsDateTime - y.AsTimeSpan);
		}

		public static int Compare(DateTimeValue? x, DateTimeValue? y)
		{
			if (!CompareWithNulls(x, y, out int result))
			{
				result = x.AsDateTime.CompareTo(y.AsDateTime);
			}

			return result;
		}

		public override string ToString()
		{
			bool hasDate = this.AsDateTime.Date != DateTime.MinValue;
			bool hasTime = this.AsDateTime.TimeOfDay != TimeSpan.Zero;

			string result;
			if (hasDate && hasTime)
			{
				result = this.AsDateTime.ToString();
			}
			else if (hasTime)
			{
				result = this.AsDateTime.ToLongTimeString();
			}
			else
			{
				result = this.AsDateTime.ToShortDateString();
			}

			return result;
		}

		public override string GetEntryValue(Calculator calc)
		{
			string result = StartDelimiter + base.GetEntryValue(calc) + EndDelimiter;
			return result;
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new(3);

			result.Add(new DisplayFormat(this.ToString(calc)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_LongDateTime, this.AsDateTime.ToString("F", CultureInfo.CurrentCulture)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_ShortDateTime, this.AsDateTime.ToString("G", CultureInfo.CurrentCulture)));

			return result;
		}

		public override bool Equals(object? obj)
		{
			DateTimeValue? value = obj as DateTimeValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.AsDateTime.GetHashCode();
		}

		public int CompareTo(DateTimeValue? other)
		{
			return Compare(this, other);
		}

		#endregion
	}
}
