namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Navigation;
	using System.Windows.Shapes;
	using Menees.Windows.Presentation;

	#endregion

	/// <summary>
	/// Interaction logic for DisplayStack.xaml
	/// </summary>
	public partial class DisplayStack
	{
		#region Constructors

		public DisplayStack()
		{
			this.InitializeComponent();
			this.FinishInitialize();
		}

		#endregion

		#region Private Methods

		private bool IsInDesignMode()
		{
			return WindowsUtility.IsInDesignMode(this);
		}

		#endregion

		#region Private Event Handlers

		private void ListBox_MouseRightButtonDown(object? sender, MouseButtonEventArgs e)
		{
			HitTestResult result = VisualTreeHelper.HitTest(this.listBox, e.GetPosition(null));
			if (result.VisualHit is ListBoxItem item)
			{
				item.IsSelected = true;
				item.Focus();
			}
		}

		#endregion
	}
}
