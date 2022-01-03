namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Globalization;
	using System.Numerics;
	using Menees.RpnCalc.Internal;

	#endregion

	public abstract class NumericValue : Value
	{
		#region Private Data Members

		private static readonly FractionValue Half = new(BigInteger.One, 2);

		#endregion

		#region Constructors

		protected NumericValue()
		{
		}

		#endregion

		#region Public Methods

		public static NumericValue Power(NumericValue x, NumericValue y)
		{
			NumericValue? result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case RpnValueType.Binary:
						result = BinaryValue.Power((BinaryValue)x, (BinaryValue)y);
						break;
					case RpnValueType.Complex:
						result = ComplexValue.Power((ComplexValue)x, (ComplexValue)y);
						break;
					case RpnValueType.Double:
						result = DoubleValue.Power((DoubleValue)x, (DoubleValue)y);
						break;
					case RpnValueType.Fraction:
						result = FractionValue.Power((FractionValue)x, (FractionValue)y);
						break;
					case RpnValueType.Integer:
						result = IntegerValue.Power((IntegerValue)x, (IntegerValue)y);
						break;
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.NumericValue_Power, x, y);
			}

			return result;
		}

		public static NumericValue Invert(NumericValue value)
		{
			NumericValue result;
			switch (value.ValueType)
			{
				case RpnValueType.Binary:
					result = BinaryValue.Invert((BinaryValue)value);
					break;
				case RpnValueType.Complex:
					result = ComplexValue.Invert((ComplexValue)value);
					break;
				case RpnValueType.Double:
					result = DoubleValue.Invert((DoubleValue)value);
					break;
				case RpnValueType.Fraction:
					result = FractionValue.Invert((FractionValue)value);
					break;
				case RpnValueType.Integer:
					result = IntegerValue.Invert((IntegerValue)value);
					break;
				default:
					throw InvalidOp(Resources.NumericValue_Invert, value);
			}

			return result;
		}

		public static NumericValue Modulus(NumericValue x, NumericValue y, Calculator calc)
		{
			NumericValue? result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case RpnValueType.Binary:
						result = BinaryValue.Modulus((BinaryValue)x, (BinaryValue)y, calc);
						break;
					case RpnValueType.Double:
						result = (DoubleValue)x % (DoubleValue)y;
						break;
					case RpnValueType.Fraction:
						result = (FractionValue)x % (FractionValue)y;
						break;
					case RpnValueType.Integer:
						result = (IntegerValue)x % (IntegerValue)y;
						break;
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.NumericValue_Modulus, x, y);
			}

			return result;
		}

		public static NumericValue Sqrt(NumericValue value)
		{
			NumericValue result = Power(value, Half);
			return result;
		}

		public static NumericValue Gcd(NumericValue x, NumericValue y)
		{
			NumericValue? result = null;

			if (HandleImplicitTypeConversion(ref x, ref y))
			{
				switch (x.ValueType)
				{
					case RpnValueType.Binary:
						var binGcd = BigInteger.GreatestCommonDivisor(((BinaryValue)x).ToInteger(), ((BinaryValue)y).ToInteger());
						result = new BinaryValue((ulong)binGcd);
						break;
					case RpnValueType.Double:
						result = new DoubleValue(Utility.Gcd(((DoubleValue)x).AsDouble, ((DoubleValue)y).AsDouble));
						break;
					case RpnValueType.Fraction:
						result = FractionValue.Gcd((FractionValue)x, (FractionValue)y);
						break;
					case RpnValueType.Integer:
						var intGcd = BigInteger.GreatestCommonDivisor(((IntegerValue)x).AsInteger, ((IntegerValue)y).AsInteger);
						result = new IntegerValue(intGcd);
						break;
				}
			}

			if (result == null)
			{
				throw InvalidOp(Resources.NumericValue_Gcd, x, y);
			}

			return result;
		}

		public static NumericValue Lcm(NumericValue x, NumericValue y, Calculator calc)
		{
			NumericValue product = (NumericValue)Value.Multiply(x, y, calc);
			NumericValue gcd = Gcd(x, y);
			NumericValue result = (NumericValue)Value.Divide(product, gcd, calc);
			return result;
		}

		public static NumericValue ChangeType(NumericValue value, RpnValueType targetType)
		{
			NumericValue result;

			RpnValueType sourceType = value.ValueType;
			if (sourceType == targetType)
			{
				result = value;
			}
			else
			{
				switch (targetType)
				{
					case RpnValueType.Binary:
						result = new BinaryValue((ulong)value.ToInteger());
						break;
					case RpnValueType.Complex:
						result = new ComplexValue(value.ToComplex());
						break;
					case RpnValueType.Double:
						result = new DoubleValue(value.ToDouble());
						break;
					case RpnValueType.Fraction:
						if (sourceType == RpnValueType.Double || sourceType == RpnValueType.Complex)
						{
							result = Utility.DoubleToFraction(value.ToDouble());
						}
						else
						{
							result = new FractionValue(value.ToInteger(), BigInteger.One);
						}

						break;
					case RpnValueType.Integer:
						result = new IntegerValue(value.ToInteger());
						break;
					default:
						throw new InvalidCastException(string.Format(
							CultureInfo.CurrentCulture,
							Resources.NumericValue_UnableToChangeType,
							value.ValueType,
							targetType));
				}
			}

			return result;
		}

		public abstract double ToDouble();

		public abstract BigInteger ToInteger();

		public virtual Complex ToComplex()
		{
			// This is sufficient for every derived type except ComplexValue.
			double real = this.ToDouble();
			return new Complex(real, 0);
		}

		#endregion

		#region Internal Methods

		internal static bool HandleImplicitTypeConversion(ref NumericValue x, ref NumericValue y)
		{
			if (x == null || y == null)
			{
				throw new ArgumentNullException(x == null ? nameof(x) : nameof(y));
			}

			RpnValueType xType = x.ValueType;
			RpnValueType yType = y.ValueType;

			// NumericValue types can be implicitly converted up in the following order:
			//  Binary --> Integer --> Fraction --> Double --> Complex.
			// A conversion from Integer or Fraction to Double can fail if the value has
			// more than 308 digits, and it will lose precision if it has more than 15 digits.
			if (xType < yType)
			{
				x = ChangeType(x, yType);
			}
			else if (xType > yType)
			{
				y = ChangeType(y, xType);
			}

			return x.ValueType == y.ValueType;
		}

		#endregion
	}
}
