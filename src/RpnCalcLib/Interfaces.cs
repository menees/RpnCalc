namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Linq;

	#endregion

	public interface INode
	{
		#region Public Methods

		string GetValue(string name, string defaultValue);

		int GetValue(string name, int defaultValue);

		TEnum GetValue<TEnum>(string name, TEnum defaultValue) where TEnum : struct;

		void SetValue(string name, string value);

		void SetValue(string name, int value);

		void SetValue<TEnum>(string name, TEnum value) where TEnum : struct;

		INode GetNode(string name, bool createIfNotFound);

		IEnumerable<INode> GetNodes();

		#endregion
	}
}