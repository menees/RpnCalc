namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Documents;
	using System.Windows.Ink;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Animation;
	using System.Windows.Shapes;

	#endregion

	public class DisplayStackItem : INotifyPropertyChanged
	{
		#region Constructors

		internal DisplayStackItem(Calculator calc, Value value, int position)
		{
			this.m_calc = calc;
			this.m_value = value;
			this.m_position = position;
		}

		#endregion

		#region Public Properties

		public string StackPosition
		{
			get
			{
				string result = string.Format(CultureInfo.CurrentCulture, "{0}: ", this.m_position + 1);
				return result;
			}
		}

		public string ValueText
		{
			get
			{
				string result = null;
				if (this.m_value != null)
				{
					result = this.m_value.ToString(this.m_calc);
				}

				return result;
			}
		}

		public IList<DisplayFormat> DisplayFormats
		{
			get
			{
				if (this.m_displayFormats == null)
				{
					// DisplayStack.xaml binds to the first four items in the returned list to
					// display them in a Grid in a ToolTip, so make sure we have that many.
					const int c_requiredDisplayFormatCount = 4;
					this.m_displayFormats = new List<DisplayFormat>(c_requiredDisplayFormatCount);

					if (this.m_value != null)
					{
						// Get rid of duplicate formats (e.g., formatting "123" with and without
						// commas produces the same result).  It's easiest to just eliminate them
						// here in one place rather than make each Value type deal with that possibility.
						var distinctOriginalFormats = this.m_value.GetAllDisplayFormats(this.m_calc).Distinct(new DisplayFormatValueComparer());

						// Add the original formats with a formatted name too.
						foreach (DisplayFormat format in distinctOriginalFormats)
						{
							this.m_displayFormats.Add(new DisplayFormat(format.FormatName + ":  ", format.DisplayValue));
						}
					}

					// Now make sure we return exactly 4 formats because that's what
					// DisplayStack's XAML expects to bind to.
					while (this.m_displayFormats.Count < c_requiredDisplayFormatCount)
					{
						this.m_displayFormats.Add(new DisplayFormat(string.Empty, string.Empty));
					}

					// If a value starts returning more than 4, then this
					// Assert should remind me to update things.
					Debug.Assert(
						this.m_displayFormats.Count == c_requiredDisplayFormatCount,
						"DisplayFormats should return exactly 4 formats since that's what DisplayStack's XAML binds to.");
				}

				return this.m_displayFormats;
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Internal Properties

		internal bool IsDummyItem
		{
			get
			{
				return this.m_value == null;
			}
		}

		internal int Position
		{
			get
			{
				return this.m_position;
			}

			set
			{
				if (this.m_position != value)
				{
					this.m_position = value;

					// Send a notification that the public string property
					// changed so the display will update.
					this.SendPropertyChanged(nameof(this.StackPosition));
				}
			}
		}

		#endregion

		#region Internal Methods

		internal void RefreshDisplayValues()
		{
			// Remove any cached formats.
			this.m_displayFormats = null;

			this.SendPropertyChanged(nameof(this.ValueText));
			this.SendPropertyChanged("ToolTipText");
		}

		#endregion

		#region Private Methods

		private void SendPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region DisplayFormatValueComparer

		private class DisplayFormatValueComparer : IEqualityComparer<DisplayFormat>
		{
			#region Public Methods

			public bool Equals(DisplayFormat x, DisplayFormat y)
			{
				bool result = object.Equals(x.DisplayValue, y.DisplayValue);
				return result;
			}

			public int GetHashCode(DisplayFormat obj)
			{
				int result = (obj.DisplayValue ?? string.Empty).GetHashCode();
				return result;
			}

			#endregion
		}

		#endregion

		#region Private Data Members

		private Calculator m_calc;
		private Value m_value;
		private int m_position;
		private List<DisplayFormat> m_displayFormats;

		#endregion
	}
}
