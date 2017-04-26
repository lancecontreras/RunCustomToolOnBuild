using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RunCustomToolOnBuild
{
  internal static class ExtensionHelper
  {
    internal static string GetPath(this ProjectItem projectItem)
    {
      return (string)projectItem.GetValue("FullPath");
    }

    internal static IEnumerable<ProjectItem> GetChildren(this ProjectItem projectItem)
    {
      foreach (ProjectItem child in projectItem.ProjectItems)
        yield return child;
    }

    internal static ProjectItem GetGeneratedItem(this ProjectItem projectItem)
    {
      foreach (ProjectItem item in projectItem.GetChildren())
      {
        if (Path.GetFileNameWithoutExtension(item.GetPath()).Equals(Path.GetFileNameWithoutExtension(projectItem.GetPath())))
          return item;
      }
      return null;
    }

    internal static bool HasChild(this ProjectItem projectItem)
    {
      return projectItem.ProjectItems?.Count > 0;
    }

    internal static object GetValue(this ProjectItem item, object index)
    {
      try
      {
        if (item == null || item.Properties == null)
          return null;

        var prop = item.Properties.Item(index);
        if (prop != null)
          return prop.Value;
      }
      catch (ArgumentException) { }
      return null;
    }

    internal static bool HasSettings(this ProjectItem projectItem)
    {
      return File.Exists(GetSettingsFileName(projectItem));
    }

    internal static string GetSettingsFileName(this ProjectItem projectItem)
    {
      IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
      IVsHierarchy project;
      solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out project);
      if (project == null) return null;
      string projectName = projectItem.ContainingProject.FullName;
      return Path.Combine(Path.GetDirectoryName(projectName), Path.GetFileNameWithoutExtension(projectName) + ".rctob");
    }

    internal static bool IsFileEmpty(string fileName)
    {
      if (File.Exists(fileName))
      {
        using (StreamReader sr = new StreamReader(fileName))
        {
          string line = string.Empty;
          do
          {
            if (line.Length > 0)
              return false;
          }
          while ((line = sr.ReadLine()) != null);
        }
      }
      return true;
    }

    internal static string GetLastSolution(this ProjectItem projectItem)
    {
      string lastSolution = string.Empty;
      if (projectItem.HasSettings())
      {
        string fileName = projectItem.GetSettingsFileName();
        if (!IsFileEmpty(fileName))
        {
          string[] lines = File.ReadAllLines(fileName);
          if (lines.Length > 0)
            lastSolution = lines[0];
        }
      }

      return lastSolution;
    }

    internal static DateTime GetLastModified(this ProjectItem projectItem)
    {
      return File.GetLastWriteTime(projectItem.GetPath());
    }

    internal static DateTime? GetLastModified(string fileName)
    {
      if (File.Exists(fileName))
      {
        return File.GetLastWriteTime(fileName);
      }
      return null;
    }

    internal static bool IsGeneratedFileUpdated(this ProjectItem projectItem)
    {
      return projectItem.GetLastModified() < projectItem.GetGeneratedItem().GetLastModified();
    }

    internal static bool IsGeneratedFileUpdated(this ProjectItem projectItem, string referenceFile)
    {
      bool isNewerThanParent = (projectItem.GetLastModified() < projectItem.GetGeneratedItem().GetLastModified());
      bool isNewerThanReferenceFile = true;
      DateTime? referenceLastModified = GetLastModified(referenceFile);
      if (referenceLastModified.HasValue)
        isNewerThanReferenceFile = (referenceLastModified < projectItem.GetGeneratedItem().GetLastModified());

      return isNewerThanParent && isNewerThanReferenceFile;
    }

    internal static void SetLastSolution(this ProjectItem projectItem, string lastSolution)
    {
      string fileName = projectItem.GetSettingsFileName();
      if (projectItem.HasSettings())
        File.Delete(fileName);
      File.WriteAllText(fileName, lastSolution);
      File.SetAttributes(fileName, FileAttributes.Hidden | FileAttributes.Archive);
    }

    public static List<string> GetReferenceAssemblies(string fileName)
    {
      List<string> referenceLines = null;
      if (Path.GetExtension(fileName) == ".tt")
      {
        string line;

        using (System.IO.StreamReader file = new System.IO.StreamReader(fileName))
        {
          Wildcard wildCard = new Wildcard("<#@ assembly name=\"*\" #>");
          while ((line = file.ReadLine()) != null)
          {
            if (wildCard.IsMatch(line))
            {
              if (referenceLines == null)
                referenceLines = new List<string>();
              line = line.Replace("<#@ assembly name=\"", "").Replace("\" #>", "");
              referenceLines.Add(line);
            }
          }

          file.Close();
        }
      }
      return referenceLines;
    }

    public static bool IsGeneratedFileUpdated(this ProjectItem projectItem, List<string> referenceFiles, string solutionDir, string activeConfiguration)
    {
      foreach (string fileName in referenceFiles)
      {
        string referenceFileName = fileName.Replace("$(SolutionDir)", solutionDir).Replace("$(Configuration)", activeConfiguration);
        if (File.Exists(referenceFileName))
        {
          if (!projectItem.IsGeneratedFileUpdated(referenceFileName))
            return false;
        }
      }
      return true;
    }
  }
}