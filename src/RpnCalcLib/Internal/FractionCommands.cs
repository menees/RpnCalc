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
	internal class FractionCommands : Commands
	{
		#region Constructors

		public FractionCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Public Methods

		public void FtoR(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, ValueType.Fraction);
			var value = (FractionValue)cmd.UseTopValue();
			cmd.Commit(new IntegerValue(value.Numerator), new IntegerValue(value.Denominator));
		}

		public void RtoF(Command cmd)
		{
			this.RequireArgs(2);
			this.RequireScalarNumericType(0);
			this.RequireScalarNumericType(1);
			var values = cmd.UseTopValues(2);
			var numerator = ((NumericValue)values[1]).ToInteger();
			var denominator = ((NumericValue)values[0]).ToInteger();
			cmd.Commit(new FractionValue(numerator, denominator));
		}

		public void DtoF(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireScalarNumericType(0);
			var value = (NumericValue)cmd.UseTopValue();
			NumericValue result = value;
			switch (value.ValueType)
			{
				case ValueType.Binary:
					result = new FractionValue(((BinaryValue)value).ToInteger(), BigInteger.One);
					break;
				case ValueType.Integer:
					result = new FractionValue(((IntegerValue)value).AsInteger, BigInteger.One);
					break;
				case ValueType.Double:
					result = Utility.DoubleToFraction(((DoubleValue)value).AsDouble);
					break;
			}

			cmd.Commit(result);
		}

		public void FtoD(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, ValueType.Fraction);
			var value = (FractionValue)cmd.UseTopValue();
			cmd.Commit(new DoubleValue(value.ToDouble()));
		}

		#endregion
	}
}
