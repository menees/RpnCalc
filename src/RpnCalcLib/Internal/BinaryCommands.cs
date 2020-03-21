#region Using Directives

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace Menees.RpnCalc.Internal
{
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
            var values = RequireTwoBinaryValues(cmd);
            var result = BinaryValue.And(values[0], values[1]);
            cmd.Commit(result);
        }

        public void BtoI(Command cmd)
        {
            RequireArgs(1);
            RequireType(0, ValueType.Binary);
            var value = (BinaryValue)cmd.UseTopValue();
            cmd.Commit(new IntegerValue(value.ToInteger()));
        }

        public void ItoB(Command cmd)
        {
            RequireArgs(1);
            RequireType(0, ValueType.Integer, ValueType.Double);
            var value = (NumericValue)cmd.UseTopValue();
            cmd.Commit(new BinaryValue((ulong)value.ToInteger()));
        }

        public void Not(Command cmd)
        {
            RequireArgs(1);
            RequireType(0, ValueType.Binary);
            var value = (BinaryValue)cmd.UseTopValue();
            cmd.Commit(BinaryValue.Not(value, Calc));
        }

        public void Or(Command cmd)
        {
            var values = RequireTwoBinaryValues(cmd);
            var result = BinaryValue.Or(values[0], values[1]);
            cmd.Commit(result);
        }

        public void RotateLeft(Command cmd)
        {
            int numBits;
            BinaryValue value;
            RequireIntegerAndBinary(cmd, out numBits, out value);
            cmd.Commit(BinaryValue.RotateLeft(value, numBits, Calc));
        }

        public void RotateRight(Command cmd)
        {
            int numBits;
            BinaryValue value;
            RequireIntegerAndBinary(cmd, out numBits, out value);
            cmd.Commit(BinaryValue.RotateRight(value, numBits, Calc));
        }

        public void ShiftLeft(Command cmd)
        {
            int numBits;
            BinaryValue value;
            RequireIntegerAndBinary(cmd, out numBits, out value);
            cmd.Commit(BinaryValue.ShiftLeft(value, numBits, Calc));
        }

        public void ShiftRight(Command cmd)
        {
            int numBits;
            BinaryValue value;
            RequireIntegerAndBinary(cmd, out numBits, out value);
            cmd.Commit(BinaryValue.ShiftRight(value, numBits, Calc));
        }

        public void Xor(Command cmd)
        {
            var values = RequireTwoBinaryValues(cmd);
            var result = BinaryValue.Xor(values[0], values[1]);
            cmd.Commit(result);
        }

        #endregion

        #region Private Methods

        private BinaryValue[] RequireTwoBinaryValues(Command cmd)
        {
            RequireArgs(2);
            RequireType(0, ValueType.Binary);
            RequireType(1, ValueType.Binary);

            var values = cmd.UseTopValues(2);
            return new[] { (BinaryValue)values[0], (BinaryValue)values[1] };
        }

        private void RequireIntegerAndBinary(Command cmd, out int integerValue, out BinaryValue binaryValue)
        {
            RequireArgs(2);
            RequireType(0, ValueType.Integer, ValueType.Double);
            RequireType(1, ValueType.Binary);

            var values = cmd.UseTopValues(2);

            integerValue = (int)((NumericValue)values[0]).ToInteger();
            binaryValue = (BinaryValue)values[1];
        }

        #endregion
    }
}
