using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A simple project metadata service that looks for metadata in the build output directory and returns if it the version matches.
    /// </summary>
#if MEF 
    [ExportWorkspaceService(typeof(IProjectMetadataService), WorkspaceKind.Any)]
#endif
    internal class ProjectMetadataService : IProjectMetadataService
    {
        public async Task<MetadataReference> GetExistingMetadataAsync(Solution solution, ProjectReference projectReference, CancellationToken cancellationToken)
        {
            var project = await solution.GetProjectAsync(projectReference.ProjectId, cancellationToken).ConfigureAwait(false);

            var filePath = project.OutputFilePath;

            if (!string.IsNullOrEmpty(filePath))
            {
                // do we have an already built assembly?
                if (File.Exists(filePath))
                {
                    // if the assembly on disk is newer than the project (and its dependencies), then it is good to use
                    var version = VersionStamp.Create(File.GetLastWriteTimeUtc(filePath));
                    var projectVersion = await solution.GetDependentVersionAsync(project.Id, cancellationToken).ConfigureAwait(false);

                    if (version.IsNewerThan(projectVersion))
                    {
                        // get metadata reference for this metadata file
                        var metadataProvider = WorkspaceService.GetService<IMetadataReferenceProviderService>(solution.Workspace).GetProvider();
                        return metadataProvider.GetReference(filePath, new MetadataReferenceProperties(alias: projectReference.Alias, embedInteropTypes: projectReference.EmbedInteropTypes));
                    }
                }
            }

            return null;
        }
    }
}