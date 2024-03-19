// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.LanguageServices
{
    internal static class FSharpVisualStudioWorkspaceExtensions
    {
        public static Metadata GetMetadata(this VisualStudioWorkspace workspace, string fullPath, DateTime snapshotTimestamp)
        {
            var metadataReferenceProvider = workspace.Services.GetRequiredService<VisualStudioMetadataReferenceManager>();
            return metadataReferenceProvider.GetMetadata(fullPath, snapshotTimestamp);
        }

        [Obsolete("When Roslyn/ProjectSystem integration is finished, don't use this.")]
        public static bool TryGetProjectIdByBinPath(this VisualStudioWorkspace workspace, string filePath, [NotNullWhen(true)] out ProjectId? projectId)
        {
            var projects = workspace.CurrentSolution.Projects.Where(p => string.Equals(p.OutputFilePath, filePath, StringComparison.OrdinalIgnoreCase)).ToList();

            if (projects.Count == 1)
            {
                projectId = projects[0].Id;
                return true;
            }
            else
            {
                projectId = null;
                return false;
            }
        }

        [Obsolete("When Roslyn/ProjectSystem integration is finished, don't use this.")]
        public static ProjectId GetOrCreateProjectIdForPath(this VisualStudioWorkspace workspace, string filePath, string projectDisplayName)
        {
            // HACK: to keep F# working, we will ensure we return the ProjectId if there is a project that matches this path. Otherwise, we'll just return
            // a random ProjectId, which is sufficient for their needs. They'll simply observe there is no project with that ID, and then go and create a
            // new project. Then they call this function again, and fetch the real ID.
            return workspace.CurrentSolution.Projects.FirstOrDefault(p => p.FilePath == filePath)?.Id ?? ProjectId.CreateNewId("ProjectNotFound");
        }

        [Obsolete("When Roslyn/ProjectSystem integration is finished, don't use this.")]
        public static string? GetProjectFilePath(this VisualStudioWorkspace workspace, ProjectId projectId)
        {
            return workspace.CurrentSolution.GetProject(projectId)?.FilePath;
        }
    }
}
