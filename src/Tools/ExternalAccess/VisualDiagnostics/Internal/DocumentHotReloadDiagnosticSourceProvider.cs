// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DocumentHotReloadDiagnosticSourceProvider(IHotReloadDiagnosticManager hotReloadDiagnosticManager)
    : AbstractHotReloadDiagnosticSourceProvider
    , IDiagnosticSourceProvider
{
    bool IDiagnosticSourceProvider.IsDocument => true;

    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (context.GetTrackedDocument<TextDocument>() is { } textDocument)
        {
            List<IDiagnosticSource> sources = new();
            foreach (var hotReloadSource in hotReloadDiagnosticManager.Sources)
            {
                sources.Add(new HotReloadDiagnosticSource(textDocument, hotReloadSource));
            }

            return new(sources.ToImmutableArray());
        }

        return new([]);
    }
}
