// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal interface IUnusedReferencesService
    {
        /// <summary>
        /// Determines unused references from this compilation.
        /// </summary>
        Task<ImmutableArray<UnusedReference>> GetUnusedReferencesAsync(
            Project project,
            Compilation compilation,
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
