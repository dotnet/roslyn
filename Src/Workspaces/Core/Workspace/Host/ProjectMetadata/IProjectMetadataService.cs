using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A workspace service that finds existing metadata for projects.
    /// </summary>
    internal interface IProjectMetadataService : IWorkspaceService
    {
        /// <summary>
        /// Gets existing metadata that corresponds to the project.
        /// Returns null if no metadata is matching or available.
        /// </summary>
        Task<MetadataReference> GetExistingMetadataAsync(Solution solution, ProjectReference projectReference, CancellationToken cancellationToken);
    }
}