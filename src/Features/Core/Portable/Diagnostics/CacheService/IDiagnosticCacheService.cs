// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IDiagnosticCacheService : IWorkspaceService
    {
        event EventHandler<DiagnosticsUpdatedArgs> CachedDiagnosticsUpdated;

        Task LoadCachedDiagnosticsAsync(Document document, CancellationToken cancellationToken);

        bool TryGetLoadedCachedDiagnostics(DocumentId documentId, [NotNullWhen(true)] out object? id, out ImmutableArray<DiagnosticData> cachedDiagnostics);
    }

    internal interface ILoadedFromCache : ISupportLiveUpdate
    { }
}
