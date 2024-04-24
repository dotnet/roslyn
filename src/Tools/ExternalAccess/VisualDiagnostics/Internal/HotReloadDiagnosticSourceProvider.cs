// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

internal abstract class HotReloadDiagnosticSourceProvider(IHotReloadDiagnosticManager diagnosticManager, bool isDocument) : IDiagnosticSourceProvider
{
    string IDiagnosticSourceProvider.Name => "HotReloadDiagnostics";
    bool IDiagnosticSourceProvider.IsDocument => isDocument;

    async ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var hotReloadContext = new HotReloadRequestContext(context);
        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var sources);
        foreach (var provider in diagnosticManager.Providers)
        {
            if (provider.IsDocument == isDocument)
            {
                var hotReloadSources = await provider.CreateDiagnosticSourcesAsync(hotReloadContext, cancellationToken).ConfigureAwait(false);
                sources.AddRange(hotReloadSources.Select(s => new HotReloadDiagnosticSource(s)));
            }
        }

        var result = sources.ToImmutableAndClear();
        return DiagnosticSourceManager.AggregateSourcesIfNeeded(result, isDocument);
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class DocumentHotReloadDiagnosticSourceProvider([Import] IHotReloadDiagnosticManager diagnosticManager)
        : HotReloadDiagnosticSourceProvider(diagnosticManager, isDocument: true)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class WorkspaceHotReloadDiagnosticSourceProvider([Import] IHotReloadDiagnosticManager diagnosticManager)
        : HotReloadDiagnosticSourceProvider(diagnosticManager, isDocument: false)
    {
    }
}
