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
        /// Synchronize data to OOP proactively so that the corresponding solution is often already available when
        /// features call into it.
        /// </summary>
        ValueTask SynchronizePrimaryWorkspaceAsync(Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken);
        ValueTask SynchronizeTextAsync(DocumentId documentId, Checksum baseTextChecksum, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken);
    }
}
