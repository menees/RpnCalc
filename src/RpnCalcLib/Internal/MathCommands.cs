#region Using Directives

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

#endregion

namespace Menees.RpnCalc.Internal
{
    internal class MathCommands : Commands
    {
        #region Constructors

        public MathCommands(Calculator calc)
            : base(calc)
        {
        }

        #endregion

        #region Public Methods

        public void Abs(Command cmd)
        {
            RequireArgs(1);
            RequireComplexNumericTypeOr(0, ValueType.TimeSpan);
            var value = cmd.UseTopValue();
            Value result = Value.Abs(value, Calc);
            cmd.Commit(result);
        }

        public void ACos(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Acos,
                value => NormalizeTrigResult(Calc.ConvertFromRadiansToAngle(Math.Acos(value))));
        }

        public void Add(Command cmd)
        {
            RequireArgs(2);
            var values = cmd.UseTopValues(2);
            Value result = Value.Add(values[1], values[0], Calc);
            cmd.Commit(result);
        }

        public void ALog(Command cmd)
        {
            TrancendentalOp(cmd,
                power => Complex.Pow(new Complex(10, 0), power),
                power => Math.Pow(10, power));
        }

        public void ASin(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Asin,
                value => NormalizeTrigResult(Calc.ConvertFromRadiansToAngle(Math.Asin(value))));
        }

        public void ATan(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Atan,
                value => NormalizeTrigResult(Calc.ConvertFromRadiansToAngle(Math.Atan(value))));
        }

        public void Ceil(Command cmd)
        {
            NumericValue value = UseTopScalarNumericValue(cmd);
            NumericValue result = value;
            switch (value.ValueType)
            {
                case ValueType.Double:
                    result = new DoubleValue(Math.Ceiling(value.ToDouble()));
                    break;
                case ValueType.Fraction:
                    result = FractionValue.Ceiling((FractionValue)value);
                    break;
            }
            cmd.Commit(result);
        }

        public void Comb(Command cmd)
        {
            //Comb(n, r) = n!/(r!(n-r)!)
            CombPerm(cmd, getDenominator: (n, r) => IntegerValue.Factorial(r) * IntegerValue.Factorial(n - r));
        }

        public void Cos(Command cmd)
        {
            TrancendentalOp(cmd,
                Complex.Cos,
                value => NormalizeTrigResult(Math.Cos(Calc.ConvertFromAngleToRadians(value))));
        }

        public void CosH(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Cosh,
                value => NormalizeTrigResult(Math.Cosh(Calc.ConvertFromAngleToRadians(value))));
        }

        public void Divide(Command cmd)
        {
            RequireArgs(2);
            var values = cmd.UseTopValues(2);
            Value result = Value.Divide(values[1], values[0], Calc);
            cmd.Commit(result);
        }

        public void DtoR(Command cmd)
        {
            var value = UseTopScalarNumericValue(cmd);
            cmd.Commit(new DoubleValue(ConvertFromDegreesToRadians(value.ToDouble())));
        }

        public void Exp(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Exp, Math.Exp);
        }

        public void Fact(Command cmd)
        {
            NumericValue value = UseTopScalarNumericValue(cmd);
            BigInteger integerValue = RequireInteger(value);
            BigInteger result = IntegerValue.Factorial(integerValue);
            cmd.Commit(new IntegerValue(result));
        }

        public void Floor(Command cmd)
        {
            NumericValue value = UseTopScalarNumericValue(cmd);
            NumericValue result = value;
            switch (value.ValueType)
            {
                case ValueType.Double:
                    result = new DoubleValue(Math.Floor(((DoubleValue)value).AsDouble));
                    break;
                case ValueType.Fraction:
                    result = FractionValue.Floor((FractionValue)value);
                    break;
            }
            cmd.Commit(result);
        }

        public void Frac(Command cmd)
        {
            NumericValue value = UseTopScalarNumericValue(cmd);
            NumericValue result = value;
            switch (value.ValueType)
            {
                case ValueType.Double:
                    double originalValue = ((DoubleValue)value).AsDouble;
                    double fractionalValue = originalValue - Utility.Truncate(originalValue);
                    result = new DoubleValue(fractionalValue);
                    break;
                case ValueType.Fraction:
                    result = ((FractionValue)value).GetFractionalPart();
                    break;
                case ValueType.Integer:
                    result = new IntegerValue(BigInteger.Zero);
                    break;
                case ValueType.Binary:
                    result = new BinaryValue(0);
                    break;
            }
            cmd.Commit(result);
        }

        public void Gcd(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoScalarNumericValues(cmd, out x, out y);
            NumericValue result = NumericValue.Gcd(x, y);
            cmd.Commit(result);
        }

        public void Hyp(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            Value xSq = Value.Multiply(x, x, Calc);
            Value ySq = Value.Multiply(y, y, Calc);
            Value sum = Value.Add(xSq, ySq, Calc);
            NumericValue result = NumericValue.Sqrt((NumericValue)sum);
            cmd.Commit(result);
        }

        public void Int(Command cmd)
        {
            NumericValue value = UseTopScalarNumericValue(cmd);
            NumericValue result = value;
            switch (value.ValueType)
            {
                case ValueType.Double:
                    result = new DoubleValue(Utility.Truncate(((DoubleValue)value).AsDouble));
                    break;
                case ValueType.Fraction:
                    result = ((FractionValue)value).GetWholePart();
                    break;
            }
            cmd.Commit(result);
        }

        public void Invert(Command cmd)
        {
            var value = UseTopNumericValue(cmd);
            NumericValue result = NumericValue.Invert(value);
            cmd.Commit(result);
        }

        public void Lcm(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoScalarNumericValues(cmd, out x, out y);
            NumericValue result = NumericValue.Lcm(x, y, Calc);
            cmd.Commit(result);
        }

        public void Ln(Command cmd)
        {
            TrancendentalOp(cmd,
                new TrancendentalOperations(Complex.Log,
                Math.Log, BigInteger.Log));
        }

        public void Log(Command cmd)
        {
            TrancendentalOp(cmd,
                new TrancendentalOperations(Complex.Log10,
                    Math.Log10, BigInteger.Log10));
        }

        public void Max(Command cmd)
        {
            Compare2(cmd, 1);
        }

        public void Min(Command cmd)
        {
            Compare2(cmd, -1);
        }

        public void Mod(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            NumericValue result = NumericValue.Modulus(y, x, Calc);
            cmd.Commit(result);
        }

        public void Multiply(Command cmd)
        {
            RequireArgs(2);
            var values = cmd.UseTopValues(2);
            Value result = Value.Multiply(values[1], values[0], Calc);
            cmd.Commit(result);
        }

        public void Negate(Command cmd)
        {
            RequireArgs(1);
            var value = cmd.UseTopValue();
            Value result = Value.Negate(value, Calc);
            cmd.Commit(result);
        }

        public void Percent(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            //Return y% of x => xy/100
            Value result = Value.Divide(Value.Multiply(x, y, Calc), c_oneHundred, Calc);
            cmd.Commit(result);
        }

        public void PercentChange(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            //Return % change from y to x as a percentage of y => 100(x-y)/y
            Value result = Value.Divide(Value.Multiply(c_oneHundred, Value.Subtract(x, y, Calc), Calc), y, Calc);
            cmd.Commit(result);
        }

        public void PercentTotal(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            //Return % of the total y represented by x => 100x/y
            Value result = Value.Divide(Value.Multiply(c_oneHundred, x, Calc), y, Calc);
            cmd.Commit(result);
        }

        public void Perm(Command cmd)
        {
            //Perm(n, r) = n!/(n-r)!
            CombPerm(cmd, getDenominator: (n, r) => IntegerValue.Factorial(n - r));
        }

        public void Power(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            NumericValue result = NumericValue.Power(y, x);
            cmd.Commit(result);
        }

        public void RtoD(Command cmd)
        {
            var value = UseTopScalarNumericValue(cmd);
            cmd.Commit(new DoubleValue(ConvertFromRadiansToDegrees(value.ToDouble())));
        }

        public void Random(Command cmd)
        {
            double value = Randomizer.NextDouble();
            cmd.Commit(new DoubleValue(value));
        }

        public void RandomBetween(Command cmd)
        {
            NumericValue first, second;
            UseTopTwoScalarNumericValues(cmd, out first, out second);
            double min = second.ToDouble();
            double max = first.ToDouble();
            double range = max - min;
            double offset = range * Randomizer.NextDouble();
            double result = min + offset;
            cmd.Commit(new DoubleValue(result));
        }

        public void Round(Command cmd)
        {
            Round(cmd, Utility.Round);
        }

        public void SetRandomSeed(Command cmd)
        {
            var value = UseTopScalarNumericValue(cmd);
            var seed = (int)RequireInteger(value);

            //Specify 0 as the seed to use a time-dependent seed value.
            if (seed == 0)
            {
                Randomizer = new Random();
            }
            else
            {
                Randomizer = new Random(seed);
            }

            cmd.Commit();
        }

        public void Sign(Command cmd)
        {
            RequireArgs(1);
            RequireComplexNumericTypeOr(0, ValueType.TimeSpan);
            var value = cmd.UseTopValue();
            NumericValue result = Value.Sign(value, Calc);
            cmd.Commit(result);
        }

        public void Sin(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Sin,
                value => NormalizeTrigResult(Math.Sin(Calc.ConvertFromAngleToRadians(value))));
        }

        public void SinH(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Sinh,
                value => NormalizeTrigResult(Math.Sinh(Calc.ConvertFromAngleToRadians(value))));
        }

        public void Sqrt(Command cmd)
        {
            var value = UseTopNumericValue(cmd);
            NumericValue result = NumericValue.Sqrt(value);
            cmd.Commit(result);
        }

        public void Square(Command cmd)
        {
            var value = UseTopNumericValue(cmd);
            Value result = Value.Multiply(value, value, Calc);
            cmd.Commit(result);
        }

        public void Subtract(Command cmd)
        {
            RequireArgs(2);
            var values = cmd.UseTopValues(2);
            Value result = Value.Subtract(values[1], values[0], Calc);
            cmd.Commit(result);
        }

        public void Tan(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Tan,
                value => NormalizeTrigResult(Math.Tan(Calc.ConvertFromAngleToRadians(value))));
        }

        public void TanH(Command cmd)
        {
            TrancendentalOp(cmd, Complex.Tanh,
                value => NormalizeTrigResult(Math.Tanh(Calc.ConvertFromAngleToRadians(value))));
        }

        public void Trunc(Command cmd)
        {
            Round(cmd, Utility.Truncate);
        }

        public void XRoot(Command cmd)
        {
            NumericValue x, y;
            UseTopTwoNumericValues(cmd, out x, out y);
            NumericValue invX = NumericValue.Invert(x);
            NumericValue result = NumericValue.Power(y, invX);
            cmd.Commit(result);
        }

        #endregion

        #region Internal Methods

        internal static double ConvertFromRadiansToDegrees(double radians)
        {
            double result = radians * c_radianToDegreeMultiplier;
            return result;
        }

        internal static double ConvertFromDegreesToRadians(double angle)
        {
            double result = angle / c_radianToDegreeMultiplier;
            return result;
        }

        #endregion

        #region Private Properties

        private Random Randomizer
        {
            get
            {
                if (m_randomizer == null)
                {
                    m_randomizer = new Random();
                }
                return m_randomizer;
            }
            set
            {
                m_randomizer = value;
            }
        }

        #endregion

        #region Private Methods

        private NumericValue UseTopScalarNumericValue(Command cmd)
        {
            RequireArgs(1);
            RequireScalarNumericType(0);
            var value = (NumericValue)cmd.UseTopValue();
            return value;
        }

        private NumericValue UseTopNumericValue(Command cmd)
        {
            RequireArgs(1);
            RequireComplexNumericType(0);
            var value = (NumericValue)cmd.UseTopValue();
            return value;
        }

        private void UseTopTwoScalarNumericValues(Command cmd, out NumericValue first, out NumericValue second)
        {
            UseTopTwoNumericValues(cmd, out first, out second);
            //After we've verified that the top two values are numeric,
            //make sure they're scalar values as well.
            RequireScalarNumericType(0);
            RequireScalarNumericType(1);
        }

        private void UseTopTwoNumericValues(Command cmd, out NumericValue first, out NumericValue second)
        {
            RequireArgs(2);
            RequireComplexNumericType(0);
            RequireComplexNumericType(1);
            var values = cmd.UseTopValues(2);
            first = (NumericValue)values[0];
            second = (NumericValue)values[1];
        }

        private void Compare2(Command cmd, int signToMatch)
        {
            RequireArgs(2);
            RequireScalarNumericTypeOr(0, ValueType.DateTime, ValueType.TimeSpan);
            RequireScalarNumericTypeOr(1, ValueType.DateTime, ValueType.TimeSpan);

            var values = cmd.UseTopValues(2);
            Value x = values[0];
            Value y = values[1];
            int compareSign = Math.Sign(Value.Compare(x, y, Calc));
            Value result = compareSign == signToMatch ? x : y;
            cmd.Commit(result);
        }

        private static BigInteger RequireInteger(NumericValue value)
        {
            bool isInteger = true;
            switch (value.ValueType)
            {
                case ValueType.Double:
                    double doubleValue = ((DoubleValue)value).AsDouble;
                    isInteger = Utility.IsInteger(doubleValue);
                    break;
                case ValueType.Fraction:
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

        private void CombPerm(Command cmd, Func<BigInteger, BigInteger, BigInteger> getDenominator)
        {
            NumericValue first, second;
            UseTopTwoScalarNumericValues(cmd, out first, out second);
            BigInteger r = RequireInteger(first);
            BigInteger n = RequireInteger(second);
            if (n < r)
            {
                throw new ArgumentOutOfRangeException();
            }

            BigInteger nFact = IntegerValue.Factorial(n);
            BigInteger denominator = getDenominator(n, r);
            BigInteger result = nFact / denominator;
            cmd.Commit(new IntegerValue(result));
        }

        private void Round(Command cmd, Func<double, int, double> roundValue)
        {
            RequireArgs(2);
            RequireComplexNumericType(1);
            RequireScalarNumericType(0);
            NumericValue value, numPlaces;
            UseTopTwoNumericValues(cmd, out numPlaces, out value);
            int digits = (int)numPlaces.ToInteger();

            NumericValue result = value;
            switch (value.ValueType)
            {
                case ValueType.Complex:
                    //I'm doing just what the HP48 does.  It always adjusts the real
                    //and imaginary portions, not the displayed first and second
                    //portions if polar mode is selected.
                    Complex complex = ((ComplexValue)value).AsComplex;
                    result = new ComplexValue(roundValue(complex.Real, digits),
                        roundValue(complex.Imaginary, digits));
                    break;
                case ValueType.Double:
                case ValueType.Fraction:
                    result = new DoubleValue(roundValue(value.ToDouble(), digits));
                    break;
            }
            cmd.Commit(result);
        }

        private void TrancendentalOp(Command cmd, Func<Complex, Complex> complexOp,
            Func<double, double> doubleOp)
        {
            TrancendentalOp(cmd, new TrancendentalOperations(complexOp, doubleOp));
        }

        private void TrancendentalOp(Command cmd, TrancendentalOperations operations)
        {
            NumericValue input = UseTopNumericValue(cmd);
            NumericValue output = operations.Apply(input);
            cmd.Commit(output);
        }

        private static double NormalizeTrigResult(double value)
        {
            double result = value;

            const double c_nearOverflow = 1e15;

            //.NET's trig functions like to return really tiny values (e.g., Sin(Pi) = 1e-16 instead of 0)
            //or really large values (e.g., Tan(Pi/2) = 1e16 instead of #INF).  Those errors are due to
            //the limitations of the double data type, so I'm going to apply some fudge factors to give
            //more real-world results.  I'm making the assumption that users are actually entering normal
            //range angle values and not microscopically small angles from horizontal or vertical.
            if (Utility.IsReallyNearZero(value, 1e-14))
            {
                result = 0;
            }
            else if (value > c_nearOverflow)
            {
                result = Double.PositiveInfinity;
            }
            else if (value < -c_nearOverflow)
            {
                result = Double.NegativeInfinity;
            }

            return result;
        }

        #endregion

        #region Private Types

        #region TrancendentalOperations

        //Used to handle exponential, log, and trig functions
        //for Complex, Double, and BigInteger values.
        //http://en.wikipedia.org/wiki/Transcendental_function
        private sealed class TrancendentalOperations
        {
            #region Constructors

            public TrancendentalOperations(Func<Complex, Complex> complexOperation,
                Func<double, double> doubleOperation)
            {
                ComplexOperation = complexOperation;
                DoubleOperation = doubleOperation;
            }

            public TrancendentalOperations(Func<Complex, Complex> complexOperation,
                Func<double, double> doubleOperation, Func<BigInteger, double> integerOperation)
            {
                ComplexOperation = complexOperation;
                DoubleOperation = doubleOperation;
                IntegerOperation = integerOperation;
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

                if (IntegerOperation != null &&
                    (input.ValueType == ValueType.Integer || input.ValueType == ValueType.Binary))
                {
                    double opResult = IntegerOperation(input.ToInteger());
                    result = new DoubleValue(opResult);
                }
                else if (DoubleOperation != null && input.ValueType != ValueType.Complex)
                {
                    double opResult = DoubleOperation(input.ToDouble());
                    result = new DoubleValue(opResult);
                }
                else
                {
                    Complex opResult = ComplexOperation(input.ToComplex());
                    result = new ComplexValue(opResult);
                }

                return result;
            }

            #endregion
        }

        #endregion

        #endregion

        #region Private Data Members

        private Random m_randomizer;

        private const double c_radianToDegreeMultiplier = 180.0 / Math.PI;
        private static readonly IntegerValue c_oneHundred = new IntegerValue(100);

        #endregion
    }
}
