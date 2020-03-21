#region Using Directives

using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Globalization;

#endregion

namespace Menees.RpnCalc.Internal
{
	internal abstract class Commands
	{
		#region Constructors

		protected Commands(Calculator calc)
		{
			this.m_calc = calc;
		}

		#endregion

		#region Public Methods

		public Func<Command, object> FindCommand(string commandName)
		{
			Func<Command, object> result = this.FindCommand(commandName, c_commandType, null);
			return result;
		}

		public Func<Command, object> FindCommand(string commandName, int commandParameter)
		{
			Func<Command, object> result = this.FindCommand(commandName, c_commandAndIntTypes, commandParameter);
			return result;
		}

		#endregion

		#region Protected Properties

		protected Calculator Calc
		{
			get
			{
				return this.m_calc;
			}
		}

		protected ValueStack Stack
		{
			get
			{
				return this.m_calc.Stack;
			}
		}

		#endregion

		#region Protected Methods

		protected void RequireArgs(int requiredArgCount)
		{
			if (this.m_calc.Stack.Count < requiredArgCount)
			{
				string message;
				switch (requiredArgCount)
				{
					case 1:
						message = Resources.Commands_AnArgumentIsRequired;
						break;
					case 2:
						message = Resources.Commands_TwoArgumentsAreRequired;
						break;
					default:
						// Most commands take 0, 1, or 2 args, but some Stack
						// commands take N+1 args (e.g., DupN, DropN).
						message = string.Format(
							CultureInfo.CurrentCulture,
							Resources.Commands_0ArgumentsAreRequired, requiredArgCount);
						break;
				}

				throw InvalidOperation(message);
			}
		}

		protected static void RequireNonNegativeCount(int count)
		{
			if (count < 0)
			{
				throw new ArgumentException(Resources.Commands_CountMustBeNonNegative);
			}
		}

		protected static void RequirePositiveStackPosition(int displayStackPosition)
		{
			if (displayStackPosition < 1)
			{
				throw new ArgumentException(Resources.Commands_PositionMustBePositive);
			}
		}

		protected void RequireType(int offsetFromTop, params ValueType[] supportedTypes)
		{
			Value value = this.m_calc.Stack.PeekAt(offsetFromTop);

			if (!supportedTypes.Contains(value.ValueType))
			{
				string message = string.Format(
					CultureInfo.CurrentCulture,
					Resources.Commands_Item0MustHaveType1, offsetFromTop + 1,
					Utility.Join(Resources.Commands_JoiningOr, supportedTypes));
				throw InvalidOperation(message);
			}
		}

		protected void RequireScalarNumericType(int offsetFromTop)
		{
			Value value = this.m_calc.Stack.PeekAt(offsetFromTop);

			if (!c_scalarNumericTypes.Contains(value.ValueType))
			{
				string message = string.Format(
					CultureInfo.CurrentCulture,
					Resources.Commands_Item0MustBeAScalarNumber, offsetFromTop + 1);
				throw InvalidOperation(message);
			}
		}

		protected void RequireComplexNumericType(int offsetFromTop)
		{
			Value value = this.m_calc.Stack.PeekAt(offsetFromTop);

			if (!c_complexNumericTypes.Contains(value.ValueType))
			{
				string message = string.Format(
					CultureInfo.CurrentCulture,
					Resources.Commands_Item0MustBeAScalarOrComplex,
					offsetFromTop + 1);
				throw InvalidOperation(message);
			}
		}

		protected void RequireMatchingTypes(int offsetFromTop1, int offsetFromTop2)
		{
			ValueStack stack = this.m_calc.Stack;
			Value value1 = stack.PeekAt(offsetFromTop1);
			Value value2 = stack.PeekAt(offsetFromTop2);

			if (value1.ValueType != value2.ValueType)
			{
				string message = string.Format(
					CultureInfo.CurrentCulture,
					Resources.Commands_Items0And1MustHaveTheSameType,
					offsetFromTop1 + 1, offsetFromTop2 + 1);
				throw InvalidOperation(message);
			}
		}

		protected void RequireScalarNumericTypeOr(int offsetFromTop, params ValueType[] otherSupportedTypes)
		{
			this.RequireType(offsetFromTop, c_scalarNumericTypes.Concat(otherSupportedTypes).ToArray());
		}

		protected void RequireComplexNumericTypeOr(int offsetFromTop, params ValueType[] otherSupportedTypes)
		{
			this.RequireType(offsetFromTop, c_complexNumericTypes.Concat(otherSupportedTypes).ToArray());
		}

		#endregion

		#region Private Methods

		private static InvalidOperationException InvalidOperation(string message)
		{
			return new InvalidOperationException(message);
		}

		private Func<Command, object> FindCommand(string commandName, Type[] commandArgTypes, object commandParameter)
		{
			Func<Command, object> result = null;

			Type thisType = this.GetType();
			MethodInfo method = thisType.GetMethod(commandName, commandArgTypes);
			if (method != null)
			{
				result = delegate (Command cmd)
				{
					object[] commandArgs;
					if (commandParameter != null)
					{
						commandArgs = new object[] { cmd, commandParameter };
					}
					else
					{
						commandArgs = new object[] { cmd };
					}

					object target = method.IsStatic ? null : this;
					object methodResult = method.Invoke(target, commandArgs);
					return methodResult;
				};
			}

			return result;
		}

		#endregion

		#region Private Data Members

		private Calculator m_calc;

		private static readonly ValueType[] c_scalarNumericTypes =
		{ ValueType.Integer, ValueType.Double, ValueType.Fraction, ValueType.Binary };

		private static readonly ValueType[] c_complexNumericTypes =
		{ ValueType.Integer, ValueType.Double, ValueType.Complex, ValueType.Fraction, ValueType.Binary };

		private static readonly Type[] c_commandType = { typeof(Command) };
		private static readonly Type[] c_commandAndIntTypes = { typeof(Command), typeof(int) };

		#endregion
	}
}
