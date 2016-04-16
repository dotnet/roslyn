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
            CSharpProjectShim projectSite = GetProjectSite(project);

            if (!this.CurrentProjectReferencesContains(projectSite.Id))
            {
                throw new ArgumentException("The finalProject reference is not currently referenced by this finalProject.", "finalProject");
            }

            var projectReferences = GetCurrentProjectReferences().Where(r => r.ProjectId == projectSite.Id);

            foreach (var projectReference in projectReferences)
            {
                RemoveProjectReference(projectReference);
            }
        }

        public void OnDiskFileUpdated(string filename, ref System.Runtime.InteropServices.ComTypes.FILETIME pFT)
        {
            throw new NotImplementedException();
        }

        public void OnCodeDirectoryAliasesChanged(ICSharpProjectRoot project, int previousAliasesCount, string[] previousAliases, int currentAliasesCount, string[] currentAliases)
        {
            CSharpProjectShim projectSite = GetProjectSite(project);

            UpdateProjectReferenceAliases(projectSite, ImmutableArray.Create(currentAliases));
        }

        public void AddReferenceToCodeDirectoryEx(string assemblyFileName, ICSharpProjectRoot project, CompilerOptions optionID)
        {
            CSharpProjectShim projectSite = GetProjectSite(project);

            AddProjectReference(new ProjectReference(projectSite.Id));
        }

        /// <summary>
        /// Given a ICSharpProjectRoot instance, it returns the ProjectSite instance, throwing if it
        /// could not be obtained.
        /// </summary>
        private static CSharpProjectShim GetProjectSite(ICSharpProjectRoot project)
        {
            // Get the host back for the project
            Guid projectSiteGuid = typeof(ICSharpProjectSite).GUID;
            CSharpProjectShim projectSite = project.GetProjectSite(ref projectSiteGuid) as CSharpProjectShim;

            // We should have gotten a ProjectSite back. If we didn't, that means we're being given
            // a project site that we didn't get BindToProject called on first which is a no-no by
            // the project system.
            if (projectSite == null)
            {
                throw new ArgumentException("finalProject was not properly sited with the languageServices service.", "finalProject");
            }

            return projectSite;
        }
    }
}
