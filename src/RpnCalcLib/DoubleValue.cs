﻿namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Numerics;

	#endregion

	public sealed class DoubleValue : NumericValue, IComparable<DoubleValue>
	{
		#region Constructors

		public DoubleValue(double value)
		{
			this.AsDouble = value;
		}

		#endregion

		#region Public Properties

		public override RpnValueType ValueType
		{
			get
			{
				return RpnValueType.Double;
			}
		}

		public double AsDouble { get; private set; }

		#endregion

		#region Public Operators

		public static DoubleValue operator +(DoubleValue x, DoubleValue y)
		{
			return Add(x, y);
		}

		public static DoubleValue operator -(DoubleValue x, DoubleValue y)
		{
			return Subtract(x, y);
		}

		public static DoubleValue operator *(DoubleValue x, DoubleValue y)
		{
			return Multiply(x, y);
		}

		public static DoubleValue operator /(DoubleValue x, DoubleValue y)
		{
			return Divide(x, y);
		}

		public static DoubleValue operator %(DoubleValue x, DoubleValue y)
		{
			return Modulus(x, y);
		}

		public static DoubleValue operator +(DoubleValue x)
		{
			return x;
		}

		public static DoubleValue operator -(DoubleValue x)
		{
			return Negate(x);
		}

		public static bool operator ==(DoubleValue x, DoubleValue y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(DoubleValue x, DoubleValue y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(DoubleValue x, DoubleValue y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(DoubleValue x, DoubleValue y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(DoubleValue x, DoubleValue y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(DoubleValue x, DoubleValue y)
		{
			return Compare(x, y) >= 0;
		}

		#endregion

		#region Public Methods

		public static string Format(double value, Calculator calc)
		{
			string result;

			switch (calc.DecimalFormat)
			{
				case DecimalFormat.Fixed:
					result = GetFixedFormat(value, calc);
					break;
				case DecimalFormat.Scientific:
					result = GetScientificFormat(value, calc);
					break;
				default:
					result = GetStandardFormat(value);
					break;
			}

			return result;
		}

		public static bool TryParse(string text, [MaybeNullWhen(false)] out DoubleValue doubleValue)
		{
			bool result = false;
			doubleValue = null;

			if (double.TryParse(text, out double value))
			{
				doubleValue = new DoubleValue(value);
				result = true;
			}

			return result;
		}

		public static DoubleValue Add(DoubleValue x, DoubleValue y)
		{
			return new DoubleValue(x.AsDouble + y.AsDouble);
		}

		public static DoubleValue Subtract(DoubleValue x, DoubleValue y)
		{
			return new DoubleValue(x.AsDouble - y.AsDouble);
		}

		public static DoubleValue Multiply(DoubleValue x, DoubleValue y)
		{
			return new DoubleValue(x.AsDouble * y.AsDouble);
		}

		public static DoubleValue Divide(DoubleValue x, DoubleValue y)
		{
			if (y.AsDouble == 0)
			{
				throw new DivideByZeroException();
			}

			return new DoubleValue(x.AsDouble / y.AsDouble);
		}

		public static DoubleValue Negate(DoubleValue x)
		{
			return new DoubleValue(-x.AsDouble);
		}

		public static NumericValue Power(DoubleValue x, DoubleValue exponent)
		{
			NumericValue result;

			double value = Math.Pow(x.AsDouble, exponent.AsDouble);
			if (double.IsNaN(value) && x.AsDouble < 0)
			{
				// They tried to take a fractional root of a negative number,
				// so we have to return a complex number instead.
				result = ComplexValue.Power(new ComplexValue(x.AsDouble, 0), exponent);
			}
			else
			{
				result = new DoubleValue(value);
			}

			return result;
		}

		public static DoubleValue Invert(DoubleValue x)
		{
			if (x.AsDouble == 0)
			{
				throw new DivideByZeroException();
			}

			return new DoubleValue(1.0 / x.AsDouble);
		}

		public static DoubleValue Modulus(DoubleValue x, DoubleValue y)
		{
			if (y.AsDouble == 0)
			{
				throw new DivideByZeroException();
			}

			return new DoubleValue(x.AsDouble % y.AsDouble);
		}

		public static int Compare(DoubleValue? x, DoubleValue? y)
		{
			if (!CompareWithNulls(x, y, out int result))
			{
				result = x.AsDouble.CompareTo(y.AsDouble);
			}

			return result;
		}

		public override string ToString()
		{
			return this.AsDouble.ToString(CultureInfo.CurrentCulture);
		}

		public override string ToString(Calculator calc)
		{
			return Format(this.AsDouble, calc);
		}

		public override string GetEntryValue(Calculator calc)
		{
			// The entry value must show all the precision available, not just what the
			// current display mode is limited to.  For example, if the display format is
			// Fixed 2 Digits, we don't want to return 0.12 if the stored value is 0.1234.
			// For maximum precision, we'll use the "round-trip" format code because it
			// can use up to 17 digits in certain cases.  That helps us minimize round-off
			// errors when we save and re-load the stack from XML (since saving uses
			// GetEntryValue).
			string result = GetFormat(this.AsDouble, "R");
			return result;
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new(4);

			result.Add(new DisplayFormat(GetStandardFormat(this.AsDouble)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Formatted, GetFormat(this.AsDouble, "N")));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Fixed, GetFixedFormat(this.AsDouble, calc)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Scientific, GetScientificFormat(this.AsDouble, calc)));

			return result;
		}

		public override double ToDouble()
		{
			return this.AsDouble;
		}

		public override BigInteger ToInteger()
		{
			return (BigInteger)this.AsDouble;
		}

		public override bool Equals(object? obj)
		{
			DoubleValue? value = obj as DoubleValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.AsDouble.GetHashCode();
		}

		public int CompareTo(DoubleValue? other)
		{
			return Compare(this, other);
		}

		#endregion

		#region Private Methods

		private static string GetStandardFormat(double value)
		{
			// .NET's "G" format uses 15 digits of precision by default for a double, so it ends up showing lots of
			// .999... sequences.  For example, 1644.6 - 1605.9 = 38.666666666668 instead of 38.7.  So I'm
			// going to use 12 here, which is what I did in RPN Calc 2 in FloatToStr in EVFunctions.cpp.  The
			// visual loss of those three digits shouldn't affect any use case I care about, and if a user really
			// needs to see them they can always do so with the Edit command, which shows all 18 sig. digits.
			//
			// To put the difference of 12 vs. 15 decimal places in perspective, think of GPS coordinates where
			// degrees can be translated into miles, feet, inches, etc.  12 decimal places gives a precision of
			// 0.00000437184 inches.  That's 0.000111 mm, which is 0.111 micrometers or 111 nanometers.
			// An average bacteria is about 1000 nm, and most viruses are 20 to 250 nm.  Trying to be precise
			// to 15 decimal digits gets us into sub-nanometer measurements, which are used to measure things
			// like the width of a single water molecule (0.280 nm).  To better visualize the ridiculous smallness
			// of this, check out "The Scale of the Universe" app at http://www.newgrounds.com/portal/view/525347
			//
			// (The initial conversion of GPS degrees to inches came from http://www.sanidumps.com/faq/faq_21.php)
			// 1 degree of latitude (and 1 degree of longitude at the equator) equals 60 nautical miles. A nautical
			// mile is 6,076.11549...(it goes on and on!) feet in length. Close enough to 1.15 statute miles, which
			// makes 1 degree equal to 69 statute miles.  (nautical miles are interesting: 1 minute = 1 nautical mile,
			// so the world is 60*360 nautical miles North South, and around the equator or 21600.
			//
			// decimals degrees miles-statute feet inches
			// 0 1 69 364320 4371840
			// 1 0.1 6.9 36432 437184
			// 2 0.01 0.69 3643.2 43718.4
			// 3 0.001 0.069 364.32 4371.84
			// 4 0.0001 0.0069 36.432 437.184
			// 5 0.00001 0.00069 3.6432 43.7184
			// 6 0.000001 0.000069 0.36432 4.37184
			// The difference between 48.898748 and 48.898749 is 4.4 inches.
			// Four digits, or around 36 feet, is really about all you need for normal navigation.
			return GetFormat(value, "G12");
		}

		private static string GetFixedFormat(double value, Calculator calc)
		{
			return GetFormat(value, "F" + calc.FixedDecimalDigits);
		}

		private static string GetScientificFormat(double value, Calculator calc)
		{
			return GetFormat(value, "e" + calc.FixedDecimalDigits);
		}

		private static string GetFormat(double value, string formatCode)
		{
			string result = value.ToString(formatCode, CultureInfo.CurrentCulture);
			return result;
		}

		#endregion
	}
}
