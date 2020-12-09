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
    internal interface IUnusedReferencesService : IWorkspaceService
    {
        /// <summary>
        /// Determines unused references for this project.
        /// </summary>
        Task<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(
            Project project,
            ImmutableArray<ReferenceInfo> references,
            string targetFrameworkMoniker,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates references by removing, adding, or marking references as TreatAsUsed
        /// for this project.
        /// </summary>
        /// <returns>Updated project</returns>
        Task<Project> UpdateReferencesAsync(
            Project project,
            ImmutableArray<ReferenceUpdate> referenceUpdates,
            CancellationToken cancellationToken);
    }
}
