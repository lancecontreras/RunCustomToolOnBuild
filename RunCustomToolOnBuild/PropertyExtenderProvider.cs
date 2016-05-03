using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;


namespace RunCustomToolOnBuild
{
	[ComVisible(true)]
	[Guid(ExtenderGuid)]
	public class PropertyExtenderProvider : IExtenderProvider
	{
		public const string ExtenderName = "RunCustomToolOnBuild.PropertyExtenderProvider";
		public const string ExtenderGuid = "4D2DA9A5-F7A5-494C-9A57-FEDF78D064B2";

		private readonly DTE _dte;
		private readonly IServiceProvider _serviceProvider;

		public PropertyExtenderProvider(DTE dte, IServiceProvider serviceProvider)
		{
			_dte = dte;
			_serviceProvider = serviceProvider;
		}

		public object GetExtender(string extenderCATID, string extenderName, object extendeeObject, IExtenderSite extenderSite, int cookie)
		{
			dynamic extendee = extendeeObject;
			string fullPath = extendee.FullPath;
			var projectItem = _dte.Solution.FindProjectItem(fullPath);
			IVsSolution solution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
			IVsHierarchy projectHierarchy;
			if (solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out projectHierarchy) != 0)
				return null;
			uint itemId;
			if (projectHierarchy.ParseCanonicalName(fullPath, out itemId) != 0)
				return null;

			if (Path.GetExtension(fullPath).Equals(".resx", StringComparison.InvariantCultureIgnoreCase) || Path.GetExtension(fullPath).Equals(".tt", StringComparison.InvariantCultureIgnoreCase))
				return new PropertyExtender((IVsBuildPropertyStorage)projectHierarchy, itemId, extenderSite, cookie);

			return null;
		}

		public bool CanExtend(string extenderCATID, string extenderName, object extendeeObject)
		{
			dynamic extendee = extendeeObject;
			string fullPath = extendee.FullPath;
			var projectItem = _dte.Solution.FindProjectItem(fullPath);
			IVsSolution solution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
			IVsHierarchy projectHierarchy;
			if (solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out projectHierarchy) != 0)
				return false;
			uint itemId;
			if (projectHierarchy.ParseCanonicalName(fullPath, out itemId) != 0)
				return false;
			if (!Path.GetExtension(fullPath).Equals(".resx", StringComparison.InvariantCultureIgnoreCase) && !Path.GetExtension(fullPath).Equals(".tt", StringComparison.InvariantCultureIgnoreCase))
				return false; 
			return true;
		}
	}
}
