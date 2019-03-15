using System;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.LanguageServices
{
    public static class VisualStudioWorkspaceExtensions
    {
        public static Metadata GetMetadata(this VisualStudioWorkspace workspace, string fullPath, DateTime snapshotTimestamp)
        {
            var metadataReferenceProvider = workspace.Services.GetService<VisualStudioMetadataReferenceManager>();
            return metadataReferenceProvider.GetMetadata(fullPath, snapshotTimestamp);
        }

        [Obsolete("This is a compatibility shim for F#; please do not use it.")]
        public static bool TryGetProjectIdByBinPath(this VisualStudioWorkspace workspace, string filePath, out ProjectId projectId)
        {
            if (workspace is VisualStudioWorkspaceImpl)
            {
                var impl = workspace as VisualStudioWorkspaceImpl;
                if (impl.ProjectTracker.TryGetProjectByBinPath(filePath, out var project))
                {
                    projectId = project.Id;
                    return true;
                }
                else
                {
                    projectId = default(ProjectId);
                    return false;
                }
            }
            projectId = default(ProjectId);
            return false;
        }

        [Obsolete("This is a compatibility shim for F#; please do not use it.")]
        public static ProjectId GetOrCreateProjectIdForPath(this VisualStudioWorkspace workspace, string filePath, string projectDisplayName)
        {
            if (workspace is VisualStudioWorkspaceImpl)
            {
                var impl = workspace as VisualStudioWorkspaceImpl;
                return impl.ProjectTracker.GetOrCreateProjectIdForPath(filePath, projectDisplayName);
            }
            return default(ProjectId);
        }

        [Obsolete("This is a compatibility shim for F#; please do not use it.")]
        public static string GetProjectFilePath(this VisualStudioWorkspace workspace, ProjectId projectId)
        {
            if (workspace is VisualStudioWorkspaceImpl)
            {
                var impl = workspace as VisualStudioWorkspaceImpl;
                var project = impl.ProjectTracker.GetProject(projectId);
                if (project != null)
                {
                    return project.ProjectFilePath;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }
    }
}
