// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DocumentHotReloadDiagnosticSourceProvider(IHotReloadDiagnosticManager hotReloadErrorService)
    : AbstractHotReloadDiagnosticSourceProvider
    , IDiagnosticSourceProvider
{
    bool IDiagnosticSourceProvider.IsDocument => true;

    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (context.GetTrackedDocument<TextDocument>() is { } textDocument)
        {
            if (hotReloadErrorService.Errors.FirstOrDefault(e => e.DocumentId == textDocument.Id) is { } documentErrors)
                return new([new HotReloadDiagnosticSource(textDocument, documentErrors.Errors)]);
        }

        return new([]);
    }
}
