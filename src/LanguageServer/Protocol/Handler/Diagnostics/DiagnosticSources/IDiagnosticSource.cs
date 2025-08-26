// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        RequestContext context,
        CancellationToken cancellationToken);
}
