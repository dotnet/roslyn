// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim : ICSharpVenusProjectSite
    {
        public void AddReferenceToCodeDirectory(string assemblyFileName, ICSharpProjectRoot project)
        {
            AddReferenceToCodeDirectoryEx(assemblyFileName, project, CompilerOptions.OPTID_IMPORTS);
        }

        public void RemoveReferenceToCodeDirectory(string assemblyFileName, ICSharpProjectRoot project)
        {
            var projectSite = GetProjectSite(project);

            var projectReferencesToRemove = VisualStudioProject.GetProjectReferences().Where(p => p.ProjectId == projectSite.VisualStudioProject.Id).ToList();

            if (projectReferencesToRemove.Count == 0)
            {
                throw new ArgumentException($"The project {nameof(project)} is not currently referenced by this project.");
            }

            foreach (var projectReferenceToRemove in projectReferencesToRemove)
            {
                VisualStudioProject.RemoveProjectReference(new ProjectReference(projectSite.VisualStudioProject.Id));
            }
        }

        public void OnDiskFileUpdated(string filename, ref System.Runtime.InteropServices.ComTypes.FILETIME pFT)
        {
            throw new NotImplementedException();
        }

        public void OnCodeDirectoryAliasesChanged(ICSharpProjectRoot project, int previousAliasesCount, string[] previousAliases, int currentAliasesCount, string[] currentAliases)
        {
            var projectSite = GetProjectSite(project);

            using (VisualStudioProject.CreateBatchScope())
            {
                var existingProjectReference = VisualStudioProject.GetProjectReferences().Single(p => p.ProjectId == projectSite.VisualStudioProject.Id);

                VisualStudioProject.RemoveProjectReference(existingProjectReference);
                VisualStudioProject.AddProjectReference(new ProjectReference(existingProjectReference.ProjectId, ImmutableArray.Create(currentAliases), existingProjectReference.EmbedInteropTypes));
            }
        }

        public void AddReferenceToCodeDirectoryEx(string assemblyFileName, ICSharpProjectRoot projectRoot, CompilerOptions optionID)
        {
            var projectSite = GetProjectSite(projectRoot);

            VisualStudioProject.AddProjectReference(new ProjectReference(projectSite.VisualStudioProject.Id, embedInteropTypes: optionID == CompilerOptions.OPTID_IMPORTSUSINGNOPIA));
        }

        /// <summary>
        /// Given a ICSharpProjectRoot instance, it returns the ProjectSite instance, throwing if it
        /// could not be obtained.
        /// </summary>
        private static CSharpProjectShim GetProjectSite(ICSharpProjectRoot project)
        {
            // Get the host back for the project
            var projectSiteGuid = typeof(ICSharpProjectSite).GUID;

            // We should have gotten a ProjectSite back. If we didn't, that means we're being given
            // a project site that we didn't get BindToProject called on first which is a no-no by
            // the project system.
            if (!(project.GetProjectSite(ref projectSiteGuid) is CSharpProjectShim projectSite))
            {
                throw new ArgumentException($"{project} was not properly sited with the language service.", nameof(project));
            }

            return projectSite;
        }
    }
}
