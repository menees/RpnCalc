#region Using Directives

using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Input;
using System.Numerics;

#endregion

namespace Menees.RpnCalc.Internal
{
	internal class Command
	{
		#region Constructors

		public Command(Calculator calc)
		{
			this.calc = calc;
		}

		#endregion

		#region Public Methods

		public Value UseTopValue()
		{
			var values = this.UseTopValues(1);
			return values[0];
		}

		public IList<Value> UseTopValues(int count)
		{
			IList<Value> result = this.calc.Stack.PeekRange(count);
			this.topValuesUsedCount = count;
			return result;
		}

		/// <summary>
		/// This method should only be used by Stack commands.
		/// </summary>
		public void SetLastArgs(IList<Value> lastArgs)
		{
			this.lastArgs = lastArgs;
			this.topValuesUsedCount = 0;
		}

		public void Commit(params Value[] valuesToPush)
		{
			this.PushResults(CommandState.Committed, valuesToPush);
		}

		public void Cancel()
		{
			Debug.Assert(this.state != CommandState.Committed, "Cancel shouldn't be called on a command that has already been committed.");
			this.state = CommandState.Cancelled;
			this.topValuesUsedCount = 0;
			this.lastArgs = null;
		}

		#endregion

		#region Internal Properties

		internal CommandState State
		{
			get
			{
				return this.state;
			}
		}

		#endregion

		#region Internal Methods

		internal void PushLastArgs()
		{
			if (this.lastArgs != null)
			{
				this.calc.Stack.PushRange(this.lastArgs.Reverse());

				// Don't null out m_lastArgs.  We'll keep the args
				// around in case the user hits LAST again.
			}
		}

		internal void PushResults(CommandState state, params Value[] valuesToPush)
		{
			// Make sure we have no double or complex values that
			// use #INF or #NaN.  Also, do value type reduction if it
			// will cause no "significant" data loss.
			var actualValuesToPush = ValidateAndReduce(valuesToPush);

			ValueStack stack = this.calc.Stack;

			if (this.topValuesUsedCount > 0)
			{
				this.lastArgs = stack.PopRange(this.topValuesUsedCount);
			}

			stack.PushRange(actualValuesToPush);

			this.state = state;
		}

		#endregion

		#region Private Methods

		private static IEnumerable<Value> ValidateAndReduce(Value[] valuesToPush)
		{
			int numValuesToPush = valuesToPush.Length;
			if (numValuesToPush == 0)
			{
				return valuesToPush;
			}

			// This method does value type reduction, which is something
			// the HP48 doesn't do.  It's usually a good thing:
			//  * Complex values with 0 imaginary part are reduced to doubles.
			//  * Doubles with no fractional part are reduced to integers.
			//  * Fractions with a denominator of 1 are reduced to integers.
			//
			// But sometimes the results can seem odd.  For example:
			//  * If I manually type in "(4,0)" it ends up as the integer 4.
			//  * If 4 and 0 are on the stack, then RtoC returns 4.
			//
			// I'm going to keep this logic in spite of the oddities though
			// because numeric value types will be implicitly converted up
			// (i.e., widened) whenever necessary in operations.
			List<Value> result = new List<Value>(numValuesToPush);
			foreach (Value value in valuesToPush)
			{
				switch (value.ValueType)
				{
					case ValueType.Complex:
						ComplexValue complexValue = (ComplexValue)value;
						Complex complex = complexValue.AsComplex;
						Validate(complex.Real);
						Validate(complex.Imaginary);

						// Reduce to a double or integer if the imaginary part is 0.
						result.Add(Reduce(complex, complexValue));
						break;
					case ValueType.Double:
						DoubleValue doubleValue = (DoubleValue)value;
						double dbl = doubleValue.AsDouble;
						Validate(dbl);

						// Reduce to an integer if there's no fractional part.
						result.Add(Reduce(dbl, doubleValue));
						break;
					case ValueType.Fraction:
						FractionValue fraction = (FractionValue)value;

						// Reduce to an integer if the denominator is 1.
						if (fraction.Denominator == BigInteger.One)
						{
							result.Add(new IntegerValue(fraction.Numerator));
						}
						else
						{
							result.Add(value);
						}

						break;
					default:
						result.Add(value);
						break;
				}
			}

			return result;
		}

		private static void Validate(double value)
		{
			if (double.IsInfinity(value) || double.IsNaN(value))
			{
				throw new NotFiniteNumberException();
			}
		}

		private static NumericValue Reduce(double rawValue, DoubleValue existingDoubleValueInstance)
		{
			NumericValue result;

			if (Utility.IsInteger(rawValue, out BigInteger integerValue))
			{
				result = new IntegerValue(integerValue);
			}
			else if (existingDoubleValueInstance != null)
			{
				result = existingDoubleValueInstance;
			}
			else
			{
				result = new DoubleValue(rawValue);
			}

			return result;
		}

		private static NumericValue Reduce(Complex rawValue, ComplexValue existingComplexValueInstance)
		{
			NumericValue result;

			// If the real and imaginary parts differ by 9+ orders of magnitude
			// (i.e., one is at least a billion times the other) and one of the parts
			// is "near zero", then I'm going to assume that the "near zero" part
			// is due to floating point rounding errors.  This is important for cases
			// like SQ(SQRT((0,8))), which comes out as (1.77635683940025E-15, 8)
			// instead of (0,8).  Also SQRT(-4) is not exactly (0,2) without this.
			const double c_nonTrivialThreshold = 1e-5;
			const double c_nearZeroThreshold = 1e-14;
			if (Math.Abs(rawValue.Real) > c_nonTrivialThreshold && Utility.IsReallyNearZero(rawValue.Imaginary, c_nearZeroThreshold))
			{
				rawValue = new Complex(rawValue.Real, 0);
				existingComplexValueInstance = null;
			}

			if (Utility.IsReallyNearZero(rawValue.Real, c_nearZeroThreshold) && Math.Abs(rawValue.Imaginary) > c_nonTrivialThreshold)
			{
				rawValue = new Complex(0, rawValue.Imaginary);
				existingComplexValueInstance = null;
			}

			// Now see if we can reduce the complex number to a double or an integer.
			if (rawValue.Imaginary == 0)
			{
				result = Reduce(rawValue.Real, null);
			}
			else if (existingComplexValueInstance != null)
			{
				result = existingComplexValueInstance;
			}
			else
			{
				result = new ComplexValue(rawValue);
			}

			return result;
		}

		#endregion

		#region Private Data Members

		private Calculator calc;
		private int topValuesUsedCount;
		private IList<Value> lastArgs;
		private CommandState state;

		#endregion
	}
}
