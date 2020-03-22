#region Comments and Copyright

// This code originally came from http://bcl.codeplex.com.  I had to modify it with
// some #if !SILVERLIGHT directives to get it to compile for Silverlight.  [7/24/2010]
//
// I also had to make two #if WINDOWS_PHONE changes for WP7. [2/27/2011]
//
// I made a lot of changes to comply with current code analysis rules and C# features.
// I also removed the SILVERLIGHT and WINDOWS_PHONE directives. [3/22/2020]

// Copyright (c) Microsoft Corporation.  All rights reserved.
/*============================================================
** Class: BigRational
**
** Purpose:
** --------
** This class is used to represent an arbitrary precision
** BigRational number
**
** A rational number (commonly called a fraction) is a ratio
** between two integers.  For example (3/6) = (2/4) = (1/2)
**
** Arithmetic
** ----------
** a/b = c/d, iff ad = bc
** a/b + c/d  == (ad + bc)/bd
** a/b - c/d  == (ad - bc)/bd
** a/b % c/d  == (ad % bc)/bd
** a/b * c/d  == (ac)/(bd)
** a/b / c/d  == (ad)/(bc)
** -(a/b)     == (-a)/b
** (a/b)^(-1) == b/a, if a != 0
**
** Reduction Algorithm
** ------------------------
** Euclid's algorithm is used to simplify the fraction.
** Calculating the greatest common divisor of two n-digit
** numbers can be found in
**
** O(n(log n)^5 (log log n)) steps as n -> +infinity
============================================================*/

#endregion

namespace Numerics
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Numerics;
	using System.Runtime.InteropServices;
	using System.Runtime.Serialization;
	using System.Security.Permissions;
	using System.Text;

	#endregion

