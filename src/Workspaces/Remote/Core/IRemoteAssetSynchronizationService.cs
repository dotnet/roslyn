// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteAssetSynchronizationService
    {
        /// <summary>
        /// Synchronize data to OOP proactively without anyone asking for it to make most of operation
        /// faster
        /// </summary>
        ValueTask SynchronizePrimaryWorkspaceAsync(PinnedSolutionInfo solutionInfo, Checksum checksum, int workspaceVersion, CancellationToken cancellationToken);
        ValueTask SynchronizeTextAsync(DocumentId documentId, Checksum baseTextChecksum, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken);
    }
}
