#region Using Directives

using System;
using System.Collections.Generic;
using System.Globalization;
using Menees.RpnCalc.Internal;

#endregion

namespace Menees.RpnCalc
{
	public sealed class DateTimeValue : Value, IComparable<DateTimeValue>
	{
		#region Constructors

		public DateTimeValue(DateTime value)
		{
			this.m_value = value;
		}

		#endregion

		#region Public Properties

		public override ValueType ValueType
		{
			get
			{
				return ValueType.DateTime;
			}
		}

		public DateTime AsDateTime
		{
			get
			{
				return this.m_value;
			}
		}

		#endregion

		#region Public Methods

		public override string ToString()
		{
			bool hasDate = this.m_value.Date != DateTime.MinValue;
			bool hasTime = this.m_value.TimeOfDay != TimeSpan.Zero;

			string result;
			if (hasDate && hasTime)
			{
				result = this.m_value.ToString();
			}
			else if (hasTime)
			{
				result = this.m_value.ToLongTimeString();
			}
			else
			{
				result = this.m_value.ToShortDateString();
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
			List<DisplayFormat> result = new List<DisplayFormat>(3);

			result.Add(new DisplayFormat(this.ToString(calc)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_LongDateTime, this.m_value.ToString("F", CultureInfo.CurrentCulture)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_ShortDateTime, this.m_value.ToString("G", CultureInfo.CurrentCulture)));

			return result;
		}

		public static bool TryParse(string text, out DateTimeValue dateTimeValue)
		{
			bool result = false;
			dateTimeValue = null;

			if (!Utility.IsNullOrWhiteSpace(text))
			{
				// Remove the delimiters if both are present.
				text = Utility.StripDelimiters(text, StartDelimiter, EndDelimiter);

				DateTime value;
				if (DateTime.TryParse(text, DateTimeFormatInfo.CurrentInfo,
					DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault,
					out value))
				{
					dateTimeValue = new DateTimeValue(value);
					result = true;
				}
			}

			return result;
		}

		public static DateTimeValue Add(DateTimeValue x, TimeSpanValue y)
		{
			return new DateTimeValue(x.m_value + y.AsTimeSpan);
		}

		public static TimeSpanValue Subtract(DateTimeValue x, DateTimeValue y)
		{
			return new TimeSpanValue(x.m_value - y.m_value);
		}

		public static DateTimeValue Subtract(DateTimeValue x, TimeSpanValue y)
		{
			return new DateTimeValue(x.m_value - y.AsTimeSpan);
		}

		public override bool Equals(object obj)
		{
			DateTimeValue value = obj as DateTimeValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.m_value.GetHashCode();
		}

		public static int Compare(DateTimeValue x, DateTimeValue y)
		{
			int result;
			if (!CompareWithNulls(x, y, out result))
			{
				result = x.m_value.CompareTo(y.m_value);
			}

			return result;
		}

		public int CompareTo(DateTimeValue other)
		{
			return Compare(this, other);
		}

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

		#region Internal Constants

		internal const char StartDelimiter = '"';
		internal const char EndDelimiter = '"';

		#endregion

		#region Private Data Members

		private DateTime m_value;

		#endregion
	}
}
