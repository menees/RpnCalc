namespace Menees.RpnCalc.Internal
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	#endregion

	internal static class TimeSpanParser
	{
		#region Public Methods

		public static bool TryParse(string text, DateTimeFormatInfo timeFmt, NumberFormatInfo numFmt, out TimeSpanValue value)
		{
			value = null;
			bool result = false;

			// Only use our custom parser if time formats are using ':' separators.
			// If another culture is using some other time format, then the caller
			// should try to let the system parse it.
			const char c_separator = ':';
			if (!string.IsNullOrEmpty(text) && timeFmt.ShortTimePattern.IndexOf(c_separator) >= 0)
			{
				string[] tokens = text.Trim().Split(new char[] { c_separator }, StringSplitOptions.RemoveEmptyEntries);
				int numTokens = tokens.Length;
				if (numTokens >= 2 && numTokens <= 4)
				{
					string decimalSeparator = numFmt.NumberDecimalSeparator;
					string[] dhmsTokens = GetDHMSTokens(tokens, decimalSeparator);
					if (dhmsTokens != null)
					{
						result = ParseDHMSTokens(dhmsTokens, decimalSeparator, numFmt.NegativeSign, out value);
					}
				}
			}

			return result;
		}

		#endregion

		#region Private Methods

		private static string[] GetDHMSTokens(string[] originalTokens, string decimalSeparator)
		{
			// This will contain the days, hours, minutes, and seconds tokens in order.
			string[] dhmsTokens = new string[4];

			// If the first token is in X.Y form, then assume D.H and require numTokens <= 3.
			string firstToken = originalTokens[0];
			int decimalSepIndex = firstToken.IndexOf(decimalSeparator, StringComparison.CurrentCulture);
			if (decimalSepIndex >= 0)
			{
				dhmsTokens[0] = firstToken.Substring(0, decimalSepIndex);
				dhmsTokens[1] = firstToken.Substring(decimalSepIndex + 1);

				switch (originalTokens.Length)
				{
					case 2: // D.H:M
						dhmsTokens[2] = originalTokens[1];
						break;
					case 3: // D.H:M:S
						dhmsTokens[2] = originalTokens[1];
						dhmsTokens[3] = originalTokens[2];
						break;
					default: // 4
							// They entered something invalid like D.H:M:S:X
						dhmsTokens = null;
						break;
				}
			}
			else
			{
				// Unlike TimeSpan.TryParse, I'll assume that two fields means
				// M:S instead of H:M because I enter a lot of M:S race times.
				switch (originalTokens.Length)
				{
					case 2: // M:S
						dhmsTokens[2] = originalTokens[0];
						dhmsTokens[3] = originalTokens[1];
						break;
					case 3: // H:M:S
						dhmsTokens[1] = originalTokens[0];
						dhmsTokens[2] = originalTokens[1];
						dhmsTokens[3] = originalTokens[2];
						break;
					case 4: // D:H:M:S.  This is an undocumented format that .NET handles.
						dhmsTokens = originalTokens;
						break;
				}
			}

			return dhmsTokens;
		}

		private static bool ParseDHMSTokens(string[] dhmsTokens, string decimalSeparator, string negativeSign, out TimeSpanValue value)
		{
			value = null;
			bool result = false;

			// We must have at least two non-empty, non-whitespace (NENWS) tokens (e.g., M:S).
			//  Note: D.H:M input will cause the S token to still be null when we get here, so we
			//  can't assume that everything after the first NENWS token will also be a NENWS token.
			// Only the first NENWS token can contain a negative sign.
			// Only the Seconds token can contain a decimal separator.
			var nenwsTokens = dhmsTokens.SkipWhile(x => string.IsNullOrWhiteSpace(x));
			if (nenwsTokens.Count() >= 2 &&
				ValidateTokens(nenwsTokens.Skip(1), x => x == null || !x.Contains(negativeSign)) &&
				ValidateTokens(dhmsTokens.Take(3), x => x == null || !x.Contains(decimalSeparator)))
			{
				if (GetTicks(dhmsTokens[0], TimeSpan.TicksPerDay, false, out long daysTicks) &&
					GetTicks(dhmsTokens[1], TimeSpan.TicksPerHour, false, out long hoursTicks) &&
					GetTicks(dhmsTokens[2], TimeSpan.TicksPerMinute, false, out long minutesTicks) &&
					GetTicks(dhmsTokens[3], TimeSpan.TicksPerSecond, true, out long secondsTicks))
				{
					// Negative on the first token means the whole TimeSpan should be negative.
					// We have to check if the token text starts with a negative sign because the
					// numeric value might be zero (e.g., "-00:19:01").
					long sign = nenwsTokens.First().StartsWith(negativeSign, StringComparison.CurrentCulture) ? -1 : 1;
					TimeSpan timeSpan = TimeSpan.FromTicks(sign * (daysTicks + hoursTicks + minutesTicks + secondsTicks));
					value = new TimeSpanValue(timeSpan);
					result = true;
				}
			}

			return result;
		}

		private static bool ValidateTokens(IEnumerable<string> tokens, Func<string, bool> validateToken)
		{
			bool valid = true;
			foreach (string token in tokens)
			{
				if (!validateToken(token))
				{
					valid = false;
					break;
				}
			}

			return valid;
		}

		private static bool GetTicks(string text, long multiplier, bool allowFractionalParts, out long ticks)
		{
			ticks = 0;
			bool result = false;

			if (string.IsNullOrEmpty(text))
			{
				// We have to treat empty fields like zeros.
				result = true;
			}
			else if (allowFractionalParts)
			{
				if (double.TryParse(text, out double value))
				{
					ticks = (long)(multiplier * Math.Abs(value));
					result = true;
				}
			}
			else
			{
				if (int.TryParse(text, out int value))
				{
					ticks = multiplier * Math.Abs(value);
					result = true;
				}
			}

			return result;
		}

		#endregion
	}
}
