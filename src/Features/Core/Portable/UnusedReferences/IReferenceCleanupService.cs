// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal interface IReferenceCleanupService : IWorkspaceService
    {
        /// <summary>
        /// Return the set of direct Project and Package references for the given project. This
        /// is used to get the initial state of the TreatAsUsed attribute for each reference.
        /// </summary>
        Task<ImmutableArray<ReferenceInfo>> GetProjectReferencesAsync(
            string projectPath,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates the project’s reference by removing or marking references as
        /// TreatAsUsed in the project file.
        /// </summary>
        /// <returns>True, if the reference was updated.</returns>
        [Obsolete($"Use {nameof(GetUpdateReferenceOperationAsync)} instead.")]
        Task<bool> TryUpdateReferenceAsync(
            string projectPath,
            ReferenceUpdate referenceUpdate,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets an operation that can update the project’s references by removing or marking references as
        /// TreatAsUsed in the project file.
        /// </summary>
        Task<IUpdateReferenceOperation> GetUpdateReferenceOperationAsync(
            string projectPath,
            ReferenceUpdate referenceUpdate,
            CancellationToken cancellationToken);
    }
}
