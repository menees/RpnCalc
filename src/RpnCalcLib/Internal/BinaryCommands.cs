namespace Menees.RpnCalc.Internal
{
	#region Using Directives

	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	#endregion

	internal class BinaryCommands : Commands
	{
		#region Constructors

		public BinaryCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Public Methods

		public void And(Command cmd)
		{
			var values = this.RequireTwoBinaryValues(cmd);
			var result = BinaryValue.And(values[0], values[1]);
			cmd.Commit(result);
		}

		public void BtoI(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, RpnValueType.Binary);
			var value = (BinaryValue)cmd.UseTopValue();
			cmd.Commit(new IntegerValue(value.ToInteger()));
		}

		public void ItoB(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, RpnValueType.Integer, RpnValueType.Double);
			var value = (NumericValue)cmd.UseTopValue();
			cmd.Commit(new BinaryValue((ulong)value.ToInteger()));
		}

		public void Not(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, RpnValueType.Binary);
			var value = (BinaryValue)cmd.UseTopValue();
			cmd.Commit(BinaryValue.Not(value, this.Calc));
		}

		public void Or(Command cmd)
		{
			var values = this.RequireTwoBinaryValues(cmd);
			var result = BinaryValue.Or(values[0], values[1]);
			cmd.Commit(result);
		}

		public void RotateLeft(Command cmd)
		{
			this.RequireIntegerAndBinary(cmd, out int numBits, out BinaryValue value);
			cmd.Commit(BinaryValue.RotateLeft(value, numBits, this.Calc));
		}

		public void RotateRight(Command cmd)
		{
			this.RequireIntegerAndBinary(cmd, out int numBits, out BinaryValue value);
			cmd.Commit(BinaryValue.RotateRight(value, numBits, this.Calc));
		}

		public void ShiftLeft(Command cmd)
		{
			this.RequireIntegerAndBinary(cmd, out int numBits, out BinaryValue value);
			cmd.Commit(BinaryValue.ShiftLeft(value, numBits, this.Calc));
		}

		public void ShiftRight(Command cmd)
		{
			this.RequireIntegerAndBinary(cmd, out int numBits, out BinaryValue value);
			cmd.Commit(BinaryValue.ShiftRight(value, numBits, this.Calc));
		}

		public void Xor(Command cmd)
		{
			var values = this.RequireTwoBinaryValues(cmd);
			var result = BinaryValue.Xor(values[0], values[1]);
			cmd.Commit(result);
		}

		#endregion

		#region Private Methods

		private BinaryValue[] RequireTwoBinaryValues(Command cmd)
		{
			this.RequireArgs(2);
			this.RequireType(0, RpnValueType.Binary);
			this.RequireType(1, RpnValueType.Binary);

			var values = cmd.UseTopValues(2);
			return new[] { (BinaryValue)values[0], (BinaryValue)values[1] };
		}

		private void RequireIntegerAndBinary(Command cmd, out int integerValue, out BinaryValue binaryValue)
		{
			this.RequireArgs(2);
			this.RequireType(0, RpnValueType.Integer, RpnValueType.Double);
			this.RequireType(1, RpnValueType.Binary);

			var values = cmd.UseTopValues(2);

			integerValue = (int)((NumericValue)values[0]).ToInteger();
			binaryValue = (BinaryValue)values[1];
		}

		#endregion
	}
}
