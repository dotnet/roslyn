// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    // Interface to be implemented and MEF exported by Project System
    internal interface IReferenceUpdateService
    {
        /// <summary>
        /// For the given project, returns the full path to the project.assets.json file
        /// typically generated in the intermediate output path by a NuGet restore.
        /// </summary>
        Task<string> GetProjectAssetsFilePathAsync(
            string projectPath,
            string targetFramework,
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
