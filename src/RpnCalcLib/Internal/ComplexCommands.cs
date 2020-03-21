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
	internal class ComplexCommands : Commands
	{
		#region Constructors

		public ComplexCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Public Methods

		public void Conj(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireComplexNumericType(0);

			var value = cmd.UseTopValue();
			if (value.ValueType == ValueType.Complex)
			{
				var complexValue = (ComplexValue)value;
				cmd.Commit(new ComplexValue(Complex.Conjugate(complexValue.AsComplex)));
			}
			else
			{
				// Conjugating a real scalar just returns the same value.
				cmd.Commit(value);
			}
		}

		public void CtoR(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, ValueType.Complex);
			var value = (ComplexValue)cmd.UseTopValue();
			cmd.Commit(new DoubleValue(value.AsComplex.Real), new DoubleValue(value.AsComplex.Imaginary));
		}

		public void Imag(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireComplexNumericType(0);

			var value = cmd.UseTopValue();
			if (value.ValueType == ValueType.Complex)
			{
				var complexValue = (ComplexValue)value;
				cmd.Commit(new DoubleValue(complexValue.AsComplex.Imaginary));
			}
			else
			{
				cmd.Commit(new DoubleValue(0));
			}
		}

		public void Real(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireComplexNumericType(0);

			var value = cmd.UseTopValue();
			if (value.ValueType == ValueType.Complex)
			{
				var complexValue = (ComplexValue)value;
				cmd.Commit(new DoubleValue(complexValue.AsComplex.Real));
			}
			else
			{
				cmd.Commit(value);
			}
		}

		public void RtoC(Command cmd)
		{
			this.RequireArgs(2);
			this.RequireScalarNumericType(0);
			this.RequireScalarNumericType(1);
			var values = cmd.UseTopValues(2);
			var real = ((NumericValue)values[1]).ToDouble();
			var imag = ((NumericValue)values[0]).ToDouble();
			cmd.Commit(new ComplexValue(new Complex(real, imag)));
		}

		public void Phase(Command cmd)
		{
			// Note: Complex Magnitude can be calculated with Abs.
			this.RequireArgs(1);
			this.RequireType(0, ValueType.Complex);
			var value = (ComplexValue)cmd.UseTopValue();
			double phaseInRadians = value.AsComplex.Phase;
			double phaseAngle = this.Calc.ConvertFromRadiansToAngle(phaseInRadians);
			cmd.Commit(new DoubleValue(phaseAngle));
		}

		#endregion
	}
}
