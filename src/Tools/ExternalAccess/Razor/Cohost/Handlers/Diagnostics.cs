// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class Diagnostics
{
    public static async Task<ImmutableArray<LSP.Diagnostic>> GetDocumentDiagnosticsAsync(Document document, bool supportsVisualStudioExtensions, CancellationToken cancellationToken)
    {
        var globalOptionsService = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var diagnosticAnalyzerService = document.Project.Solution.Services.ExportProvider.GetService<IDiagnosticAnalyzerService>();

        var diagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken).ConfigureAwait(false);

        var project = document.Project;
        // isLiveSource means build might override a diagnostics, but this method is only used by tooling, so builds aren't relevant
        const bool IsLiveSource = false;
        // Potential duplicate is only set for workspace diagnostics
        const bool PotentialDuplicate = false;

        var result = ArrayBuilder<LSP.Diagnostic>.GetInstance(capacity: diagnostics.Length);
        foreach (var diagnostic in diagnostics)
        {
            result.AddRange(ProtocolConversions.ConvertDiagnostic(diagnostic, supportsVisualStudioExtensions, project, IsLiveSource, PotentialDuplicate, globalOptionsService));
        }

        return result.ToImmutableAndFree();
    }
}
