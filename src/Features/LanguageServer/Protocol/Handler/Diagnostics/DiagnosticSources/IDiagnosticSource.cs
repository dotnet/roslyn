// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Wrapper around a source for diagnostics (e.g. a <see cref="Project"/> or <see cref="Document"/>)
/// so that we can share per file diagnostic reporting code in <see cref="AbstractPullDiagnosticHandler{TDiagnosticsParams, TReport, TReturn}"/>
/// </summary>
internal interface IDiagnosticSource
{
    Project GetProject();
    ProjectOrDocumentId GetId();
    TextDocumentIdentifier? GetDocumentIdentifier();
    string ToDisplayString();

    /// <summary>
    /// True if this source produces diagnostics that are considered 'live' or not.  Live errors represent up to date
    /// information that should supersede other sources.  Non 'live' errors (aka "build errors") are recognized to
    /// potentially represent stale results from a point in the past when the computation occurred.  The only time
    /// Roslyn produces non-live errors through an explicit user gesture to "run code analysis". Because these represent
    /// errors from the past, we do want them to be superseded by a more recent live run, or a more recent build from
    /// another source.
    /// </summary>
    bool IsLiveSource();

    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        CancellationToken cancellationToken);
}
