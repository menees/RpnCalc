#region Using Directives

using System;

#endregion

namespace Menees.RpnCalc.Internal
{
	#region CommandState

	internal enum CommandState
	{
		None,
		Committed,
		Cancelled
	}

	#endregion
}
