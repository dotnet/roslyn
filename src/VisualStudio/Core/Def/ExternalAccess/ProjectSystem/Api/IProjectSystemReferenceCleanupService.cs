// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api
{
    // Interface to be implemented and MEF exported by Project System
    internal interface IProjectSystemReferenceCleanupService
    {
        /// <summary>
        /// Return the set of direct Project and Package References for the given project. This
        /// is used to get the initial state of the TreatAsUsed attribute for each reference.
        /// </summary>
        Task<ImmutableArray<ProjectSystemReferenceInfo>> GetProjectReferencesAsync(
            string projectPath,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates the project’s references by removing or marking references as
        /// TreatAsUsed in the project file.
        /// </summary>
        /// <returns>True, if the reference was updated.</returns>
        Task<bool> TryUpdateReferenceAsync(
            string projectPath,
            ProjectSystemReferenceUpdate referenceUpdate,
            CancellationToken cancellationToken);
    }
}
