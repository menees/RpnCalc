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

	public class Ribbon : ItemsControl
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
