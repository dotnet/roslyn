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
        var solutionServices = document.Project.Solution.Services;
        var globalOptionsService = solutionServices.ExportProvider.GetService<IGlobalOptionService>();
        var diagnosticAnalyzerService = solutionServices.GetRequiredService<IDiagnosticAnalyzerService>();

        var diagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(
            document, range: null, DiagnosticKind.All, cancellationToken).ConfigureAwait(false);

        var project = document.Project;
        // Potential duplicate is only set for workspace diagnostics
        const bool PotentialDuplicate = false;

        var result = ArrayBuilder<LSP.Diagnostic>.GetInstance(capacity: diagnostics.Length);
        foreach (var diagnostic in diagnostics)
        {
            if (!diagnostic.IsSuppressed)
                result.AddRange(ProtocolConversions.ConvertDiagnostic(diagnostic, supportsVisualStudioExtensions, project, PotentialDuplicate, globalOptionsService));
        }

        return result.ToImmutableAndFree();
    }
}
