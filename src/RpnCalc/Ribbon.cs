namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Animation;
	using System.Windows.Shapes;

	#endregion

#pragma warning disable CA1724 // Name conflicts with system type. I don't care about System.Windows.Controls.Ribbon.
	public class Ribbon : ItemsControl
#pragma warning restore CA1724
	{
		#region Constructors

		public Ribbon()
		{
			this.DefaultStyleKey = typeof(Ribbon);
			this.IsTabStop = false;
		}

		#endregion
	}
}
