// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal static class CohostDocumentPullDiagnosticsHelpers
{
    public static async Task<ImmutableArray<LspDiagnostic>> GetDocumentDiagnosticsAsync(Document document, bool supportsVisualStudioExtensions, CancellationToken cancellationToken)
    {
        var solutionServices = document.Project.Solution.Services;
        var globalOptionsService = solutionServices.ExportProvider.GetService<IGlobalOptionService>();
        var diagnosticAnalyzerService = solutionServices.GetRequiredService<IDiagnosticAnalyzerService>();

        var diagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(
            document, range: null, DiagnosticKind.All, cancellationToken).ConfigureAwait(false);

        var encDiagnostics = await EditAndContinueDiagnosticSource.GetDocumentDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);

        return ConvertDiagnostics(document, supportsVisualStudioExtensions, globalOptionsService, [.. diagnostics, .. encDiagnostics]);
    }

    internal static ImmutableArray<LspDiagnostic> ConvertDiagnostics(Document document, bool supportsVisualStudioExtensions, IGlobalOptionService globalOptionsService, ImmutableArray<DiagnosticData> diagnostics)
    {
        var project = document.Project;
        // Potential duplicate is only set for workspace diagnostics, which Razor doesn't support
        const bool PotentialDuplicate = false;

        using var result = new PooledArrayBuilder<LspDiagnostic>(diagnostics.Length);
        foreach (var diagnostic in diagnostics)
        {
            if (!diagnostic.IsSuppressed)
            {
                result.AddRange(ProtocolConversions.ConvertDiagnostic(diagnostic, supportsVisualStudioExtensions, project, PotentialDuplicate, globalOptionsService));
            }
        }

        return result.ToImmutableAndClear();
    }
}
