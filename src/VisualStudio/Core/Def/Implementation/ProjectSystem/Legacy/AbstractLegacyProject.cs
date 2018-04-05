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
            bool getIsCurrentContext(IVisualStudioHostDocument document) => LinkedFileUtilities.IsCurrentContextHierarchy(document, RunningDocumentTable);
            AddFile(filename, sourceCodeKind, getIsCurrentContext, GetFolderNamesFromHierarchy);
        }

        protected void SetOutputPathAndRelatedData(string objOutputPath)
        {
            // Update the objOutputPath and related data.
            SetObjOutputPathAndRelatedData(objOutputPath);
            // Also fetch and update the new binOutputPath.
            if (TryGetOutputPathFromHierarchy(this.Hierarchy, this.ContainingDirectoryPathOpt, out var binOutputPath))
            {
                SetBinOutputPathAndRelatedData(binOutputPath);
            }
        }

        private static bool TryGetOutputPathFromHierarchy(IVsHierarchy hierarchy, string containingDirectoryPathOpt, out string binOutputPath)
        {
            binOutputPath = null;
            var storage = hierarchy as IVsBuildPropertyStorage;
            if (storage == null)
            {
                return false;
            }

            if (ErrorHandler.Failed(storage.GetPropertyValue("OutDir", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out var outputDirectory)) ||
                ErrorHandler.Failed(storage.GetPropertyValue("TargetFileName", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out var targetFileName)))
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

            if (outputDirectory == null || targetFileName == null)
            {
                return false;
            }

            binOutputPath = FileUtilities.NormalizeAbsolutePath(Path.Combine(outputDirectory, targetFileName));
            return true;
        }

        private static string GetProjectDisplayName(IVsHierarchy hierarchy)
        {
            return hierarchy.TryGetName(out var name) ? name : null;
        }

        internal static string GetProjectFilePath(IVsHierarchy hierarchy)
        {
            return ErrorHandler.Succeeded(((IVsProject3)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out var filePath)) ? filePath : null;
        }

        private static string GetProjectType(IVsHierarchy hierarchy)
        {
            var aggregatableProject = hierarchy as IVsAggregatableProject;
            if (aggregatableProject == null)
            {
                return string.Empty;
            }

            if (ErrorHandler.Succeeded(aggregatableProject.GetAggregateProjectTypeGuids(out var projectType)))
            {
                return projectType;
            }

            return string.Empty;
        }

        private static Guid GetProjectIDGuid(IVsHierarchy hierarchy)
        {
            if (hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, out var guid))
            {
                return guid;
            }

            return Guid.Empty;
        }

        private static bool GetIsWebsiteProject(IVsHierarchy hierarchy)
        {
            try
            {
                if (hierarchy.TryGetProject(out var project))
                {
                    return project.Kind == VsWebSite.PrjKind.prjKindVenusProject;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}
