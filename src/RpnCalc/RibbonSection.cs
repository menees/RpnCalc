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

	public class RibbonSection : HeaderedContentControl
	{
		#region Constructors

		public RibbonSection()
		{
			this.DefaultStyleKey = typeof(RibbonSection);
			this.IsTabStop = false;
		}

		#endregion
	}
}
