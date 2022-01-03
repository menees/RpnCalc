namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	#endregion

	internal sealed class SettingsNode : INode
	{
		#region Private Data Members

		private readonly ISettingsNode node;

		#endregion

		#region Constructors

		public SettingsNode(ISettingsNode node)
		{
			this.node = node;
		}

		#endregion

		#region Public Methods

		public string GetValue(string name, string defaultValue)
		{
			return this.node.GetValue(name, defaultValue);
		}

		public string? GetValueN(string name, string? defaultValue)
		{
			return this.node.GetValueN(name, defaultValue);
		}

		public int GetValue(string name, int defaultValue)
		{
			return this.node.GetValue(name, defaultValue);
		}

		public TEnum GetValue<TEnum>(string name, TEnum defaultValue)
			where TEnum : struct
		{
			return this.node.GetValue(name, defaultValue);
		}

		public void SetValue(string name, string? value)
		{
			this.node.SetValue(name, value);
		}

		public void SetValue(string name, int value)
		{
			this.node.SetValue(name, value);
		}

		public void SetValue<TEnum>(string name, TEnum value)
			where TEnum : struct
		{
			this.node.SetValue(name, value);
		}

		public INode GetNode(string name)
		{
			ISettingsNode child = this.node.GetSubNode(name);
			INode result = new SettingsNode(child);
			return result;
		}

		public INode? TryGetNode(string name)
		{
			ISettingsNode? child = this.node.TryGetSubNode(name);

			INode? result = null;
			if (child != null)
			{
				result = new SettingsNode(child);
			}

			return result;
		}

		public IEnumerable<INode> GetNodes()
		{
			foreach (string nodeName in this.node.GetSubNodeNames())
			{
				yield return this.GetNode(nodeName);
			}
		}

		#endregion
	}
}
