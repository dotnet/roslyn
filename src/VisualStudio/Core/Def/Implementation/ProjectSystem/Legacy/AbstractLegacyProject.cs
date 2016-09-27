// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using VSLangProj;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    /// <summary>
    /// Base type for legacy C# and VB project system shim implementations.
    /// These legachy shims are based on legacy project system interfaces defined in csproj/msvbprj.
    /// </summary>
    internal abstract partial class AbstractLegacyProject : AbstractProject
    {
        public AbstractLegacyProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            string language,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt = null)
            : base(projectTracker,
                  reportExternalErrorCreatorOpt,
                  projectSystemName,
                  projectFilePath: GetProjectFilePath(hierarchy),
                  hierarchy: hierarchy,
                  projectGuid: GetProjectIDGuid(hierarchy),
                  language: language,
                  serviceProvider: serviceProvider,
                  visualStudioWorkspaceOpt: visualStudioWorkspaceOpt,
                  hostDiagnosticUpdateSourceOpt: hostDiagnosticUpdateSourceOpt,
                  commandLineParserServiceOpt: commandLineParserServiceOpt)
        {
            if (Hierarchy != null)
            {
                ConnectHierarchyEvents();
                this.IsWebSite = GetIsWebsiteProject(Hierarchy);
            }

            // Initialize command line arguments.
            base.SetArguments(commandLine: string.Empty);
        }

        /// <summary>
        /// string (Guid) of the Hierarchy project type
        /// </summary>
        public string ProjectType => GetProjectType(Hierarchy);

        public override void Disconnect()
        {
            base.Disconnect();

            // Unsubscribe IVsHierarchyEvents
            DisconnectHierarchyEvents();
        }

        protected void AddFile(string filename, SourceCodeKind sourceCodeKind)
        {
            Func<IVisualStudioHostDocument, bool> getIsCurrentContext = document => LinkedFileUtilities.IsCurrentContextHierarchy(document, RunningDocumentTable);
            AddFile(filename, sourceCodeKind, getIsCurrentContext, GetFolderNamesFromHierarchy);
        }

        protected void SetOutputPathAndRelatedData(string objOutputPath)
        {
            // Update the objOutputPath and related data.
            SetObjOutputPathAndRelatedData(objOutputPath);

            // Also fetch and update the new binOutputPath.
            string binOutputPath;
            if (TryGetOutputPathFromHierarchy(this.Hierarchy, this.ContainingDirectoryPathOpt, out binOutputPath))
            {
                SetBinOutputPathAndRelatedData(binOutputPath);
            }
        }

        private static bool TryGetOutputPathFromHierarchy(IVsHierarchy hierarchy, string containingDirectoryPathOpt, out string binOutputPath)
        {
            binOutputPath = null;

            string outputDirectory;
            string targetFileName;

            var storage = hierarchy as IVsBuildPropertyStorage;
            if (storage == null)
            {
                return false;
            }

            if (ErrorHandler.Failed(storage.GetPropertyValue("OutDir", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out outputDirectory)) ||
                ErrorHandler.Failed(storage.GetPropertyValue("TargetFileName", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out targetFileName)))
            {
                return false;
            }

            // web app case
            if (!PathUtilities.IsAbsolute(outputDirectory))
            {
                if (containingDirectoryPathOpt == null)
                {
                    return false;
                }

                outputDirectory = FileUtilities.ResolveRelativePath(outputDirectory, containingDirectoryPathOpt);
            }

            binOutputPath = FileUtilities.NormalizeAbsolutePath(Path.Combine(outputDirectory, targetFileName));
            return true;
        }

        private static string GetProjectDisplayName(IVsHierarchy hierarchy)
        {
            string name;
            return hierarchy.TryGetName(out name) ? name : null;
        }

        internal static string GetProjectFilePath(IVsHierarchy hierarchy)
        {
            string filePath;
            return ErrorHandler.Succeeded(((IVsProject3)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out filePath)) ? filePath : null;
        }

        private static string GetProjectType(IVsHierarchy hierarchy)
        {
            var aggregatableProject = hierarchy as IVsAggregatableProject;
            if (aggregatableProject == null)
            {
                return string.Empty;
            }

            string projectType;
            if (ErrorHandler.Succeeded(aggregatableProject.GetAggregateProjectTypeGuids(out projectType)))
            {
                return projectType;
            }

            return string.Empty;
        }

        private static Guid GetProjectIDGuid(IVsHierarchy hierarchy)
        {
            Guid guid;
            if (hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, out guid))
            {
                return guid;
            }

            return Guid.Empty;
        }

        private static bool GetIsWebsiteProject(IVsHierarchy hierarchy)
        {
            EnvDTE.Project project;
            try
            {
                if (hierarchy.TryGetProject(out project))
                {
                    return project.Kind == VsWebSite.PrjKind.prjKindVenusProject;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        protected sealed override void ValidateReferences()
        {
            ValidateReferencesCore();
        }

        [Conditional("DEBUG")]
        private void ValidateReferencesCore()
        {
            // can happen when project is unloaded and reloaded or in Venus (aspx) case
            if (ProjectFilePath == null || BinOutputPath == null || ObjOutputPath == null)
            {
                return;
            }

            object property = null;
            if (ErrorHandler.Failed(Hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out property)))
            {
                return;
            }

            var dteProject = property as EnvDTE.Project;
            if (dteProject == null)
            {
                return;
            }

            var vsproject = dteProject.Object as VSProject;
            if (vsproject == null)
            {
                return;
            }

            var noReferenceOutputAssemblies = new List<string>();
            var factory = this.ServiceProvider.GetService(typeof(SVsEnumHierarchyItemsFactory)) as IVsEnumHierarchyItemsFactory;

            IEnumHierarchyItems items;
            if (ErrorHandler.Failed(factory.EnumHierarchyItems(Hierarchy, (uint)__VSEHI.VSEHI_Leaf, (uint)VSConstants.VSITEMID.Root, out items)))
            {
                return;
            }

            uint fetched;
            VSITEMSELECTION[] item = new VSITEMSELECTION[1];
            while (ErrorHandler.Succeeded(items.Next(1, item, out fetched)) && fetched == 1)
            {
                // ignore ReferenceOutputAssembly=false references since those will not be added to us in design time.
                var storage = Hierarchy as IVsBuildPropertyStorage;
                string value;
                storage.GetItemAttribute(item[0].itemid, "ReferenceOutputAssembly", out value);

                object caption;
                Hierarchy.GetProperty(item[0].itemid, (int)__VSHPROPID.VSHPROPID_Caption, out caption);

                if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
                {
                    noReferenceOutputAssemblies.Add((string)caption);
                }
            }

            var projectReferences = GetCurrentProjectReferences();
            var metadataReferences = GetCurrentMetadataReferences();
            var set = new HashSet<string>(vsproject.References.OfType<Reference>().Select(r => PathUtilities.IsAbsolute(r.Name) ? Path.GetFileNameWithoutExtension(r.Name) : r.Name), StringComparer.OrdinalIgnoreCase);
            var delta = set.Count - noReferenceOutputAssemblies.Count - (projectReferences.Length + metadataReferences.Length);
            if (delta == 0)
            {
                return;
            }

            // okay, two has different set of dlls referenced. check special Microsoft.VisualBasic case.
            if (delta != 1)
            {
                //// Contract.Requires(false, "different set of references!!!");
                return;
            }

            set.ExceptWith(noReferenceOutputAssemblies);
            set.ExceptWith(projectReferences.Select(r => ProjectTracker.GetProject(r.ProjectId).DisplayName));
            set.ExceptWith(metadataReferences.Select(m => Path.GetFileNameWithoutExtension(m.FilePath)));

            //// Contract.Requires(set.Count == 1);

            var reference = set.First();
            if (!string.Equals(reference, "Microsoft.VisualBasic", StringComparison.OrdinalIgnoreCase))
            {
                //// Contract.Requires(false, "unknown new reference " + reference);
                return;
            }

#if DEBUG
            // when we are missing microsoft.visualbasic reference, make sure we have embedded vb core option on.
            Contract.Requires(Debug_VBEmbeddedCoreOptionOn);
#endif
        }
    }
}
