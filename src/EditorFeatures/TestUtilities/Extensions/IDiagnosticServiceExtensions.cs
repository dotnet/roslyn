// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

internal static class IDiagnosticServiceExtensions
{
    public static async Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(
        this IDiagnosticAnalyzerService service, Project project, CancellationToken cancellationToken)
    {
        var documentDiagnostics = await service.GetDiagnosticsForIdsAsync(project, documentId: null, diagnosticIds: null, shouldIncludeAnalyzer: null, includeLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
        var projectDiagnostics = await service.GetProjectDiagnosticsForIdsAsync(project, diagnosticIds: null, shouldIncludeAnalyzer: null, cancellationToken).ConfigureAwait(false);
        ImmutableArray<DiagnosticData> diagnostics = [.. documentDiagnostics, .. projectDiagnostics];

        return diagnostics.WhereAsArray(d => d.Severity != DiagnosticSeverity.Hidden);
    }
}
