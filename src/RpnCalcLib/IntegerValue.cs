#region Using Directives

using System;
using System.Collections.Generic;
using System.Numerics;
using Menees.RpnCalc.Internal;
using System.Globalization;

#endregion

namespace Menees.RpnCalc
{
	public sealed class IntegerValue : NumericValue, IComparable<IntegerValue>
	{
		#region Constructors

		public IntegerValue(BigInteger value)
		{
			this.value = value;
		}

		#endregion

		#region Public Properties

		public override ValueType ValueType
		{
			get
			{
				return ValueType.Integer;
			}
		}

		public BigInteger AsInteger
		{
			get
			{
				return this.value;
			}
		}

		#endregion

		#region Public Methods

		public override string ToString()
		{
			// Use the "round-trip" format, so we can see more than the 50 most significant digits.
			return this.value.ToString("R", CultureInfo.CurrentCulture);
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new List<DisplayFormat>(3);

			result.Add(new DisplayFormat(this.ToString(calc)));

			// BigInteger doesn't support the "N" format, so we'll only do it for "normal-sized" values.
			if (this.value >= long.MinValue && this.value <= long.MaxValue)
			{
				result.Add(new DisplayFormat(Resources.DisplayFormat_Formatted, ((long)this.value).ToString("N0", CultureInfo.CurrentCulture)));
			}

			result.Add(new DisplayFormat(Resources.DisplayFormat_Hexadecimal, "0x" + this.value.ToString("X", CultureInfo.CurrentCulture)));

			return result;
		}

		public static bool TryParse(string text, out IntegerValue integerValue)
		{
			bool result = false;
			integerValue = null;

			if (Utility.TryParse(text, out BigInteger value))
			{
				integerValue = new IntegerValue(value);
				result = true;
			}

			return result;
		}

		public override double ToDouble()
		{
			return (double)this.value;
		}

		public override BigInteger ToInteger()
		{
			return this.value;
		}

		public static IntegerValue Add(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.value + y.value);
		}

		public static IntegerValue Subtract(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.value - y.value);
		}

		public static IntegerValue Multiply(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.value * y.value);
		}

		public static NumericValue Divide(IntegerValue x, IntegerValue y)
		{
			FractionValue fraction = new FractionValue(x.value, y.value);
			NumericValue result;
			if (fraction.Denominator == 1)
			{
				result = new IntegerValue(fraction.Numerator);
			}
			else
			{
				result = fraction;
			}

			return result;
		}

		public static IntegerValue Negate(IntegerValue x)
		{
			return new IntegerValue(BigInteger.Negate(x.value));
		}

		public static NumericValue Power(IntegerValue x, IntegerValue exponent)
		{
			if (exponent.value < 0 || exponent.value > int.MaxValue)
			{
				return FractionValue.Power(
					new FractionValue(x.value, BigInteger.One),
					new FractionValue(exponent.value, BigInteger.One));
			}
			else
			{
				return new IntegerValue(BigInteger.Pow(x.value, (int)exponent.value));
			}
		}

		public static FractionValue Invert(IntegerValue x)
		{
			return new FractionValue(BigInteger.One, x.value);
		}

		public static IntegerValue Modulus(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.value % y.value);
		}

		public static BigInteger Factorial(BigInteger x)
		{
			if (x < BigInteger.Zero)
			{
				throw new ArgumentOutOfRangeException();
			}

			// 0! = 1 by definition.  http://mathforum.org/library/drmath/view/57128.html
			// 1! = 1 by definition too, obviously.
			BigInteger result = BigInteger.One;
			for (BigInteger i = 2; i <= x; i++)
			{
				result *= i;
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			IntegerValue value = obj as IntegerValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		public static int Compare(IntegerValue x, IntegerValue y)
		{
			if (!CompareWithNulls(x, y, out int result))
			{
				result = x.value.CompareTo(y.value);
			}

			return result;
		}

		public int CompareTo(IntegerValue other)
		{
			return Compare(this, other);
		}

		#endregion

		#region Public Operators

		public static IntegerValue operator +(IntegerValue x, IntegerValue y)
		{
			return Add(x, y);
		}

		public static IntegerValue operator -(IntegerValue x, IntegerValue y)
		{
			return Subtract(x, y);
		}

		public static IntegerValue operator *(IntegerValue x, IntegerValue y)
		{
			return Multiply(x, y);
		}

		public static NumericValue operator /(IntegerValue x, IntegerValue y)
		{
			return Divide(x, y);
		}

		public static IntegerValue operator %(IntegerValue x, IntegerValue y)
		{
			return Modulus(x, y);
		}

		public static IntegerValue operator +(IntegerValue x)
		{
			return x;
		}

		public static IntegerValue operator -(IntegerValue x)
		{
			return Negate(x);
		}

		public static bool operator ==(IntegerValue x, IntegerValue y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(IntegerValue x, IntegerValue y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(IntegerValue x, IntegerValue y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(IntegerValue x, IntegerValue y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(IntegerValue x, IntegerValue y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(IntegerValue x, IntegerValue y)
		{
			return Compare(x, y) >= 0;
		}

		#endregion

		#region Private Data Members

		private BigInteger value;

		#endregion
	}
}
