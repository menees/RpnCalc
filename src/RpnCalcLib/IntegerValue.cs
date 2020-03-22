namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Numerics;
	using Menees.RpnCalc.Internal;

	#endregion

	public sealed class IntegerValue : NumericValue, IComparable<IntegerValue>
	{
		#region Constructors

		public IntegerValue(BigInteger value)
		{
			this.AsInteger = value;
		}

		#endregion

		#region Public Properties

		public override RpnValueType ValueType
		{
			get
			{
				return RpnValueType.Integer;
			}
		}

		public BigInteger AsInteger { get; private set; }

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

		#region Public Methods

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

		public static IntegerValue Add(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.AsInteger + y.AsInteger);
		}

		public static IntegerValue Subtract(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.AsInteger - y.AsInteger);
		}

		public static IntegerValue Multiply(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.AsInteger * y.AsInteger);
		}

		public static NumericValue Divide(IntegerValue x, IntegerValue y)
		{
			FractionValue fraction = new FractionValue(x.AsInteger, y.AsInteger);
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
			return new IntegerValue(BigInteger.Negate(x.AsInteger));
		}

		public static NumericValue Power(IntegerValue x, IntegerValue exponent)
		{
			NumericValue result;

			if (exponent.AsInteger < 0 || exponent.AsInteger > int.MaxValue)
			{
				result = FractionValue.Power(
					new FractionValue(x.AsInteger, BigInteger.One),
					new FractionValue(exponent.AsInteger, BigInteger.One));
			}
			else
			{
				result = new IntegerValue(BigInteger.Pow(x.AsInteger, (int)exponent.AsInteger));
			}

			return result;
		}

		public static FractionValue Invert(IntegerValue x)
		{
			return new FractionValue(BigInteger.One, x.AsInteger);
		}

		public static IntegerValue Modulus(IntegerValue x, IntegerValue y)
		{
			return new IntegerValue(x.AsInteger % y.AsInteger);
		}

		public static BigInteger Factorial(BigInteger x)
		{
			if (x < BigInteger.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(x), "Factorial can't be used on negative integers.");
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

		public static int Compare(IntegerValue x, IntegerValue y)
		{
			if (!CompareWithNulls(x, y, out int result))
			{
				result = x.AsInteger.CompareTo(y.AsInteger);
			}

			return result;
		}

		public override string ToString()
		{
			// Use the "round-trip" format, so we can see more than the 50 most significant digits.
			return this.AsInteger.ToString("R", CultureInfo.CurrentCulture);
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new List<DisplayFormat>(3);

			result.Add(new DisplayFormat(this.ToString(calc)));

			// BigInteger doesn't support the "N" format, so we'll only do it for "normal-sized" values.
			if (this.AsInteger >= long.MinValue && this.AsInteger <= long.MaxValue)
			{
				result.Add(new DisplayFormat(Resources.DisplayFormat_Formatted, ((long)this.AsInteger).ToString("N0", CultureInfo.CurrentCulture)));
			}

			result.Add(new DisplayFormat(Resources.DisplayFormat_Hexadecimal, "0x" + this.AsInteger.ToString("X", CultureInfo.CurrentCulture)));

			return result;
		}

		public override double ToDouble()
		{
			return (double)this.AsInteger;
		}

		public override BigInteger ToInteger()
		{
			return this.AsInteger;
		}

		public override bool Equals(object obj)
		{
			IntegerValue value = obj as IntegerValue;
			return Compare(this, value) == 0;
		}

		public override int GetHashCode()
		{
			return this.AsInteger.GetHashCode();
		}

		public int CompareTo(IntegerValue other)
		{
			return Compare(this, other);
		}

		#endregion
	}
}
