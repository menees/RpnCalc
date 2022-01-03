namespace Menees.RpnCalc.Internal
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO.IsolatedStorage;
	using System.Numerics;
	using System.Text;
	using System.Windows;
	using System.Xml.Linq;

	#endregion

	internal static class Utility
	{
		#region Private Data Members

		private const int DefaultPrecision = 12;

		#endregion

		#region Xml Public Methods

		public static int GetAttributeValue(this XElement element, XName attributeName, int defaultValue)
		{
			int result = element.GetAttributeValue(attributeName, defaultValue, x => int.Parse(x, CultureInfo.CurrentCulture));
			return result;
		}

		public static string GetAttributeValue(this XElement element, XName attributeName, string defaultValue)
		{
			string result = element.GetAttributeValue(attributeName, defaultValue, x => x);
			return result;
		}

		public static T GetAttributeValue<T>(this XElement element, XName attributeName, T defaultValue)
			where T : struct
		{
			T result = element.GetAttributeValue(attributeName, defaultValue, x => (T)Enum.Parse(typeof(T), x, false));
			return result;
		}

		public static T GetAttributeValue<T>(
			this XElement element,
			XName attributeName,
			T defaultValue,
			Func<string, T> converter)
		{
			T result = defaultValue;

			XAttribute? attr = element.Attribute(attributeName);
			if (attr != null)
			{
				string value = attr.Value;
				result = converter(value);
			}

			return result;
		}

		#endregion

		#region Parsing Public Methods

		public static bool TryParse(string text, out BigInteger value)
		{
			bool result = TryParse(text, NumberStyles.Integer | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out value);
			return result;
		}

		public static bool TryParse(string text, NumberStyles style, IFormatProvider provider, out BigInteger value)
		{
			bool parsedSuccessfully = false;
			value = BigInteger.Zero;

			// Use long's TryParse routine first because it will handle most common
			// input values more quickly and thoroughly than my code below will.
			if (long.TryParse(text, style, provider, out long longValue))
			{
				value = longValue;
				parsedSuccessfully = true;
			}
			else if (!string.IsNullOrWhiteSpace(text))
			{
				// The input wasn't parsible with a long, which has a range of
				// -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807.
				// So either the input is a big number, or it's poorly formed.
				// I'll try to parse it as simply as possible, but I'm not going
				// to try to handle all of the supported NumberStyles and formats.
				// Note: According to Int64.TryParse, .NET looks for patterns
				// in the following order, so I'll stick to that too roughly:
				// [ws][$][sign][digits,]digits[.fractional_digits][e[sign]exponential_digits][ws]
				NumberFormatInfo numFmt = NumberFormatInfo.GetInstance(provider);
				if (style.HasFlag(NumberStyles.AllowLeadingWhite))
				{
					text = text.TrimStart();
				}

				if (style.HasFlag(NumberStyles.AllowTrailingWhite))
				{
					text = text.TrimEnd();
				}

				if (style.HasFlag(NumberStyles.AllowCurrencySymbol) && text.StartsWith(numFmt.CurrencySymbol, StringComparison.CurrentCulture))
				{
					text = text.Substring(numFmt.CurrencySymbol.Length);
				}

				bool negativeValue = false;
				if (style.HasFlag(NumberStyles.AllowLeadingSign) && text.StartsWith(numFmt.NegativeSign, StringComparison.CurrentCulture))
				{
					negativeValue = true;
					text = text.Substring(numFmt.NegativeSign.Length);
				}
				else if (style.HasFlag(NumberStyles.AllowTrailingSign) && text.EndsWith(numFmt.NegativeSign, StringComparison.CurrentCulture))
				{
					negativeValue = true;
					text = text.Substring(0, text.Length - numFmt.NegativeSign.Length);
				}

				// Note: I'm ignoring NumberStyles.AllowParentheses.
				if (style.HasFlag(NumberStyles.AllowThousands))
				{
					// This is weak.  I'm not checking group sizes at all.
					text = text.Replace(numFmt.PercentGroupSeparator, string.Empty);
				}

				if (style.HasFlag(NumberStyles.AllowDecimalPoint))
				{
					// If it contains a decimal point, then it must have only zeros after that.
					// Otherwise, we'll just treat the whole thing as an empty string.
					text = HandleBigIntegerWithDecimalPoint(text, numFmt);
				}

				// By now, we're hopefully down to just decimal or hex digits.
				if (style.HasFlag(NumberStyles.AllowHexSpecifier))
				{
					parsedSuccessfully = TryParseDigits(text, 16, out value);
				}
				else
				{
					parsedSuccessfully = TryParseDigits(text, 10, out value);
				}

				if (parsedSuccessfully && negativeValue)
				{
					value = BigInteger.Negate(value);
				}
			}

			return parsedSuccessfully;
		}

		public static bool TryParseDigits(string text, int digitBase, out BigInteger value)
		{
			Debug.Assert(digitBase >= 2 && digitBase <= 16, "digitBase must be between 2 and 16.");

			value = BigInteger.Zero;
			int numDigits = text.Length;
			int successfullyParsedDigits = 0;

			BigInteger cumulativeValue = BigInteger.Zero;
			for (int i = 0; i < numDigits; i++)
			{
				// We have another digit, so shift everything left one more place.
				cumulativeValue *= digitBase;

				char ch = text[i];
				int digitValue = -1;
				switch (ch)
				{
					case '0': digitValue = 0; break;
					case '1': digitValue = 1; break;
					case '2': digitValue = 2; break;
					case '3': digitValue = 3; break;
					case '4': digitValue = 4; break;
					case '5': digitValue = 5; break;
					case '6': digitValue = 6; break;
					case '7': digitValue = 7; break;
					case '8': digitValue = 8; break;
					case '9': digitValue = 9; break;
					case 'a':
					case 'A': digitValue = 10; break;
					case 'b':
					case 'B': digitValue = 11; break;
					case 'c':
					case 'C': digitValue = 12; break;
					case 'd':
					case 'D': digitValue = 13; break;
					case 'e':
					case 'E': digitValue = 14; break;
					case 'f':
					case 'F': digitValue = 15; break;
				}

				if (digitValue >= 0 && digitValue < digitBase)
				{
					cumulativeValue += digitValue;
					successfullyParsedDigits++;
				}
				else
				{
					break;
				}
			}

			bool parsed = successfullyParsedDigits == numDigits;
			if (parsed)
			{
				value = cumulativeValue;
			}

			return parsed;
		}

		public static string StripDelimiters(string text, char start, char end)
		{
			string result = text;

			if (!string.IsNullOrEmpty(text))
			{
				int startIndex = 0;
				int length = text.Length;

				// Remove either of the delimiters or both.  The HP48 allows
				// "(1,2" (a complex without the closing parenthesis) to be
				// entered and parsed correctly if it's the only thing on the
				// entry line, so I want to support that behavior too.
				//
				// EntryLineParser looks for the start delimiter, and then it
				// goes to the end delimiter or the end of the entry line.
				// It determines the ValueType and which TryParse method
				// to invoke just by the start delimiter.  So by stripping
				// partially applied delimiters here (e.g., start without end)
				// we get HP48's entry parsing behavior.
				if (text[length - 1] == end)
				{
					length--;
				}

				if (text[0] == start)
				{
					startIndex++;
					length--;
				}

				result = text.Substring(startIndex, length);
			}

			return result;
		}

		#endregion

		#region Math Public Methods

		public static double Round(double value, int places)
		{
			// This uses a very simplistic rounding scheme, unlike Math.Round(double, int).
			double dPow10 = Math.Pow(10, places);
			value *= dPow10;
			value += value > 0 ? 0.5 : -0.5;
			value = value < 0 ? Math.Ceiling(value) : Math.Floor(value);
			return value / dPow10;
		}

		public static double Truncate(double value)
		{
			// http://en.wikipedia.org/wiki/Floor_and_ceiling_functions#Truncation
			double result = Math.Sign(value) * Math.Floor(Math.Abs(value));
			return result;
		}

		public static double Truncate(double value, int places)
		{
			double powerOf10 = Math.Pow(10, places);
			double result = Truncate(value * powerOf10) / powerOf10;
			return result;
		}

		public static bool IsInteger(double doubleValue)
		{
			return IsInteger(doubleValue, out _);
		}

		public static bool IsInteger(double doubleValue, out BigInteger integerValue)
		{
			bool result;

			double truncatedValue = Truncate(doubleValue);
			if (truncatedValue == 0)
			{
				// If the integral part is 0, then doubleValue is
				// only an integer if it's exactly equal to zero.
				result = doubleValue == 0;
			}
			else
			{
				// If the double has (almost) no fractional part AND it would fit in a "normal" integer,
				// then convert it to an integer.  Otherwise, leave it a double.  We need the value
				// size check because otherwise the MaxDouble command would return a 309 digit
				// number if 1.7 × 10^308 got value type reduced to a BigInteger.  Actually, any
				// double using the full 15-17 digits for the integral part (e.g., 1e16 or 1e100) would
				// get converted to an integer.  So we'll cap it at long's size, which is 19 digits.
				//
				// The check for IsReallyNearZero is necessary because SQ(ABS((1,3))) displays as
				// "10", but internally it's actually 10.000000000000002.  So we need the fudge factor
				// to handle non-displayed tiny fractional pieces.
				result = IsReallyNearZero(doubleValue - truncatedValue, 5e-15) &&
							Math.Abs(doubleValue) <= long.MaxValue;
			}

			if (result)
			{
				integerValue = (BigInteger)doubleValue;
			}
			else
			{
				integerValue = BigInteger.Zero;
			}

			return result;
		}

		public static double Gcd(double x, double y)
		{
			double result;

			if ((x == 0) && (y == 0))
			{
				result = 1;
			}
			else
			{
				// An epsilon of 1e-10 is necessary because the loop gets fairly
				// inaccurate for non-integers... (e.g. gcd(1821.204, 99))
				double remainder;
				int iteration = 0;
				while (!IsReallyNearZero(y, 1e-10))
				{
					remainder = x % y;
					x = y;
					y = remainder;

					CheckGcdLoopIterations(iteration++);
				}

				result = Round(x, DefaultPrecision);
			}

			return result;
		}

		public static void CheckGcdLoopIterations(int iteration)
		{
			// The GCD loop should converge.  But if it doesn't for some
			// reason, I want to throw an exception once we get
			// to an unreasonable number of iterations.  That's better
			// than being stuck in an infinite, non-converging loop.
			// Most values take less than 10 iterations.
			if (iteration >= 100000)
			{
				throw new OverflowException();
			}
		}

		public static bool IsReallyNearZero(double value, double epsilon)
		{
			return Math.Abs(value) < epsilon;
		}

		public static FractionValue DoubleToFraction(double value)
		{
			// I used to always do "new FractionValue((decimal)value)" because
			// it does a good job for fixed length fractions.  But it made no attempt
			// to deal with repeating fractions, which are very common, so I've
			// pulled in the algorithm I used in my old RPN Calc 2.  Now I try both
			// algorithms and then see which appears to be better.
			FractionValue local = DoubleToRationalLocal(value);
			double localDouble = local.ToDouble();

			// Use FractionValue's algorithm.
			FractionValue fraction = new((decimal)value);
			double fractionDouble = fraction.ToDouble();

			// See how far both are off from the original value.
			double localEpsilon = Math.Abs(localDouble - value);
			double fractionEpsilon = Math.Abs(fractionDouble - value);

			// Use the number of digits too.  If I divide 5.0/7.0 and then
			// convert back to a fraction, then the local algorithm nails it.
			// But if I enter a value of "0.714285714285714", then local's
			// epsilon is a tad farther off, even though it comes up with a
			// much smaller and better denominator.
			int localDenomDigits = NumDigits(local.Denominator);
			int fracDenomDigits = NumDigits(fraction.Denominator);
			int diffDigits = Math.Abs(localDenomDigits - fracDenomDigits);

			FractionValue result;
			if (diffDigits <= 2)
			{
				// The denominator sizes are close, so choose the result based on which
				// epsilon is smaller.  This is important for "0.13333", which produces fewer
				// denominator digits with the local algorithm but a much larger epsilon.
				result = localEpsilon < fractionEpsilon ? local : fraction;
			}
			else
			{
				// The denominator sizes are several orders of magnitude different, so
				// choose the one with the fewest digits.  The FractionValue algorithm
				// can always get a small epsilon by using a huge denominator, but that
				// rarely produces a fraction the user actually wants.
				result = localDenomDigits < fracDenomDigits ? local : fraction;
			}

			return result;
		}

		#endregion

		#region Private Methods

		private static string HandleBigIntegerWithDecimalPoint(string text, NumberFormatInfo numFmt)
		{
			int decimalPointPos = text.IndexOf(numFmt.NumberDecimalSeparator, StringComparison.CurrentCulture);
			if (decimalPointPos >= 0)
			{
				if (decimalPointPos < (text.Length - 1))
				{
					// Text is something like "123.4" or "123.000", so see if everything
					// after the decimal point parses to zero.
					string textAfterDecimal = text.Substring(decimalPointPos + 1);
					if (TryParse(textAfterDecimal, NumberStyles.None, numFmt, out BigInteger valueAfterDecimal)
						&& valueAfterDecimal == BigInteger.Zero)
					{
						// Just keep the text before the "." and any trailing zeros.
						text = text.Substring(0, decimalPointPos - 1);
					}
					else
					{
						// The text after the decimal wasn't parsable or it was non-zero,
						// so give up on the whole thing.
						text = string.Empty;
					}
				}
				else if (decimalPointPos == 0)
				{
					// The text must just be ".", which is crapola.
					text = string.Empty;
				}
				else
				{
					// The text is something like "123.", so drop the "."
					text = text.Substring(0, decimalPointPos - 1);
				}
			}

			return text;
		}

		private static FractionValue DoubleToRationalLocal(double value)
		{
			// Example: x = 0.5; 10x = 5; --> 9x = 4.5 --> x = 4.5/9 --> x = 1/2
			//
			// Ideally, we would scale by the length of the repeating pattern.
			// For example, 1/11 = 0.090909... we should scale by 100.
			// Try scaling by 2 to handle the 1 and 2 cases.  Anything higher
			// might miss shorter repeating patterns. (e.g. 3 misses 1/11)
			// Anything shorter would only look for single digit patterns,
			// which would be found when scaling by 2 anyway.
			const int c_scaleFactor = 100;
			const int c_scaleFactorMinus1 = c_scaleFactor - 1;

			double nineX = Round((c_scaleFactor * value) - value, DefaultPrecision);
			double gcd = Gcd(nineX, c_scaleFactorMinus1);

			double doubleNumerator = Round(nineX / gcd, DefaultPrecision);
			double doubleDenominator = Round(c_scaleFactorMinus1 / gcd, DefaultPrecision);

			return new FractionValue((BigInteger)doubleNumerator, (BigInteger)doubleDenominator);
		}

		private static int NumDigits(BigInteger value)
		{
			int result = 1;
			if (value != BigInteger.Zero)
			{
				double log = BigInteger.Log10(BigInteger.Abs(value));
				result = (int)Truncate(log) + 1;
			}

			return result;
		}

		#endregion
	}
}
