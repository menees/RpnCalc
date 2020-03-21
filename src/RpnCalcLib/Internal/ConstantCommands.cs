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
	internal class ConstantCommands : Commands
	{
		#region Constructors

		public ConstantCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Public Methods

		public static void E(Command cmd)
		{
			cmd.Commit(new DoubleValue(Math.E));
		}

		public static void I(Command cmd)
		{
			cmd.Commit(new ComplexValue(Complex.ImaginaryOne));
		}

		public static void MaxDate(Command cmd)
		{
			cmd.Commit(new DateTimeValue(DateTime.MaxValue));
		}

		public static void MaxDouble(Command cmd)
		{
			cmd.Commit(new DoubleValue(Double.MaxValue));
		}

		public static void MaxInteger(Command cmd)
		{
			cmd.Commit(new IntegerValue(int.MaxValue));
		}

		public static void MaxLong(Command cmd)
		{
			cmd.Commit(new IntegerValue(long.MaxValue));
		}

		public static void MinDate(Command cmd)
		{
			cmd.Commit(new DateTimeValue(DateTime.MinValue));
		}

		public static void MinDouble(Command cmd)
		{
			cmd.Commit(new DoubleValue(Double.MinValue));
		}

		public static void MinInteger(Command cmd)
		{
			cmd.Commit(new IntegerValue(int.MinValue));
		}

		public static void MinLong(Command cmd)
		{
			cmd.Commit(new IntegerValue(long.MinValue));
		}

		public static void Pi(Command cmd)
		{
			cmd.Commit(new DoubleValue(Math.PI));
		}

		#endregion
	}
}
