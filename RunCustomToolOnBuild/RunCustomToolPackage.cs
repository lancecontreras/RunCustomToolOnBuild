//------------------------------------------------------------------------------
// <copyright file="RunCustomToolPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

namespace RunCustomToolOnBuild
{
  [PackageRegistration(UseManagedResourcesOnly = true)]
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
  [Guid(RunCustomToolPackage.PackageGuidString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  [ProvideAutoLoad(UIContextGuids.SolutionExists)]
  public sealed class RunCustomToolPackage : Package
  {
    /// <summary>
    ///   RunCustomToolPackage GUID string.
    /// </summary>
    public const string PackageGuidString = "f9a70f0c-cb6b-4c22-9e9f-ce86369d191e";

    /// <summary>
    ///   Initializes a new instance of the <see cref="RunCustomToolPackage" /> class.
    /// </summary>
    public RunCustomToolPackage()
    {
      // Inside this method you can place any initialization code that does not require any Visual
      // Studio service because at this point the package object is created but not sited yet inside
      // Visual Studio environment. The place to do all the other initialization is the Initialize method.
    }

    private DocumentEvents _documentEvents;
    private DTE _dte;
    private Events _events;
    private OutputWindowPane _outputPane;
    private ErrorListProvider _errorListProvider;
    private readonly Dictionary<int, IExtenderProvider> _registerExtenderProviders = new Dictionary<int, IExtenderProvider>();
    public const string Property_RunCustomToolOnBuild = "RunCustomToolOnBuild";
    public const string Property_AlwaysRun = "AlwaysRun";
    public const string Property_ReferenceFile = "ReferenceFile";
    public const string LastBuiltOnPropertyName = "LastBuiltOnSolution";

    protected override void Initialize()
    {
      Debug.WriteLine("Entering Initialize() of: {0}", this);
      base.Initialize();

      _dte = (DTE)GetService(typeof(DTE));
      _events = _dte.Events;
      _documentEvents = _events.DocumentEvents;
      _events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
      var window = _dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

      var outputWindow = (OutputWindow)window.Object;

      _outputPane = outputWindow.OutputWindowPanes
                                .Cast<OutputWindowPane>()
                                .FirstOrDefault(p => p.Name.Equals("Build", StringComparison.CurrentCultureIgnoreCase));
      _errorListProvider = new ErrorListProvider(this)
      {
        ProviderName = "RunCustomToolOnBuild",
        ProviderGuid = Guid.NewGuid()
      };
      RegisterExtenderProvider();
    }

    private void RegisterExtenderProvider()
    {
      var provider = new PropertyExtenderProvider(_dte, this);
      string name = PropertyExtenderProvider.ExtenderName;
      RegisterExtenderProvider(VSConstants.CATID.CSharpFileProperties_string, name, provider);
      RegisterExtenderProvider(VSConstants.CATID.VBFileProperties_string, name, provider);
    }

    private void RegisterExtenderProvider(string extenderCatId, string name, IExtenderProvider extenderProvider)
    {
      int cookie = _dte.ObjectExtenders.RegisterExtenderProvider(extenderCatId, name, extenderProvider);
      _registerExtenderProviders.Add(cookie, extenderProvider);
    }

    private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
    {
      try
      {
        if (Scope == vsBuildScope.vsBuildScopeProject)
        {
          Project currentProject = GetCurrentProject();
          if (currentProject != null && currentProject.ProjectItems != null)
          {
            foreach (ProjectItem projectItem in currentProject.ProjectItems)
              CheckProjectItems(projectItem);
          }
          else
          {
            //If there's no selected project, try the whole solution
            foreach (Project project in _dte.Solution.Projects)
            {
              if (project != null && project.ProjectItems != null)
              {
                foreach (ProjectItem projectItem in project.ProjectItems)
                  CheckProjectItems(projectItem);
              }
            }
          }
        }
        else
        {
          foreach (Project project in _dte.Solution.Projects)
          {
            if (project != null && project.ProjectItems != null)
            {
              foreach (ProjectItem projectItem in project.ProjectItems)
                CheckProjectItems(projectItem);
            }
          }
        }
      }
      catch (Exception ex)
      {
        LogActivity(ex.ToString());
      }
    }

    private Project GetCurrentProject()
    {
      IntPtr hierarchyPointer, selectionContainerPointer;
      Object selectedObject = null;
      IVsMultiItemSelect multiItemSelect;
      uint projectItemId;

      IVsMonitorSelection monitorSelection =
              (IVsMonitorSelection)Package.GetGlobalService(
              typeof(SVsShellMonitorSelection));

      monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                           out projectItemId,
                                           out multiItemSelect,
                                           out selectionContainerPointer);

      IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                           hierarchyPointer,
                                           typeof(IVsHierarchy)) as IVsHierarchy;

      if (selectedHierarchy != null)
      {
        ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(
                                          projectItemId,
                                          (int)__VSHPROPID.VSHPROPID_ExtObject,
                                          out selectedObject));
      }

      Project selectedProject = selectedObject as Project;
      return selectedProject;
    }

    private string GetActiveConfiguration()
    {
      string activeConfiguration = _dte.DTE.Solution.SolutionBuild.ActiveConfiguration.Name;
      return activeConfiguration;
    }

    #region RCT

    private bool WillRunCustomToolOnBuild(ProjectItem projectItem)
    {
      IVsSolution solution = (IVsSolution)GetGlobalService(typeof(SVsSolution));
      IVsHierarchy project;
      solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out project);
      string docFullPath = projectItem.GetPath();

