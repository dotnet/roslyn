// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IRemoteDiagnosticCacheService
    {
        ValueTask<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(SerializableDocumentKey documentKey, Checksum checksum, CancellationToken cancellation);

        ValueTask CacheDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics, CancellationToken cancellationToken);
    }
}
