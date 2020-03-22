namespace Menees.RpnCalc
{
	#region Using Directives

	using System;

	#endregion

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
#pragma warning disable CA1720 // Identifier contains type name
		Decimal = 10,
#pragma warning restore CA1720 // Identifier contains type name
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
#pragma warning disable CA1720 // Identifier contains type name
		Decimal,
#pragma warning restore CA1720 // Identifier contains type name
	}

	#endregion

	#region RpnValueType

	// We can't name this ValueType without causing naming conflicts with System.ValueType
	// when a "using System;" directive is used inside the Menees.RpnCalc namespace.
#pragma warning disable CA1720 // Identifier contains type name. Integer and Double conflict with type names in VB.
	public enum RpnValueType
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
#pragma warning restore CA1720 // Identifier contains type name

	#endregion
}
