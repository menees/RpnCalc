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
		#region Private Data Members

		private readonly Calculator calc;
		private readonly Value value;
		private int position;
		private List<DisplayFormat> displayFormats;

		#endregion

		#region Constructors

		internal DisplayStackItem(Calculator calc, Value value, int position)
		{
			this.calc = calc;
			this.value = value;
			this.position = position;
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Public Properties

		public string StackPosition
		{
			get
			{
				string result = string.Format(CultureInfo.CurrentCulture, "{0}: ", this.position + 1);
				return result;
			}
		}

		public string ValueText
		{
			get
			{
				string result = null;
				if (this.value != null)
				{
					result = this.value.ToString(this.calc);
				}

				return result;
			}
		}

		public IList<DisplayFormat> DisplayFormats
		{
			get
			{
				if (this.displayFormats == null)
				{
					// DisplayStack.xaml binds to the first four items in the returned list to
					// display them in a Grid in a ToolTip, so make sure we have that many.
					const int c_requiredDisplayFormatCount = 4;
					this.displayFormats = new List<DisplayFormat>(c_requiredDisplayFormatCount);

					if (this.value != null)
					{
						// Get rid of duplicate formats (e.g., formatting "123" with and without
						// commas produces the same result).  It's easiest to just eliminate them
						// here in one place rather than make each Value type deal with that possibility.
						var distinctOriginalFormats = this.value.GetAllDisplayFormats(this.calc).Distinct(new DisplayFormatValueComparer());

						// Add the original formats with a formatted name too.
						foreach (DisplayFormat format in distinctOriginalFormats)
						{
							this.displayFormats.Add(new DisplayFormat(format.FormatName + ":  ", format.DisplayValue));
						}
					}

					// Now make sure we return exactly 4 formats because that's what
					// DisplayStack's XAML expects to bind to.
					while (this.displayFormats.Count < c_requiredDisplayFormatCount)
					{
						this.displayFormats.Add(new DisplayFormat(string.Empty, string.Empty));
					}

					// If a value starts returning more than 4, then this
					// Assert should remind me to update things.
					Debug.Assert(
						this.displayFormats.Count == c_requiredDisplayFormatCount,
						"DisplayFormats should return exactly 4 formats since that's what DisplayStack's XAML binds to.");
				}

				return this.displayFormats;
			}
		}

		#endregion

		#region Internal Properties

		internal bool IsDummyItem
		{
			get
			{
				return this.value == null;
			}
		}

		internal int Position
		{
			get
			{
				return this.position;
			}

			set
			{
				if (this.position != value)
				{
					this.position = value;

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
			this.displayFormats = null;

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
	}
}
