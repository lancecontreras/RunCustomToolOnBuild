using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Runtime.InteropServices;

namespace RunCustomToolOnBuild
{
	[ComVisible(true)]
	public class PropertyExtender
	{
		private readonly IVsBuildPropertyStorage _storage;
		private readonly uint _itemId;
		private readonly IExtenderSite _extenderSite;
		private readonly int _cookie;

		public PropertyExtender(IVsBuildPropertyStorage storage, uint itemId, IExtenderSite extenderSite, int cookie)
		{
			_storage = storage;
			_itemId = itemId;
			_extenderSite = extenderSite;
			_cookie = cookie;
		}

		~PropertyExtender()
		{
			try
			{
				if (_extenderSite != null)
					_extenderSite.NotifyDelete(_cookie);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error in PropertyExtender finalizer: {0}", ex);
			}
		}

		[Category("RunCustomToolOnBuild")]
		[DisplayName("RunCustomToolOnBuild")]
		[Description("When the project or solution is built, the custom tools for these files will run")]
		[Editor("System.ComponentModel.Design.BinaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
		public bool RunCustomTool
		{
			get
			{
				return LoadRunCustomToolOn();
			}
			set
			{
				SaveRunCustomToolOn(value);
			}
		}

		private bool LoadRunCustomToolOn()
		{
			string s;
			_storage.GetItemAttribute(_itemId, RunCustomToolPackage.TargetsPropertyName, out s);
			if (s != null)
			{
				bool bValue;
				if (bool.TryParse(s, out bValue))
					return bValue;
			}
			return false;
		}

		private void SaveRunCustomToolOn(bool WillRunCustomTool)
		{
			_storage.SetItemAttribute(_itemId, RunCustomToolPackage.TargetsPropertyName, WillRunCustomTool.ToString());
		}
	}
}
