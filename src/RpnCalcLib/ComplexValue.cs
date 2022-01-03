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

	#endregion

	public sealed class ComplexValue : NumericValue
	{
		#region Internal Constants

		internal const char PhasePrefix = '@';
		internal const char StartDelimiter = '(';
		internal const char EndDelimiter = ')';

		#endregion

		#region Private Data Members

		private readonly Complex value;

		#endregion

		#region Constructors

		public ComplexValue(Complex value)
		{
			this.value = value;
		}

		public ComplexValue(double real, double imaginary)
		{
			this.value = new Complex(real, imaginary);
		}

		#endregion

		#region Public Properties

		public override RpnValueType ValueType
		{
			get
			{
				return RpnValueType.Complex;
			}
		}

		public Complex AsComplex
		{
			get
			{
				return this.value;
			}
		}

		#endregion

		#region Private Properties

		private static char Separator
		{
			get
			{
				// If the "decimal point" is a comma, then we'll use a semicolon
				// as the number separator when formatting and parsing.  If it's
				// any other string (e.g., "."), then we'll use a comma.  I can't
				// find any info on how other cultures format complex numbers.
				// In .NET 4.0, Complex.ToString() always uses a "," separator!
				string decimalPoint = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
				char result = decimalPoint == "," ? ';' : ',';
				return result;
			}
		}

		#endregion

		#region Public Operators

		public static ComplexValue operator +(ComplexValue x, ComplexValue y)
		{
			return Add(x, y);
		}

		public static ComplexValue operator -(ComplexValue x, ComplexValue y)
		{
			return Subtract(x, y);
		}

		public static ComplexValue operator *(ComplexValue x, ComplexValue y)
		{
			return Multiply(x, y);
		}

		public static ComplexValue operator /(ComplexValue x, ComplexValue y)
		{
			return Divide(x, y);
		}

		public static ComplexValue operator +(ComplexValue x)
		{
			return x;
		}

		public static ComplexValue operator -(ComplexValue x)
		{
			return Negate(x);
		}

		public static bool operator ==(ComplexValue x, ComplexValue y)
		{
			bool result;
			if (!CompareWithNulls(x, y, out int compareResult))
			{
				result = x.value == y.value;
			}
			else
			{
				result = compareResult == 0;
			}

			return result;
		}

		public static bool operator !=(ComplexValue x, ComplexValue y)
		{
			return !(x == y);
		}

		#endregion

		#region Public Methods

		public static bool TryParse(string text, [MaybeNullWhen(false)] out ComplexValue complexValue)
		{
			return TryParse(text, null, out complexValue);
		}

		public static bool TryParse(string text, Calculator? calc, [MaybeNullWhen(false)] out ComplexValue complexValue)
		{
			bool result = false;
			complexValue = null;

			if (!string.IsNullOrWhiteSpace(text))
			{
				text = Utility.StripDelimiters(text.Trim(), StartDelimiter, EndDelimiter);

				// Treat space as a separator, so Split will automatically remove them.
				string[] parts = text.Split(new[] { Separator, ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 2)
				{
					string part0 = parts[0];
					string part1 = parts[1];
					bool inPolarFormat = false;
					if (part1[0] == PhasePrefix)
					{
						part1 = part1.Substring(1, part1.Length - 1);
						inPolarFormat = true;
					}

					if (double.TryParse(part0, out double part0Value) &&
						double.TryParse(part1, out double part1Value))
					{
						Complex value;
						if (inPolarFormat)
						{
							if (calc != null)
							{
								part1Value = calc.ConvertFromAngleToRadians(part1Value);
							}

							value = Complex.FromPolarCoordinates(part0Value, part1Value);
						}
						else
						{
							value = new Complex(part0Value, part1Value);
						}

						complexValue = new ComplexValue(value);
						result = true;
					}
				}
			}

			return result;
		}

		public static ComplexValue Add(ComplexValue x, ComplexValue y)
		{
			return new ComplexValue(x.value + y.value);
		}

		public static ComplexValue Subtract(ComplexValue x, ComplexValue y)
		{
			return new ComplexValue(x.value - y.value);
		}

		public static ComplexValue Multiply(ComplexValue x, ComplexValue y)
		{
			return new ComplexValue(x.value * y.value);
		}

		public static ComplexValue Divide(ComplexValue x, ComplexValue y)
		{
			return new ComplexValue(x.value / y.value);
		}

		public static ComplexValue Negate(ComplexValue x)
		{
			return new ComplexValue(Complex.Negate(x.value));
		}

		public static ComplexValue Power(ComplexValue x, DoubleValue exponent)
		{
			// Unfortunately, Complex.Pow doesn't always return the "principal" root.
			// For example, Complex.Pow((-8,0), 1/3) returns (1, 1.73205080756888)
			// instead of the principal root of -2.  Oh well.  It's a valid answer; it's
			// just not the expected one.
			return new ComplexValue(Complex.Pow(x.value, exponent.AsDouble));
		}

		public static ComplexValue Power(ComplexValue x, ComplexValue exponent)
		{
			return new ComplexValue(Complex.Pow(x.value, exponent.value));
		}

		public static ComplexValue Invert(ComplexValue x)
		{
			return new ComplexValue(Complex.One / x.value);
		}

		public override double ToDouble()
		{
			if (this.value.Imaginary == 0)
			{
				return this.value.Real;
			}
			else
			{
				throw new InvalidCastException(Resources.ComplexValue_CannotCastToScalar);
			}
		}

		public override BigInteger ToInteger()
		{
			if (this.value.Imaginary == 0)
			{
				return (BigInteger)this.value.Real;
			}
			else
			{
				throw new InvalidCastException(Resources.ComplexValue_CannotCastToScalar);
			}
		}

		public override Complex ToComplex()
		{
			return this.value;
		}

		public override string ToString()
		{
			return this.value.ToString();
		}

		public override string ToString(Calculator calc)
		{
			string result;

			if (calc.ComplexFormat == ComplexFormat.Polar)
			{
				result = GetPolarFormat(this.value, calc);
			}
			else
			{
				result = GetRectangularFormat(this.value, calc);
			}

			return result;
		}

		public override IEnumerable<DisplayFormat> GetAllDisplayFormats(Calculator calc)
		{
			List<DisplayFormat> result = new(3);

			result.Add(new DisplayFormat(Resources.DisplayFormat_Algebraic, GetAlgebraicFormat(this.value, calc)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Rectangular, GetRectangularFormat(this.value, calc)));
			result.Add(new DisplayFormat(Resources.DisplayFormat_Polar, GetPolarFormat(this.value, calc)));

			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ComplexValue value && value.value == this.value;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		#endregion

		#region Private Methods

		private static string GetAlgebraicFormat(Complex value, Calculator calc)
		{
			StringBuilder sb = new();

			if (value.Real == 0 && value.Imaginary != 0)
			{
				sb.Append(DoubleValue.Format(value.Imaginary, calc));
				sb.Append('i');
			}
			else
			{
				sb.Append(DoubleValue.Format(value.Real, calc));
				if (value.Imaginary != 0)
				{
					sb.Append(Math.Sign(value.Imaginary) == -1 ? " - " : " + ");
					sb.Append(DoubleValue.Format(Math.Abs(value.Imaginary), calc));
					sb.Append('i');
				}
			}

			return sb.ToString();
		}

		private static string GetRectangularFormat(Complex value, Calculator calc)
		{
			string result = string.Format(
				CultureInfo.CurrentCulture,
				"{3}{0}{2} {1}{4}",
				DoubleValue.Format(value.Real, calc),
				DoubleValue.Format(value.Imaginary, calc),
				Separator,
				StartDelimiter,
				EndDelimiter);
			return result;
		}

		private static string GetPolarFormat(Complex value, Calculator calc)
		{
			string result = string.Format(
				CultureInfo.CurrentCulture,
				"{4}{0}{3} {1}{2}{5}",
				DoubleValue.Format(value.Magnitude, calc),
				PhasePrefix,
				DoubleValue.Format(calc.ConvertFromRadiansToAngle(value.Phase), calc),
				Separator,
				StartDelimiter,
				EndDelimiter);
			return result;
		}

		#endregion
	}
}
