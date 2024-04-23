// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class WorkspaceHotReloadDiagnosticSourceProvider(IHotReloadDiagnosticManager hotReloadErrorService)
    : AbstractHotReloadDiagnosticSourceProvider
    , IDiagnosticSourceProvider
{
    bool IDiagnosticSourceProvider.IsDocument => false;

    async ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (context.Solution is not Solution solution)
        {
            return [];
        }

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var builder);
        foreach (var hotReloadSource in hotReloadErrorService.Sources)
        {
            var docIds = await hotReloadSource.GetDocumentIdsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var docId in docIds)
            {
                if (solution.GetDocument(docId) is { } document && !context.IsTracking(document.GetURI()))
                {
                    builder.Add(new HotReloadDiagnosticSource(document, hotReloadSource));
                }
            }
        }

        var result = builder.ToImmutableAndClear();
        return result;
    }
}
