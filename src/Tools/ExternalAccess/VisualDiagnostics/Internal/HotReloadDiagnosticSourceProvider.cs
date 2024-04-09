// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

// THIS IS WRONG. Need to follow EdinAndContinue 
[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class HotReloadDiagnosticSourceProvider(IDiagnosticsRefresher diagnosticsRefresher)
    : IDiagnosticSourceProvider
    , IHotReloadDiagnosticService
{
    private const string SourceName = "HotReloadDiagnostic";
    private static readonly ImmutableArray<string> sourceNames = [SourceName];

    private readonly ConcurrentDictionary<ProjectId, HotReloadDiagnosticSource> projectDiagnostics = new();

    bool IDiagnosticSourceProvider.IsDocument => false;
    ImmutableArray<string> IDiagnosticSourceProvider.SourceNames => sourceNames;

    void IHotReloadDiagnosticService.UpdateDiagnostics(IEnumerable<Diagnostic> diagnostics, string sourceName)
    {
        // TODO: store diagnostics in projectDiagnostics
        //foreach (var diagnostic in diagnostics)
        //{
        //}
        diagnosticsRefresher.RequestWorkspaceRefresh();
    }

    ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, CancellationToken cancellationToken)
    {
        if (sourceName == SourceName)
        {
            return new(projectDiagnostics.Values.ToImmutableArray<IDiagnosticSource>());
        }

        return new([]);
    }
}
