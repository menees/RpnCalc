namespace Menees.RpnCalc.Internal
{
	#region Using Directives

	using System;

	#endregion

	#region CommandState

	internal enum CommandState
	{
		None,
		Committed,
		Cancelled,
	}

	#endregion
}
