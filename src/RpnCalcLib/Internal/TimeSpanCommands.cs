#region Using Directives

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace Menees.RpnCalc.Internal
{
	internal class TimeSpanCommands : Commands
	{
		#region Constructors

		public TimeSpanCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Public Methods

		public void FromHr(Command cmd)
		{
			double value = this.UseTopDouble(cmd);
			TimeSpanValue result = new TimeSpanValue(TimeSpan.FromHours(value));
			cmd.Commit(result);
		}

		public void ToHr(Command cmd)
		{
			TimeSpan value = this.UseTopTimeSpan(cmd);
			DoubleValue result = new DoubleValue(value.TotalHours);
			cmd.Commit(result);
		}

		public void FromMin(Command cmd)
		{
			double value = this.UseTopDouble(cmd);
			TimeSpanValue result = new TimeSpanValue(TimeSpan.FromMinutes(value));
			cmd.Commit(result);
		}

		public void ToMin(Command cmd)
		{
			TimeSpan value = this.UseTopTimeSpan(cmd);
			DoubleValue result = new DoubleValue(value.TotalMinutes);
			cmd.Commit(result);
		}

		public void FromSec(Command cmd)
		{
			double value = this.UseTopDouble(cmd);
			TimeSpanValue result = new TimeSpanValue(TimeSpan.FromSeconds(value));
			cmd.Commit(result);
		}

		public void ToSec(Command cmd)
		{
			TimeSpan value = this.UseTopTimeSpan(cmd);
			DoubleValue result = new DoubleValue(value.TotalSeconds);
			cmd.Commit(result);
		}

		#endregion

		#region Private Methods

		private double UseTopDouble(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireScalarNumericType(0);
			var value = (NumericValue)cmd.UseTopValue();
			return value.ToDouble();
		}

		private TimeSpan UseTopTimeSpan(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, RpnValueType.TimeSpan);
			var result = (TimeSpanValue)cmd.UseTopValue();
			return result.AsTimeSpan;
		}

		#endregion
	}
}
