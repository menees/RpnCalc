#region Using Directives

using System;
using System.Globalization;

#endregion

namespace Menees.RpnCalc
{
    public class DisplayFormat
    {
        #region Constructors

        public DisplayFormat(string displayValue)
            : this(Resources.DisplayFormat_Standard, displayValue)
        {
        }

        public DisplayFormat(string formatName, string displayValue)
        {
            FormatName = formatName;
            DisplayValue = displayValue;
        }

        #endregion

        #region Public Properties

        public string FormatName { get; private set; }
        public string DisplayValue { get; private set; }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            string result = string.Format(CultureInfo.CurrentCulture,
                Resources.DisplayFormat_NameValueFormat, FormatName, DisplayValue);
            return result;
        }

        #endregion
    }
}