#pragma warning disable MEN007 // Use a single return. This would require too many logic changes to Microsoft's code.
	[Serializable]
	[ComVisible(false)]
	public struct BigRational : IComparable, IComparable<BigRational>, IEquatable<BigRational>,
		IDeserializationCallback, ISerializable
	{
		#region Private Data Members

		private const int DoubleMaxScale = 308;
		private const int DecimalScaleMask = 0x00FF0000;
		private const int DecimalSignMask = unchecked((int)0x80000000);
		private const int DecimalMaxScale = 28;
		private const string Solidus = @"/";

		private static readonly BigInteger DoublePrecision = BigInteger.Pow(10, DoubleMaxScale);
		private static readonly BigInteger DoubleMaxValue = (BigInteger)double.MaxValue;
		private static readonly BigInteger DoubleMinValue = (BigInteger)double.MinValue;

		private static readonly BigInteger DecimalPrecision = BigInteger.Pow(10, DecimalMaxScale);
		private static readonly BigInteger DecimalMaxValue = (BigInteger)decimal.MaxValue;
		private static readonly BigInteger DecimalMinValue = (BigInteger)decimal.MinValue;

		private static readonly double NegativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000));

		#endregion Members for Internal Support

		#region Constructors

		public BigRational(BigInteger numerator)
		{
			this.Numerator = numerator;
			this.Denominator = BigInteger.One;
		}

		// BigRational(Double)
		public BigRational(double value)
		{
			if (double.IsNaN(value))
			{
				throw new ArgumentException("Argument is not a number", nameof(value));
			}
			else if (double.IsInfinity(value))
			{
				throw new ArgumentException("Argument is infinity", nameof(value));
			}

			SplitDoubleIntoParts(value, out int sign, out int exponent, out ulong significand, out _);

			if (significand == 0)
			{
				this = Zero;
				return;
			}

			// NOTE: This section contains bug fixes from MS described in:
			// http://bcl.codeplex.com/Thread/View.aspx?ThreadId=217082
			this.Numerator = significand;
			this.Denominator = BigInteger.One;

			if (exponent > 0)
			{
				this.Numerator <<= exponent;
			}
			else if (exponent < 0)
			{
				this.Denominator <<= -exponent;
			}

			if (sign < 0)
			{
				this.Numerator = BigInteger.Negate(this.Numerator);
			}

			this.Simplify();
		}

		// BigRational(Decimal) -
		//
		// The Decimal type represents floating point numbers exactly, with no rounding error.
		// Values such as "0.1" in Decimal are actually representable, and convert cleanly
		// to BigRational as "11/10"
		public BigRational(decimal value)
		{
			int[] bits = decimal.GetBits(value);
			if (bits == null || bits.Length != 4 || (bits[3] & ~(DecimalSignMask | DecimalScaleMask)) != 0 || (bits[3] & DecimalScaleMask) > (28 << 16))
			{
				throw new ArgumentException("invalid Decimal", nameof(value));
			}

			if (value == decimal.Zero)
			{
				this = Zero;
				return;
			}

			// build up the numerator
			unchecked
			{
				ulong ul = (((ulong)(uint)bits[2]) << 32) | ((ulong)(uint)bits[1]);   // (hi    << 32) | (mid)
				this.Numerator = (new BigInteger(ul) << 32) | (uint)bits[0];             // (hiMid << 32) | (low)
			}

			bool isNegative = (bits[3] & DecimalSignMask) != 0;
			if (isNegative)
			{
				this.Numerator = BigInteger.Negate(this.Numerator);
			}

			// build up the denominator
			int scale = (bits[3] & DecimalScaleMask) >> 16;     // 0-28, power of 10 to divide numerator by
			this.Denominator = BigInteger.Pow(10, scale);

			this.Simplify();
		}

		public BigRational(BigInteger numerator, BigInteger denominator)
		{
			if (denominator.Sign == 0)
			{
				throw new DivideByZeroException();
			}
			else if (numerator.Sign == 0)
			{
				// 0/m -> 0/1
				this.Numerator = BigInteger.Zero;
				this.Denominator = BigInteger.One;
			}
			else if (denominator.Sign < 0)
			{
				this.Numerator = BigInteger.Negate(numerator);
				this.Denominator = BigInteger.Negate(denominator);
			}
			else
			{
				this.Numerator = numerator;
				this.Denominator = denominator;
			}

			this.Simplify();
		}

		public BigRational(BigInteger whole, BigInteger numerator, BigInteger denominator)
		{
			if (denominator.Sign == 0)
			{
				throw new DivideByZeroException();
			}
			else if (numerator.Sign == 0 && whole.Sign == 0)
			{
				this.Numerator = BigInteger.Zero;
				this.Denominator = BigInteger.One;
			}
			else if (denominator.Sign < 0)
			{
				this.Denominator = BigInteger.Negate(denominator);
				this.Numerator = (BigInteger.Negate(whole) * this.Denominator) + BigInteger.Negate(numerator);
			}
			else
			{
				this.Denominator = denominator;
				this.Numerator = (whole * denominator) + numerator;
			}

			this.Simplify();
		}

#pragma warning disable CA1801 // Unused parameter. The context parameter is required for .NET deserialization.
		private BigRational(SerializationInfo info, StreamingContext context)
