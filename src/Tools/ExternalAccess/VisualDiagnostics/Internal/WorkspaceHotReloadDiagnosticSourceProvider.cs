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
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class WorkspaceHotReloadDiagnosticSourceProvider(IHotReloadDiagnosticManager hotReloadErrorService)
    : AbstractHotReloadDiagnosticSourceProvider
    , IDiagnosticSourceProvider
{
    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (context.Solution is not Solution solution)
        {
            return new([]);
        }

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var builder);
        foreach (var documentErrors in hotReloadErrorService.Errors)
        {
            TextDocument? document = solution.GetAdditionalDocument(documentErrors.DocumentId) ?? solution.GetDocument(documentErrors.DocumentId);
            if (document != null && !context.IsTracking(document.GetURI()))
            {
                builder.Add(new HotReloadDiagnosticSource(document, documentErrors.Errors));
            }
        }

        var result = builder.ToImmutableAndClear();
        return new(result);
    }
}
