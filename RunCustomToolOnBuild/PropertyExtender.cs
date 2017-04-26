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

    [Category("Run Custom Tool")]
    [DisplayName("Run Custom Tool On Build")]
    [Description("If true, the custom tool for the selected file will run on build.")]
    [Editor("System.ComponentModel.Design.BinaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    public bool RunCustomTool
    {
      get
      {
        return GetBoolValue(RunCustomToolPackage.Property_RunCustomToolOnBuild);
      }
      set
      {
        SetBoolValue(value, RunCustomToolPackage.Property_RunCustomToolOnBuild);
      }
    }

    [Category("Run Custom Tool")]
    [DisplayName("Always Run")]
    [Description("Always run the custom tool")]
    [Editor("System.ComponentModel.Design.BinaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    public bool AlwaysRun
    {
      get
      {
        return GetBoolValue(RunCustomToolPackage.Property_AlwaysRun);
      }
      set
      {
        SetBoolValue(value, RunCustomToolPackage.Property_AlwaysRun);
      }
    }

    private bool GetBoolValue(string propertyName)
    {
      string s;
      _storage.GetItemAttribute(_itemId, propertyName, out s);
      if (s != null)
      {
        bool bValue;
        if (bool.TryParse(s, out bValue))
          return bValue;
      }
      return false;
    }

    private void SetBoolValue(bool WillRunCustomTool, string propertyName)
    {
      _storage.SetItemAttribute(_itemId, propertyName, WillRunCustomTool.ToString());
    }
  }
}