      if (docFullPath == null) return false;
      string customTool = projectItem.GetValue("CustomTool") as string;
      if (customTool != null && customTool != string.Empty)
      {
        IVsBuildPropertyStorage storage = project as IVsBuildPropertyStorage;
        if (storage == null)
          return false;

        uint itemId;
        if (project.ParseCanonicalName(docFullPath, out itemId) != 0)
          return false;

        bool? isRunCustomTool = storage.GetBoolean(itemId, Property_RunCustomToolOnBuild);
        bool? isAlwaysRun = storage.GetBoolean(itemId, Property_AlwaysRun);
        List<string> referenceFiles = new List<string>();

        if (Path.GetExtension(docFullPath) == ".tt") //If it's a T4 file we query the reference assemblies.
          referenceFiles = ExtensionHelper.GetReferenceAssemblies(docFullPath);

        if (isRunCustomTool.HasValue && isRunCustomTool.Value)
        {
          if (isAlwaysRun.HasValue && isAlwaysRun.Value)
            return true;

          // Get the solution file name
          string solDir, solFile, solOpts;
          solution.GetSolutionInfo(out solDir, out solFile, out solOpts);

          string lastBuiltIn = projectItem.GetLastSolution();
          string lastConfigurationBuild = projectItem.GetLastConfiguration();
          string activeConfiguration = GetActiveConfiguration();
          if (lastBuiltIn == solFile && lastConfigurationBuild == activeConfiguration) //in the same solution
          {
            if (projectItem.HasChild())
            {
                   
              string generatedItemFileName = projectItem.GetGeneratedItem().GetPath();
              if (!ExtensionHelper.IsFileEmpty(generatedItemFileName) &&
                IsGeneratedFileUpdated(projectItem, referenceFiles, solDir, activeConfiguration)) return false;
            }
          }
          else
            projectItem.UpdateLastBuild(solFile, activeConfiguration);

          return true;
        }
      }
      return false;
    }

    private bool IsGeneratedFileUpdated(ProjectItem projectItem, List<string> referenceFiles, string solutionDir, string activeConfiguration)
    {
      if (referenceFiles == null) return true;
      foreach (string fileName in referenceFiles)
      {
        string referenceFileName = fileName.Replace("$(SolutionDir)", solutionDir).Replace("$(Configuration)", activeConfiguration);
        
        // These are the assemblies referenced by path. Make sure it exist so there will be no error when we run custom tool. 
        if (referenceFileName.Contains(@":\") && !File.Exists(referenceFileName)) continue;

        // Check if the generated file is not new. We return false to allow the custom tool to run.
        if (!projectItem.IsGeneratedFileUpdated(referenceFileName)) return false;
      }
      return true;
    }

    private void CheckProjectItems(ProjectItem projectItem)
    {
      if (WillRunCustomToolOnBuild(projectItem))
      {
        RunCustomTool(projectItem);
        return;
      }
      if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
      {
        foreach (ProjectItem innerProjectItem in projectItem.ProjectItems)
          CheckProjectItems(innerProjectItem);
      }
    }

    private void RunCustomTool(ProjectItem projectItem)
    {
      IVsSolution solution = (IVsSolution)GetGlobalService(typeof(SVsSolution));
      IVsHierarchy project;
      solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out project);
      try
      {
        string docFullPath = projectItem.GetPath();
        if (docFullPath == null)
          docFullPath = projectItem.Name;

        LogActivity("{0}", docFullPath);
        VSLangProj.VSProjectItem vsProjectItem = projectItem.Object as VSLangProj.VSProjectItem;
        vsProjectItem.RunCustomTool();
      }
      catch
      {
        LogError(project, projectItem.Document.Name, $"Failed to Run Custom Tool on {projectItem.Name}");
      }
    }

    #endregion RCT

    #region Log

    private void LogActivity(string format, params object[] args)
    {
      string prefix = $"[{DateTime.Now.ToString("M/d/y h:mm:ss.FFF", CultureInfo.InvariantCulture)} RunCustomToolOnBuild] {format}";
      _outputPane.Activate();
      _outputPane.OutputString(string.Format(prefix, args) + Environment.NewLine);
    }

    private void LogError(IVsHierarchy project, string document, string format, params object[] args)
    {
      string text = string.Format(format, args);
      LogErrorTask(project, document, TaskErrorCategory.Error, text);
    }

    private void LogWarning(IVsHierarchy project, string document, string format, params object[] args)
    {
      string text = string.Format(format, args);
      LogErrorTask(project, document, TaskErrorCategory.Warning, text);
    }

    private void LogErrorTask(IVsHierarchy project, string document, TaskErrorCategory errorCategory, string text)
    {
      var task = new ErrorTask
      {
        Category = TaskCategory.BuildCompile,
        ErrorCategory = errorCategory,
        Text = $" {DateTime.Now.ToString("M/d/y h:mm:ss.FFF", CultureInfo.InvariantCulture)}] {text}",
        Document = document,
        HierarchyItem = project,
        Line = -1,
        Column = -1
      };
      _errorListProvider.Tasks.Add(task);
      string prefix = "";
      switch (errorCategory)
      {
        case TaskErrorCategory.Error:
          prefix = "[!";
          break;

        case TaskErrorCategory.Warning:
          prefix = "[*: ";
          break;
      }
      _outputPane.OutputString(prefix + text + Environment.NewLine);
    }

    #endregion Log
  }
}