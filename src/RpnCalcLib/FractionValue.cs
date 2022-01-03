namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Numerics;
	using System.Text;
	using Menees.RpnCalc.Internal;
	using Numerics;

	#endregion

	public sealed class FractionValue : NumericValue, IComparable<FractionValue>
	{
		#region Internal Constants

		// Use '_' like my old Casio fx-85 did.  That's better than using '|',
		// which looks weird when entering mixed fractions.
		internal const char EntrySeparator = '_';
		internal const char DisplaySeparator = '/';

		#endregion

		#region Private Data Members

		private BigRational value;

		#endregion

		#region Constructors

		public FractionValue(BigInteger numerator, BigInteger denominator)
		{
			this.value = new BigRational(numerator, denominator);
		}

		/// <summary>
		/// Creates a fraction value from a decimal value.
		/// </summary>
		/// <remarks>
		/// Decimal values convert to fractions <i>much</i> cleaner than
		/// doubles.  Due to double's base-2 storage format, most
		/// doubles will convert to ugly, large fractions to capture
		/// the "exact" double value.  For example 0.33 converts to
		/// 5944751508129055/18014398509481984.  Yikes!
		/// <para/>
		/// However, by converting to decimal first, which uses
		/// base-10 storage, the fractions come out much more
		/// like you'd expect.  For example, 0.33 --> 33/100.
		/// </remarks>
		/// <devnote>
		/// The MS developer for BigRational discusses this at:
		/// http://bcl.codeplex.com/Thread/View.aspx?ThreadId=217082
		/// </devnote>
		public FractionValue(decimal value)
		{
			this.value = new BigRational(value);
		}

		// See comments here and in TryParse for why this is internal.
		internal FractionValue(BigInteger whole, BigInteger numerator, BigInteger denominator)
		{
			// Make numerator always have the same sign as the whole portion,
			// and make denominator always non-negative.  That gets rid of some
			// ambiguous cases.  Without this, then (-2,1,2) is interpreted by
			// BigRational as (-2*2+1)/2, which is -3/2 instead of -5/2.  The math
			// makes sense and GetWholePart and GetFractionalPart behave
			// consistently.  But it's unintuitive to me because (2,1,2) gives 5/2.
			//
			// So I'm going to make FractionValue work the way I want and only
			// use the sign from the whole portion.  In standard math notation
			// for mixed fractions, the sign distributes to both the whole and
			// fractional portions.  I'm going with Dr. Math's approach of treating
			// a b/c like a + b/c and -a b/c like -(a + b/c).
			// http://mathforum.org/library/drmath/view/69479.html
			numerator = whole.Sign * BigInteger.Abs(numerator);
			denominator = BigInteger.Abs(denominator);
			this.value = new BigRational(whole, numerator, denominator);
		}

		private FractionValue(BigRational value)
		{
			this.value = value;
		}

		#endregion

		#region Public Properties

		public override RpnValueType ValueType
		{
			get
			{
				return RpnValueType.Fraction;
			}
		}

		public BigInteger Numerator
		{
			get
			{
				return this.value.Numerator;
			}
		}

		public BigInteger Denominator
		{
			get
			{
				return this.value.Denominator;
			}
		}

		public new int Sign
		{
			get
			{
				return this.value.Sign;
			}
		}

		#endregion

		#region Public Operators

		public static FractionValue operator +(FractionValue x, FractionValue y)
		{
			return Add(x, y);
		}

		public static FractionValue operator -(FractionValue x, FractionValue y)
		{
			return Subtract(x, y);
		}

		public static FractionValue operator *(FractionValue x, FractionValue y)
		{
			return Multiply(x, y);
		}

		public static FractionValue operator /(FractionValue x, FractionValue y)
		{
			return Divide(x, y);
		}

		public static FractionValue operator %(FractionValue x, FractionValue y)
		{
			return Modulus(x, y);
		}

		public static FractionValue operator +(FractionValue x)
		{
			return x;
		}

		public static FractionValue operator -(FractionValue x)
		{
			return Negate(x);
		}

		public static bool operator ==(FractionValue x, FractionValue y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(FractionValue x, FractionValue y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(FractionValue x, FractionValue y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(FractionValue x, FractionValue y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(FractionValue x, FractionValue y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(FractionValue x, FractionValue y)
		{
			return Compare(x, y) >= 0;
		}

		#endregion

		#region Public Methods

		public static bool TryParse(string text, [MaybeNullWhen(false)] out FractionValue fractionValue)
		{
			bool result = false;
			fractionValue = null;

			if (!string.IsNullOrWhiteSpace(text))
			{
				// Include a space separator since mixed fractions
				// include it between the whole and fractional parts.
				string[] parts = text.Split(
					new[] { EntrySeparator, DisplaySeparator, ' ' },
					StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length == 2)
				{
					// In a common fraction, the numerator, denominator, or both
					// can be negative, but the denominator must be non-zero.
					if (Utility.TryParse(parts[0], out BigInteger numerator) &&
						Utility.TryParse(parts[1], out BigInteger denominator) &&
						!denominator.IsZero)
					{
						fractionValue = new FractionValue(numerator, denominator);
						result = true;
					}
				}
				else if (parts.Length == 3)
				{
					// In a mixed fraction, I'm requiring that the numerator be non-negative
					// and the denominator be positive.  So the sign of the whole portion
					// determines the sign of the final value.  See comments in our three
					// argument constructor for why.
					if (Utility.TryParse(parts[0], out BigInteger whole) &&
						Utility.TryParse(parts[1], out BigInteger numerator) &&
						Utility.TryParse(parts[2], out BigInteger denominator) &&
						numerator.Sign >= 0 && denominator.Sign > 0)
					{
						fractionValue = new FractionValue(whole, numerator, denominator);
						result = true;
					}
				}
			}

			return result;
		}

		public static FractionValue Add(FractionValue x, FractionValue y)
		{
			return new FractionValue(x.value + y.value);
		}

		public static FractionValue Subtract(FractionValue x, FractionValue y)
		{
			return new FractionValue(x.value - y.value);
		}

		public static FractionValue Multiply(FractionValue x, FractionValue y)
		{
			return new FractionValue(x.value * y.value);
		}

		public static FractionValue Divide(FractionValue x, FractionValue y)
		{
			return new FractionValue(x.value / y.value);
		}

		public static FractionValue Negate(FractionValue x)
		{
			return new FractionValue(-x.value);
		}

		public static NumericValue Power(FractionValue x, FractionValue exponent)
		{
			NumericValue result;

			// Wikipedia has good info on exponentiation with fractional powers.
			// http://en.wikipedia.org/wiki/Exponentiation#Principal_n-th_root
			if (exponent.Denominator == 1)
			{
				result = new FractionValue(BigRational.Pow(x.value, exponent.Numerator));
			}
			else if (x.Sign < 0 && exponent.Sign > 0 && !exponent.Denominator.IsEven)
			{
				// We can do a better job on odd roots of negative fractions than
				// DoubleValue can.  It ends up deferring to Complex.Pow, which
				// returns non-principal roots.  This will return -2 as the cube root
				// of -8, whereas Complex.Pow would return (1, 1.73205080756888).
				BigRational radicand = BigRational.Pow(x.value, exponent.Numerator);
				double rootPower = 1 / (double)exponent.Denominator;
				double value = radicand.Sign * Math.Pow(Math.Abs((double)radicand), rootPower);
				result = new DoubleValue(value);
			}
			else if (x.Sign > 0 && exponent.Sign > 0)
			{
				// Take the power of the numerator and denominator separately.
				// Then if we end up with two integers, we can make a fraction.
				double exponentDouble = exponent.ToDouble();
				double resultNumeratorDouble = Math.Pow((double)x.Numerator, exponentDouble);
				double resultDenominatorDouble = Math.Pow((double)x.Denominator, exponentDouble);
				if (Utility.IsInteger(resultNumeratorDouble, out BigInteger resultNumerator) &&
					Utility.IsInteger(resultDenominatorDouble, out BigInteger resultDenominator))
				{
					result = new FractionValue(resultNumerator, resultDenominator);
				}
				else
				{
					result = new DoubleValue(resultNumeratorDouble / resultDenominatorDouble);
				}
			}
			else
			{
				result = DoubleValue.Power(new DoubleValue(x.ToDouble()), new DoubleValue(exponent.ToDouble()));
			}

			return result;
		}

		public static FractionValue Invert(FractionValue x)
		{
			return new FractionValue(BigRational.Invert(x.value));
		}

		public static FractionValue Modulus(FractionValue x, FractionValue y)
		{
			return new FractionValue(x.value % y.value);
		}

		public static IntegerValue Ceiling(FractionValue x)
		{
			BigInteger result = BigInteger.DivRem(x.Numerator, x.Denominator, out BigInteger remainder);
			if (remainder > BigInteger.Zero)
			{
				result++;
			}

			return new IntegerValue(result);
		}

		public static IntegerValue Floor(FractionValue x)
		{
			BigInteger result = BigInteger.DivRem(x.Numerator, x.Denominator, out BigInteger remainder);
			if (remainder < BigInteger.Zero)
			{
				result--;
			}

			return new IntegerValue(result);
		}

		public static FractionValue Gcd(FractionValue x, FractionValue y)
		{
			BigRational rX = x.value;
			BigRational rY = y.value;

			BigRational result;
			if ((rX == BigRational.Zero) && (rY == BigRational.Zero))
			{
				result = BigRational.One;
			}
			else
			{
				int iteration = 0;
				while (rY != BigRational.Zero)
				{
					BigRational remainder = rX % rY;
					rX = rY;
					rY = remainder;

					Utility.CheckGcdLoopIterations(iteration++);
				}

				result = rX;
			}

			return new FractionValue(result);
		}

		public static int Compare(FractionValue? x, FractionValue? y)
		{
			if (!CompareWithNulls(x, y, out int result))
			{
				result = x.value.CompareTo(y.value);
			}

			return result;
		}

		public override string ToString()
		{
			return GetMixedFormat(this.value, DisplaySeparator);
		}

		public override string ToString(Calculator calc)
		{
			string result;

			switch (calc.FractionFormat)
			{
				case FractionFormat.Mixed:
					result = GetMixedFormat(this.value, DisplaySeparator);
					break;
				case FractionFormat.Decimal:
					result = GetDecimalFormat(this.value, DisplaySeparator, calc, out _);
					break;
				default:
					result = GetCommonFormat(this.value, DisplaySeparator);
					break;
			}

			return result;
		}

		public override string GetEntryValue(Calculator calc)
		{
			string result;

			switch (calc.FractionFormat)
			{
				case FractionFormat.Mixed:
					result = GetMixedFormat(this.value, EntrySeparator);
					break;
				default:
					// I'm intentionally handling FractionFormat.Decimal
					// with the Common format because entry values
					// should always use a lossless format.  If I let them
					// edit it as a decimal, then precision would be lost,
					// and the parser wouldn't convert it back into a
					// fraction when they entered the value.
					result = GetCommonFormat(this.value, EntrySeparator);
					break;
			}

			return result;
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new(3);

			// The mixed and common formats are identical unless the whole part is non-zero.
			if (!this.value.GetWholePart().IsZero)
			{
				result.Add(new DisplayFormat(Resources.DisplayFormat_Mixed, GetMixedFormat(this.value, DisplaySeparator)));
			}

			result.Add(new DisplayFormat(Resources.DisplayFormat_Common, GetCommonFormat(this.value, DisplaySeparator)));

			// If BigInteger division overflows what a double can hold, then we'll actually get
			// back a fractional form instead.  In that case, we don't need to show it again.
			string decimalFormat = GetDecimalFormat(this.value, DisplaySeparator, calc, out bool isDecimalFormat);
			if (isDecimalFormat)
			{
				result.Add(new DisplayFormat(Resources.DisplayFormat_Decimal, decimalFormat));
			}

			return result;
		}

		public override double ToDouble()
		{
			return (double)this.value;
		}

		public override BigInteger ToInteger()
		{
			return (BigInteger)this.value;
		}

		public IntegerValue GetWholePart()
		{
			return new IntegerValue(this.value.GetWholePart());
		}

		public FractionValue GetFractionalPart()
		{
			return new FractionValue(this.value.GetFractionPart());
		}

		public override bool Equals(object? obj)
		{
			FractionValue? value = obj as FractionValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		public int CompareTo(FractionValue? other)
		{
			return Compare(this, other);
		}

		#endregion

		#region Private Methods

		private static string GetMixedFormat(BigRational value, char separator)
		{
			StringBuilder sb = new();

			BigInteger whole = value.GetWholePart();
			BigRational fractionPart = value.GetFractionPart();
			if (!whole.IsZero)
			{
				sb.Append(whole.ToString("R", CultureInfo.CurrentCulture));
				if (separator != DisplaySeparator)
				{
					sb.Append(separator);
				}
				else
				{
					// When the DisplaySeparator is being used,
					// then the whole and fractional parts need
					// to be separated by whitespace.
					sb.Append(' ');
				}

				// If the rational number was negative, then the
				// whole part will have been rendered as a negative
				// number, so we need to ensure that the fractional
				// part doesn't render as a negative value too.
				fractionPart = BigRational.Abs(fractionPart);
			}

			AppendCommonFormat(sb, fractionPart, separator);

			return sb.ToString();
		}

		private static string GetCommonFormat(BigRational value, char separator)
		{
			StringBuilder sb = new();

			AppendCommonFormat(sb, value, separator);

			return sb.ToString();
		}

		private static StringBuilder AppendCommonFormat(StringBuilder sb, BigRational value, char separator)
		{
			sb.Append(value.Numerator.ToString("R", CultureInfo.CurrentCulture));
			sb.Append(separator);
			sb.Append(value.Denominator.ToString("R", CultureInfo.CurrentCulture));
			return sb;
		}

		private static string GetDecimalFormat(BigRational value, char separator, Calculator calc, out bool isDecimalFormat)
		{
			string result;

			// If converting from a fraction to a double overflows, we'll return the common format instead.
			double doubleValue = (double)value;
			if (double.IsInfinity(doubleValue) || double.IsNaN(doubleValue))
			{
				result = GetCommonFormat(value, separator);
				isDecimalFormat = false;
			}
			else
			{
				result = DoubleValue.Format(doubleValue, calc);
				isDecimalFormat = true;
			}

			return result;
		}

		#endregion
	}
}
