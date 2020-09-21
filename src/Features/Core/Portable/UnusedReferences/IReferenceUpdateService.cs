// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal interface IReferenceUpdateService : IWorkspaceService
    {
        /// <summary>
        /// Gets the current selected TargetFramework for the specified project.
        /// </summary>
        string GetTargetFramworkMoniker(ProjectId projectId);

        /// <summary>
        /// For the given project, returns the full path to the project.assets.json file
        /// typically generated in the intermediate output path by a NuGet restore.
        /// </summary>
        Task<string> GetProjectAssetsFilePathAsync(
            string projectPath,
            string targetFramework,
            CancellationToken cancellationToken);

        /// <summary>
        /// Return the set of direct references for the given project. This is used to
        /// get the initial state of the TreatAsUsed attribute for each reference.
        /// </summary>
        Task<ImmutableArray<Reference>> GetProjectReferencesAsync(
            string projectPath,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates the project’s references by adding, removing or marking references as
        /// TreatAsUsed in the project file.
        /// </summary>
        Task<bool> UpdateReferencesAsync(
            string projectPath,
            string targetFramework,
            ImmutableArray<ReferenceUpdate> referenceUpdates,
            CancellationToken cancellationToken);
    }
}
