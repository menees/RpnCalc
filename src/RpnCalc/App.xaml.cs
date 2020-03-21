namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using Menees.Windows.Presentation;

	#endregion

	public partial class App : Application
	{
		#region Constructors

		public App()
		{
			WindowsUtility.InitializeApplication("RPN Calc 3.0", null);
		}

		#endregion
	}
}
