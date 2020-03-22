namespace Menees.RpnCalc.Internal
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Numerics;
	using System.Text;

	#endregion

	internal class MathCommands : Commands
	{
		#region Private Data Members

		private const double RadianToDegreeMultiplier = 180.0 / Math.PI;
		private static readonly IntegerValue OneHundred = new IntegerValue(100);

		private Random randomizer;

		#endregion

		#region Constructors

		public MathCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Private Properties

		private Random Randomizer
		{
			get
			{
				if (this.randomizer == null)
				{
					this.randomizer = new Random();
				}

				return this.randomizer;
			}

			set
			{
				this.randomizer = value;
			}
		}

		#endregion

		#region Public Methods

		public void Abs(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireComplexNumericTypeOr(0, RpnValueType.TimeSpan);
			var value = cmd.UseTopValue();
			Value result = Value.Abs(value, this.Calc);
			cmd.Commit(result);
		}

		public void ACos(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Acos, value => NormalizeTrigResult(this.Calc.ConvertFromRadiansToAngle(Math.Acos(value))));
		}

		public void Add(Command cmd)
		{
			this.RequireArgs(2);
			var values = cmd.UseTopValues(2);
			Value result = Value.Add(values[1], values[0], this.Calc);
			cmd.Commit(result);
		}

		public void ALog(Command cmd)
		{
			this.TrancendentalOp(
				cmd,
				power => Complex.Pow(new Complex(10, 0), power),
				power => Math.Pow(10, power));
		}

		public void ASin(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Asin, value => NormalizeTrigResult(this.Calc.ConvertFromRadiansToAngle(Math.Asin(value))));
		}

		public void ATan(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Atan, value => NormalizeTrigResult(this.Calc.ConvertFromRadiansToAngle(Math.Atan(value))));
		}

		public void Ceil(Command cmd)
		{
			NumericValue value = this.UseTopScalarNumericValue(cmd);
			NumericValue result = value;
			switch (value.ValueType)
			{
				case RpnValueType.Double:
					result = new DoubleValue(Math.Ceiling(value.ToDouble()));
					break;
				case RpnValueType.Fraction:
					result = FractionValue.Ceiling((FractionValue)value);
					break;
			}

			cmd.Commit(result);
		}

		public void Comb(Command cmd)
		{
			// Comb(n, r) = n!/(r!(n-r)!)
			this.CombPerm(cmd, getDenominator: (n, r) => IntegerValue.Factorial(r) * IntegerValue.Factorial(n - r));
		}

		public void Cos(Command cmd)
		{
			this.TrancendentalOp(
				cmd,
				Complex.Cos,
				value => NormalizeTrigResult(Math.Cos(this.Calc.ConvertFromAngleToRadians(value))));
		}

		public void CosH(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Cosh, value => NormalizeTrigResult(Math.Cosh(this.Calc.ConvertFromAngleToRadians(value))));
		}

		public void Divide(Command cmd)
		{
			this.RequireArgs(2);
			var values = cmd.UseTopValues(2);
			Value result = Value.Divide(values[1], values[0], this.Calc);
			cmd.Commit(result);
		}

		public void DtoR(Command cmd)
		{
			var value = this.UseTopScalarNumericValue(cmd);
			cmd.Commit(new DoubleValue(ConvertFromDegreesToRadians(value.ToDouble())));
		}

		public void Exp(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Exp, Math.Exp);
		}

		public void Fact(Command cmd)
		{
			NumericValue value = this.UseTopScalarNumericValue(cmd);
			BigInteger integerValue = RequireInteger(value);
			BigInteger result = IntegerValue.Factorial(integerValue);
			cmd.Commit(new IntegerValue(result));
		}

		public void Floor(Command cmd)
		{
			NumericValue value = this.UseTopScalarNumericValue(cmd);
			NumericValue result = value;
			switch (value.ValueType)
			{
				case RpnValueType.Double:
					result = new DoubleValue(Math.Floor(((DoubleValue)value).AsDouble));
					break;
				case RpnValueType.Fraction:
					result = FractionValue.Floor((FractionValue)value);
					break;
			}

			cmd.Commit(result);
		}

		public void Frac(Command cmd)
		{
			NumericValue value = this.UseTopScalarNumericValue(cmd);
			NumericValue result = value;
			switch (value.ValueType)
			{
				case RpnValueType.Double:
					double originalValue = ((DoubleValue)value).AsDouble;
					double fractionalValue = originalValue - Utility.Truncate(originalValue);
					result = new DoubleValue(fractionalValue);
					break;
				case RpnValueType.Fraction:
					result = ((FractionValue)value).GetFractionalPart();
					break;
				case RpnValueType.Integer:
					result = new IntegerValue(BigInteger.Zero);
					break;
				case RpnValueType.Binary:
					result = new BinaryValue(0);
					break;
			}

			cmd.Commit(result);
		}

		public void Gcd(Command cmd)
		{
			this.UseTopTwoScalarNumericValues(cmd, out NumericValue x, out NumericValue y);
			NumericValue result = NumericValue.Gcd(x, y);
			cmd.Commit(result);
		}

		public void Hyp(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);
			Value xSq = Value.Multiply(x, x, this.Calc);
			Value ySq = Value.Multiply(y, y, this.Calc);
			Value sum = Value.Add(xSq, ySq, this.Calc);
			NumericValue result = NumericValue.Sqrt((NumericValue)sum);
			cmd.Commit(result);
		}

		public void Int(Command cmd)
		{
			NumericValue value = this.UseTopScalarNumericValue(cmd);
			NumericValue result = value;
			switch (value.ValueType)
			{
				case RpnValueType.Double:
					result = new DoubleValue(Utility.Truncate(((DoubleValue)value).AsDouble));
					break;
				case RpnValueType.Fraction:
					result = ((FractionValue)value).GetWholePart();
					break;
			}

			cmd.Commit(result);
		}

		public void Invert(Command cmd)
		{
			var value = this.UseTopNumericValue(cmd);
			NumericValue result = NumericValue.Invert(value);
			cmd.Commit(result);
		}

		public void Lcm(Command cmd)
		{
			this.UseTopTwoScalarNumericValues(cmd, out NumericValue x, out NumericValue y);
			NumericValue result = NumericValue.Lcm(x, y, this.Calc);
			cmd.Commit(result);
		}

		public void Ln(Command cmd)
		{
			this.TrancendentalOp(cmd, new TrancendentalOperations(Complex.Log, Math.Log, BigInteger.Log));
		}

		public void Log(Command cmd)
		{
			this.TrancendentalOp(cmd, new TrancendentalOperations(Complex.Log10, Math.Log10, BigInteger.Log10));
		}

		public void Max(Command cmd)
		{
			this.Compare2(cmd, 1);
		}

		public void Min(Command cmd)
		{
			this.Compare2(cmd, -1);
		}

		public void Mod(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);
			NumericValue result = NumericValue.Modulus(y, x, this.Calc);
			cmd.Commit(result);
		}

		public void Multiply(Command cmd)
		{
			this.RequireArgs(2);
			var values = cmd.UseTopValues(2);
			Value result = Value.Multiply(values[1], values[0], this.Calc);
			cmd.Commit(result);
		}

		public void Negate(Command cmd)
		{
			this.RequireArgs(1);
			var value = cmd.UseTopValue();
			Value result = Value.Negate(value, this.Calc);
			cmd.Commit(result);
		}

		public void Percent(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);

			// Return y% of x => xy/100
			Value result = Value.Divide(Value.Multiply(x, y, this.Calc), OneHundred, this.Calc);
			cmd.Commit(result);
		}

		public void PercentChange(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);

			// Return % change from y to x as a percentage of y => 100(x-y)/y
			Value result = Value.Divide(Value.Multiply(OneHundred, Value.Subtract(x, y, this.Calc), this.Calc), y, this.Calc);
			cmd.Commit(result);
		}

		public void PercentTotal(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);

			// Return % of the total y represented by x => 100x/y
			Value result = Value.Divide(Value.Multiply(OneHundred, x, this.Calc), y, this.Calc);
			cmd.Commit(result);
		}

		public void Perm(Command cmd)
		{
			// Perm(n, r) = n!/(n-r)!
			this.CombPerm(cmd, getDenominator: (n, r) => IntegerValue.Factorial(n - r));
		}

		public void Power(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);
			NumericValue result = NumericValue.Power(y, x);
			cmd.Commit(result);
		}

		public void RtoD(Command cmd)
		{
			var value = this.UseTopScalarNumericValue(cmd);
			cmd.Commit(new DoubleValue(ConvertFromRadiansToDegrees(value.ToDouble())));
		}

		public void Random(Command cmd)
		{
			double value = this.Randomizer.NextDouble();
			cmd.Commit(new DoubleValue(value));
		}

		public void RandomBetween(Command cmd)
		{
			this.UseTopTwoScalarNumericValues(cmd, out NumericValue first, out NumericValue second);
			double min = second.ToDouble();
			double max = first.ToDouble();
			double range = max - min;
			double offset = range * this.Randomizer.NextDouble();
			double result = min + offset;
			cmd.Commit(new DoubleValue(result));
		}

		public void Round(Command cmd)
		{
			this.Round(cmd, Utility.Round);
		}

		public void SetRandomSeed(Command cmd)
		{
			var value = this.UseTopScalarNumericValue(cmd);
			var seed = (int)RequireInteger(value);

			// Specify 0 as the seed to use a time-dependent seed value.
			if (seed == 0)
			{
				this.Randomizer = new Random();
			}
			else
			{
				this.Randomizer = new Random(seed);
			}

			cmd.Commit();
		}

		public void Sign(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireComplexNumericTypeOr(0, RpnValueType.TimeSpan);
			var value = cmd.UseTopValue();
			NumericValue result = Value.Sign(value, this.Calc);
			cmd.Commit(result);
		}

		public void Sin(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Sin, value => NormalizeTrigResult(Math.Sin(this.Calc.ConvertFromAngleToRadians(value))));
		}

		public void SinH(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Sinh, value => NormalizeTrigResult(Math.Sinh(this.Calc.ConvertFromAngleToRadians(value))));
		}

		public void Sqrt(Command cmd)
		{
			var value = this.UseTopNumericValue(cmd);
			NumericValue result = NumericValue.Sqrt(value);
			cmd.Commit(result);
		}

		public void Square(Command cmd)
		{
			var value = this.UseTopNumericValue(cmd);
			Value result = Value.Multiply(value, value, this.Calc);
			cmd.Commit(result);
		}

		public void Subtract(Command cmd)
		{
			this.RequireArgs(2);
			var values = cmd.UseTopValues(2);
			Value result = Value.Subtract(values[1], values[0], this.Calc);
			cmd.Commit(result);
		}

		public void Tan(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Tan, value => NormalizeTrigResult(Math.Tan(this.Calc.ConvertFromAngleToRadians(value))));
		}

		public void TanH(Command cmd)
		{
			this.TrancendentalOp(cmd, Complex.Tanh, value => NormalizeTrigResult(Math.Tanh(this.Calc.ConvertFromAngleToRadians(value))));
		}

		public void Trunc(Command cmd)
		{
			this.Round(cmd, Utility.Truncate);
		}

		public void XRoot(Command cmd)
		{
			this.UseTopTwoNumericValues(cmd, out NumericValue x, out NumericValue y);
			NumericValue invX = NumericValue.Invert(x);
			NumericValue result = NumericValue.Power(y, invX);
			cmd.Commit(result);
		}

		#endregion

		#region Internal Methods

		internal static double ConvertFromRadiansToDegrees(double radians)
		{
			double result = radians * RadianToDegreeMultiplier;
			return result;
		}

		internal static double ConvertFromDegreesToRadians(double angle)
		{
			double result = angle / RadianToDegreeMultiplier;
			return result;
		}

		#endregion

		#region Private Methods

		private static BigInteger RequireInteger(NumericValue value)
		{
			bool isInteger = true;
			switch (value.ValueType)
			{
				case RpnValueType.Double:
					double doubleValue = ((DoubleValue)value).AsDouble;
					isInteger = Utility.IsInteger(doubleValue);
					break;
				case RpnValueType.Fraction:
					var fraction = (FractionValue)value;
					isInteger = fraction.Denominator == 1;
					break;
			}

			if (!isInteger)
			{
				throw new ArgumentException(Resources.MathCommands_IntegerIsRequired);
			}

			BigInteger result = value.ToInteger();
			return result;
		}

		private static double NormalizeTrigResult(double value)
		{
			double result = value;

			const double c_nearOverflow = 1e15;

			// .NET's trig functions like to return really tiny values (e.g., Sin(Pi) = 1e-16 instead of 0)
			// or really large values (e.g., Tan(Pi/2) = 1e16 instead of #INF).  Those errors are due to
			// the limitations of the double data type, so I'm going to apply some fudge factors to give
			// more real-world results.  I'm making the assumption that users are actually entering normal
			// range angle values and not microscopically small angles from horizontal or vertical.
			if (Utility.IsReallyNearZero(value, 1e-14))
			{
				result = 0;
			}
			else if (value > c_nearOverflow)
			{
				result = double.PositiveInfinity;
			}
			else if (value < -c_nearOverflow)
			{
				result = double.NegativeInfinity;
			}

			return result;
		}

		private void CombPerm(Command cmd, Func<BigInteger, BigInteger, BigInteger> getDenominator)
		{
			this.UseTopTwoScalarNumericValues(cmd, out NumericValue first, out NumericValue second);
			BigInteger r = RequireInteger(first);
			BigInteger n = RequireInteger(second);
			if (n < r)
			{
				throw new ArgumentOutOfRangeException("r must be <= n", (Exception)null);
			}

			BigInteger nFact = IntegerValue.Factorial(n);
			BigInteger denominator = getDenominator(n, r);
			BigInteger result = nFact / denominator;
			cmd.Commit(new IntegerValue(result));
		}

		private NumericValue UseTopScalarNumericValue(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireScalarNumericType(0);
			var value = (NumericValue)cmd.UseTopValue();
			return value;
		}

		private NumericValue UseTopNumericValue(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireComplexNumericType(0);
			var value = (NumericValue)cmd.UseTopValue();
			return value;
		}

		private void UseTopTwoScalarNumericValues(Command cmd, out NumericValue first, out NumericValue second)
		{
			this.UseTopTwoNumericValues(cmd, out first, out second);

			// After we've verified that the top two values are numeric,
			// make sure they're scalar values as well.
			this.RequireScalarNumericType(0);
			this.RequireScalarNumericType(1);
		}

		private void UseTopTwoNumericValues(Command cmd, out NumericValue first, out NumericValue second)
		{
			this.RequireArgs(2);
			this.RequireComplexNumericType(0);
			this.RequireComplexNumericType(1);
			var values = cmd.UseTopValues(2);
			first = (NumericValue)values[0];
			second = (NumericValue)values[1];
		}

		private void Compare2(Command cmd, int signToMatch)
		{
			this.RequireArgs(2);
			this.RequireScalarNumericTypeOr(0, RpnValueType.DateTime, RpnValueType.TimeSpan);
			this.RequireScalarNumericTypeOr(1, RpnValueType.DateTime, RpnValueType.TimeSpan);

			var values = cmd.UseTopValues(2);
			Value x = values[0];
			Value y = values[1];
			int compareSign = Math.Sign(Value.Compare(x, y, this.Calc));
			Value result = compareSign == signToMatch ? x : y;
			cmd.Commit(result);
		}

		private void Round(Command cmd, Func<double, int, double> roundValue)
		{
			this.RequireArgs(2);
			this.RequireComplexNumericType(1);
			this.RequireScalarNumericType(0);
			this.UseTopTwoNumericValues(cmd, out NumericValue numPlaces, out NumericValue value);
			int digits = (int)numPlaces.ToInteger();

			NumericValue result = value;
			switch (value.ValueType)
			{
				case RpnValueType.Complex:
					// I'm doing just what the HP48 does.  It always adjusts the real
					// and imaginary portions, not the displayed first and second
					// portions if polar mode is selected.
					Complex complex = ((ComplexValue)value).AsComplex;
					result = new ComplexValue(
						roundValue(complex.Real, digits),
						roundValue(complex.Imaginary, digits));
					break;
				case RpnValueType.Double:
				case RpnValueType.Fraction:
					result = new DoubleValue(roundValue(value.ToDouble(), digits));
					break;
			}

			cmd.Commit(result);
		}

		private void TrancendentalOp(Command cmd, Func<Complex, Complex> complexOp, Func<double, double> doubleOp)
		{
			this.TrancendentalOp(cmd, new TrancendentalOperations(complexOp, doubleOp));
		}

		private void TrancendentalOp(Command cmd, TrancendentalOperations operations)
		{
			NumericValue input = this.UseTopNumericValue(cmd);
			NumericValue output = operations.Apply(input);
			cmd.Commit(output);
		}

		#endregion

		#region Private Types

		#region TrancendentalOperations

		// Used to handle exponential, log, and trig functions
		// for Complex, Double, and BigInteger values.
		// http://en.wikipedia.org/wiki/Transcendental_function
		private sealed class TrancendentalOperations
		{
			#region Constructors

			public TrancendentalOperations(
				Func<Complex, Complex> complexOperation,
				Func<double, double> doubleOperation)
			{
				this.ComplexOperation = complexOperation;
				this.DoubleOperation = doubleOperation;
			}

			public TrancendentalOperations(
				Func<Complex, Complex> complexOperation,
				Func<double, double> doubleOperation,
				Func<BigInteger, double> integerOperation)
			{
				this.ComplexOperation = complexOperation;
				this.DoubleOperation = doubleOperation;
				this.IntegerOperation = integerOperation;
			}

			#endregion

			#region Public Properties

			public Func<Complex, Complex> ComplexOperation { get; private set; }

			public Func<double, double> DoubleOperation { get; private set; }

			public Func<BigInteger, double> IntegerOperation { get; private set; }

			#endregion

			#region Public Methods

			public NumericValue Apply(NumericValue input)
			{
				NumericValue result;

				if (this.IntegerOperation != null &&
					(input.ValueType == RpnValueType.Integer || input.ValueType == RpnValueType.Binary))
				{
					double opResult = this.IntegerOperation(input.ToInteger());
					result = new DoubleValue(opResult);
				}
				else if (this.DoubleOperation != null && input.ValueType != RpnValueType.Complex)
				{
					double opResult = this.DoubleOperation(input.ToDouble());
					result = new DoubleValue(opResult);
				}
				else
				{
					Complex opResult = this.ComplexOperation(input.ToComplex());
					result = new ComplexValue(opResult);
				}

				return result;
			}

			#endregion
		}

		#endregion

		#endregion
	}
}