#pragma warning restore CA1801 // Unused parameter
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			this.Numerator = (BigInteger)info.GetValue(nameof(this.Numerator), typeof(BigInteger));
			this.Denominator = (BigInteger)info.GetValue(nameof(this.Denominator), typeof(BigInteger));
		}

		#endregion Constructors

		#region Public Properties
		public static BigRational Zero { get; } = new BigRational(BigInteger.Zero);

		public static BigRational One { get; } = new BigRational(BigInteger.One);

		public static BigRational MinusOne { get; } = new BigRational(BigInteger.MinusOne);

		public int Sign
		{
			get
			{
				return this.Numerator.Sign;
			}
		}

		public BigInteger Numerator { get; private set; }

		public BigInteger Denominator { get; private set; }

		#endregion Public Properties

		#region Explicit conversions from BigRational
		[CLSCompliant(false)]
		public static explicit operator sbyte(BigRational value)
		{
			return (sbyte)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		[CLSCompliant(false)]
		public static explicit operator ushort(BigRational value)
		{
			return (ushort)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		[CLSCompliant(false)]
		public static explicit operator uint(BigRational value)
		{
			return (uint)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		[CLSCompliant(false)]
		public static explicit operator ulong(BigRational value)
		{
			return (ulong)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		public static explicit operator byte(BigRational value)
		{
			return (byte)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		public static explicit operator short(BigRational value)
		{
			return (short)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		public static explicit operator int(BigRational value)
		{
			return (int)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		public static explicit operator long(BigRational value)
		{
			return (long)BigInteger.Divide(value.Numerator, value.Denominator);
		}

		public static explicit operator BigInteger(BigRational value)
		{
			return BigInteger.Divide(value.Numerator, value.Denominator);
		}

		public static explicit operator float(BigRational value)
		{
			// The Single value type represents a single-precision 32-bit number with
			// values ranging from negative 3.402823e38 to positive 3.402823e38
			// values that do not fit into this range are returned as Infinity
			return (float)(double)value;
		}

		public static explicit operator double(BigRational value)
		{
			// The Double value type represents a double-precision 64-bit number with
			// values ranging from -1.79769313486232e308 to +1.79769313486232e308
			// values that do not fit into this range are returned as +/-Infinity
			if (SafeCastToDouble(value.Numerator) && SafeCastToDouble(value.Denominator))
			{
				return (double)value.Numerator / (double)value.Denominator;
			}

			// scale the numerator to preseve the fraction part through the integer division
			BigInteger denormalized = value.Numerator * DoublePrecision / value.Denominator;
			if (denormalized.IsZero)
			{
				return (value.Sign < 0) ? NegativeZero : 0d; // underflow to -+0
			}

			double result = 0;
			bool isDouble = false;
			int scale = DoubleMaxScale;

			while (scale > 0)
			{
				if (!isDouble)
				{
					if (SafeCastToDouble(denormalized))
					{
						result = (double)denormalized;
						isDouble = true;
					}
					else
					{
						denormalized /= 10;
					}
				}

				result /= 10;
				scale--;
			}

#pragma warning disable CC0013 // Use ternary operator
			if (!isDouble)
#pragma warning restore CC0013 // Use ternary operator
			{
				return (value.Sign < 0) ? double.NegativeInfinity : double.PositiveInfinity;
			}
			else
			{
				return result;
			}
		}

		public static explicit operator decimal(BigRational value)
		{
			// The Decimal value type represents decimal numbers ranging
			// from +79,228,162,514,264,337,593,543,950,335 to -79,228,162,514,264,337,593,543,950,335
			// the binary representation of a Decimal value is of the form, ((-2^96 to 2^96) / 10^(0 to 28))
			if (SafeCastToDecimal(value.Numerator) && SafeCastToDecimal(value.Denominator))
			{
				return (decimal)value.Numerator / (decimal)value.Denominator;
			}

			// scale the numerator to preseve the fraction part through the integer division
			BigInteger denormalized = value.Numerator * DecimalPrecision / value.Denominator;
			if (denormalized.IsZero)
			{
				return decimal.Zero; // underflow - fraction is too small to fit in a decimal
			}

			for (int scale = DecimalMaxScale; scale >= 0; scale--)
			{
				if (!SafeCastToDecimal(denormalized))
				{
					denormalized /= 10;
				}
				else
				{
					DecimalUInt32 dec = default;
					dec.Dec = (decimal)denormalized;
					dec.Flags = (dec.Flags & ~DecimalScaleMask) | (scale << 16);
					return dec.Dec;
				}
			}

			throw new OverflowException("Value was either too large or too small for a Decimal.");
		}
		#endregion Explicit conversions from BigRational

		#region Implicit conversions to BigRational

		[CLSCompliant(false)]
		public static implicit operator BigRational(sbyte value)
		{
			return new BigRational((BigInteger)value);
		}

		[CLSCompliant(false)]
		public static implicit operator BigRational(ushort value)
		{
			return new BigRational((BigInteger)value);
		}

		[CLSCompliant(false)]
		public static implicit operator BigRational(uint value)
		{
			return new BigRational((BigInteger)value);
		}

		[CLSCompliant(false)]
		public static implicit operator BigRational(ulong value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(byte value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(short value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(int value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(long value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(BigInteger value)
		{
			return new BigRational(value);
		}

		public static implicit operator BigRational(float value)
		{
			return new BigRational((double)value);
		}

		public static implicit operator BigRational(double value)
		{
			return new BigRational(value);
		}

		public static implicit operator BigRational(decimal value)
		{
			return new BigRational(value);
		}

		#endregion Implicit conversions to BigRational

		#region Operator Overloads
		public static bool operator ==(BigRational x, BigRational y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(BigRational x, BigRational y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(BigRational x, BigRational y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(BigRational x, BigRational y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(BigRational x, BigRational y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(BigRational x, BigRational y)
		{
			return Compare(x, y) >= 0;
		}

		public static BigRational operator +(BigRational r)
		{
			return r;
		}

		public static BigRational operator -(BigRational r)
		{
			return new BigRational(-r.Numerator, r.Denominator);
		}

		public static BigRational operator ++(BigRational r)
		{
			return r + One;
		}

		public static BigRational operator --(BigRational r)
		{
			return r - One;
		}

		public static BigRational operator +(BigRational r1, BigRational r2)
		{
			// a/b + c/d  == (ad + bc)/bd
			return new BigRational((r1.Numerator * r2.Denominator) + (r1.Denominator * r2.Numerator), r1.Denominator * r2.Denominator);
		}

		public static BigRational operator -(BigRational r1, BigRational r2)
		{
			// a/b - c/d  == (ad - bc)/bd
			return new BigRational((r1.Numerator * r2.Denominator) - (r1.Denominator * r2.Numerator), r1.Denominator * r2.Denominator);
		}

		public static BigRational operator *(BigRational r1, BigRational r2)
		{
			// a/b * c/d  == (ac)/(bd)
			return new BigRational(r1.Numerator * r2.Numerator, r1.Denominator * r2.Denominator);
		}

		public static BigRational operator /(BigRational r1, BigRational r2)
		{
			// a/b / c/d  == (ad)/(bc)
			return new BigRational(r1.Numerator * r2.Denominator, r1.Denominator * r2.Numerator);
		}

		public static BigRational operator %(BigRational r1, BigRational r2)
		{
			// a/b % c/d  == (ad % bc)/bd
			return new BigRational((r1.Numerator * r2.Denominator) % (r1.Denominator * r2.Numerator), r1.Denominator * r2.Denominator);
		}
		#endregion Operator Overloads

		#region Public Static Methods

		public static BigRational Abs(BigRational r)
		{
			return r.Numerator.Sign < 0 ? new BigRational(BigInteger.Abs(r.Numerator), r.Denominator) : r;
		}

		public static BigRational Negate(BigRational r)
		{
			return new BigRational(BigInteger.Negate(r.Numerator), r.Denominator);
		}

		public static BigRational Invert(BigRational r)
		{
			return new BigRational(r.Denominator, r.Numerator);
		}

		public static BigRational Add(BigRational x, BigRational y)
		{
			return x + y;
		}

		public static BigRational Subtract(BigRational x, BigRational y)
		{
			return x - y;
		}

		public static BigRational Multiply(BigRational x, BigRational y)
		{
			return x * y;
		}

		public static BigRational Divide(BigRational dividend, BigRational divisor)
		{
			return dividend / divisor;
		}

		public static BigRational Remainder(BigRational dividend, BigRational divisor)
		{
			return dividend % divisor;
		}

		public static BigRational DivRem(BigRational dividend, BigRational divisor, out BigRational remainder)
		{
			// a/b / c/d  == (ad)/(bc)
			// a/b % c/d  == (ad % bc)/bd

			// (ad) and (bc) need to be calculated for both the division and the remainder operations.
			BigInteger ad = dividend.Numerator * divisor.Denominator;
			BigInteger bc = dividend.Denominator * divisor.Numerator;
			BigInteger bd = dividend.Denominator * divisor.Denominator;

			remainder = new BigRational(ad % bc, bd);
			return new BigRational(ad, bc);
		}

		public static BigRational Pow(BigRational baseValue, BigInteger exponent)
		{
			if (exponent.Sign == 0)
			{
				// 0^0 -> 1
				// n^0 -> 1
				return One;
			}
			else if (exponent.Sign < 0)
			{
				if (baseValue == Zero)
				{
					throw new ArgumentException("cannot raise zero to a negative power", nameof(baseValue));
				}

				// n^(-e) -> (1/n)^e
				baseValue = Invert(baseValue);
				exponent = BigInteger.Negate(exponent);
			}

			BigRational result = baseValue;
			while (exponent > BigInteger.One)
			{
				result *= baseValue;
				exponent--;
			}

			return result;
		}

		// Least Common Denominator (LCD)
		//
		// The LCD is the least common multiple of the two denominators.  For instance, the LCD of
		// {1/2, 1/4} is 4 because the least common multiple of 2 and 4 is 4.  Likewise, the LCD
		// of {1/2, 1/3} is 6.
		//
		// To find the LCD:
		//
		// 1) Find the Greatest Common Divisor (GCD) of the denominators
		// 2) Multiply the denominators together
		// 3) Divide the product of the denominators by the GCD
		public static BigInteger LeastCommonDenominator(BigRational x, BigRational y)
		{
			// LCD( a/b, c/d ) == (bd) / gcd(b,d)
			return x.Denominator * y.Denominator / BigInteger.GreatestCommonDivisor(x.Denominator, y.Denominator);
		}

		public static int Compare(BigRational r1, BigRational r2)
		{
			// a/b = c/d, iff ad = bc
			return BigInteger.Compare(r1.Numerator * r2.Denominator, r2.Numerator * r1.Denominator);
		}
		#endregion Public Static Methods

		#region Public Instance Methods

		// GetWholePart() and GetFractionPart()
		//
		// BigRational == Whole, Fraction
		//  0/2        ==     0,  0/2
		//  1/2        ==     0,  1/2
		// -1/2        ==     0, -1/2
		//  1/1        ==     1,  0/1
		// -1/1        ==    -1,  0/1
		// -3/2        ==    -1, -1/2
		//  3/2        ==     1,  1/2
		public BigInteger GetWholePart()
		{
			return BigInteger.Divide(this.Numerator, this.Denominator);
		}

		public BigRational GetFractionPart()
		{
			return new BigRational(BigInteger.Remainder(this.Numerator, this.Denominator), this.Denominator);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (!(obj is BigRational))
			{
				return false;
			}

			return this.Equals((BigRational)obj);
		}

		public override int GetHashCode()
		{
			return (this.Numerator / this.Denominator).GetHashCode();
		}

		// IComparable
		int IComparable.CompareTo(object obj)
		{
			if (obj == null)
			{
				return 1;
			}

			if (!(obj is BigRational))
			{
				throw new ArgumentException("Argument must be of type BigRational", nameof(obj));
			}

			return Compare(this, (BigRational)obj);
		}

		// IComparable<BigRational>
		public int CompareTo(BigRational other)
		{
			return Compare(this, other);
		}

		// Object.ToString
		public override string ToString()
		{
			StringBuilder ret = new StringBuilder();
			ret.Append(this.Numerator.ToString("R", CultureInfo.InvariantCulture));
			ret.Append(Solidus);
			ret.Append(this.Denominator.ToString("R", CultureInfo.InvariantCulture));
			return ret.ToString();
		}

		// IEquatable<BigRational>
		// a/b = c/d, iff ad = bc
		public bool Equals(BigRational other)
		{
#pragma warning disable CC0013 // Use ternary operator
			if (this.Denominator == other.Denominator)
#pragma warning restore CC0013 // Use ternary operator
			{
				return this.Numerator == other.Numerator;
			}
			else
			{
				return (this.Numerator * other.Denominator) == (this.Denominator * other.Numerator);
			}
		}

		#endregion Public Instance Methods

		#region Serialization
		void IDeserializationCallback.OnDeserialization(object sender)
		{
			try
			{
				// verify that the deserialized number is well formed
				if (this.Denominator.Sign == 0 || this.Numerator.Sign == 0)
				{
					// n/0 -> 0/1
					// 0/m -> 0/1
					this.Numerator = BigInteger.Zero;
					this.Denominator = BigInteger.One;
				}
				else if (this.Denominator.Sign < 0)
				{
					this.Numerator = BigInteger.Negate(this.Numerator);
					this.Denominator = BigInteger.Negate(this.Denominator);
				}

				this.Simplify();
			}
			catch (ArgumentException e)
			{
				throw new SerializationException("invalid serialization data", e);
			}
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			info.AddValue(nameof(this.Numerator), this.Numerator);
			info.AddValue(nameof(this.Denominator), this.Denominator);
		}

		#endregion Serialization

		#region Private Static helper methods
		private static bool SafeCastToDouble(BigInteger value)
		{
			return value >= DoubleMinValue && value <= DoubleMaxValue;
		}

		private static bool SafeCastToDecimal(BigInteger value)
		{
			return value >= DecimalMinValue && value <= DecimalMaxValue;
		}

		private static void SplitDoubleIntoParts(double dbl, out int sign, out int exp, out ulong man, out bool isFinite)
		{
			DoubleUlong du;
			du.Uu = 0;
			du.Dbl = dbl;

			sign = 1 - ((int)(du.Uu >> 62) & 2);
			man = du.Uu & 0x000FFFFFFFFFFFFF;
			exp = (int)(du.Uu >> 52) & 0x7FF;
			if (exp == 0)
			{
				// Denormalized number.
				isFinite = true;
				if (man != 0)
				{
					exp = -1074;
				}
			}
			else if (exp == 0x7FF)
			{
				// NaN or Infinite.
				isFinite = false;
				exp = int.MaxValue;
			}
			else
			{
				isFinite = true;
				man |= 0x0010000000000000; // mask in the implied leading 53rd significand bit
				exp -= 1075;
			}
		}

		#endregion Private static helper methods

		#region Private Instance helper methods
		private void Simplify()
		{
			// * if the numerator is {0, +1, -1} then the fraction is already reduced
			// * if the denominator is {+1} then the fraction is already reduced
			if (this.Numerator == BigInteger.Zero)
			{
				this.Denominator = BigInteger.One;
			}

			BigInteger gcd = BigInteger.GreatestCommonDivisor(this.Numerator, this.Denominator);
			if (gcd > BigInteger.One)
			{
				this.Numerator /= gcd;
				this.Denominator /= gcd;
			}
		}
		#endregion Private instance helper methods

		#region Private Types

		[StructLayout(LayoutKind.Explicit)]
		internal struct DoubleUlong
		{
			[FieldOffset(0)]
			public double Dbl;
			[FieldOffset(0)]
			public ulong Uu;
		}

		[StructLayout(LayoutKind.Explicit)]
		internal struct DecimalUInt32
		{
			[FieldOffset(0)]
			public decimal Dec;
			[FieldOffset(0)]
			public int Flags;
		}

		#endregion
	} // BigRational
#pragma warning restore MEN007 // Use a single return
} // namespace Numerics