using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace RunCustomToolOnBuild
{
  public static class StorageExtension
  {
    public static bool? GetBoolean(this IVsBuildPropertyStorage storage, uint itemId, string propertyName)
    {
      string runCustomToolOnBuildPropertyValue;
      if (storage.GetItemAttribute(itemId, propertyName, out runCustomToolOnBuildPropertyValue) != 0)
        return null;

      bool boolValue;
      if (bool.TryParse(runCustomToolOnBuildPropertyValue, out boolValue))
        return boolValue;

      return null;
    }

    public static string GetString(this IVsBuildPropertyStorage storage, uint itemId, string propertyName)
    {
      string runCustomToolOnBuildPropertyValue;
      if (storage.GetItemAttribute(itemId, propertyName, out runCustomToolOnBuildPropertyValue) != 0)
        return null;

      return runCustomToolOnBuildPropertyValue;
    }

    public static void SetValue(IVsBuildPropertyStorage storage, uint itemId, string propertyName, object value)
    {
      storage.SetItemAttribute(itemId, propertyName, value.ToString());
    }
  }
}