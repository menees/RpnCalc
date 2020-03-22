#region Using Directives

using System;

#endregion

namespace Menees.RpnCalc
{
	#region AngleMode

	public enum AngleMode
	{
		Radians,
		Degrees,
	}

	#endregion

	#region BinaryFormat

	public enum BinaryFormat
	{
		// Note: BinaryValue.TryParseSuffixedText depends on the
		// field value equaling the base value.
		Unknown = 0,
		Binary = 2,
		Octal = 8,
		Decimal = 10,
		Hexadecimal = 16,
	}

	#endregion

	#region ComplexFormat

	public enum ComplexFormat
	{
		Rectangular,
		Polar,
	}

	#endregion

	#region DecimalFormat

	public enum DecimalFormat
	{
		Standard,
		Fixed,
		Scientific,
	}

	#endregion

	#region FractionFormat

	public enum FractionFormat
	{
		// I got the name "Common" from:
		// http://en.wikipedia.org/wiki/Vulgar_fraction#Vulgar.2C_proper.2C_and_improper_fractions
		Common,
		Mixed,
		Decimal,
	}

	#endregion

	#region ValueType

	public enum ValueType
	{
		// Group all the numeric types together in order of "range size". This makes
		// type coercion easier in NumericValue.HandleImplicitTypeConversion.
		Binary,
		Integer,
		Fraction,
		Double,
		Complex,

		// The rest of the types are non-numeric.
		DateTime,
		TimeSpan,
	}

	#endregion
}
