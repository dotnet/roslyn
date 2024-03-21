// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class Extensions
{
    public static async Task<ImmutableArray<DiagnosticData>> GetCachedCopilotDiagnosticsAsync(this TextDocument document, TextSpan span, CancellationToken cancellationToken)
    {
        var diagnostics = await document.GetCachedCopilotDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        if (diagnostics.IsEmpty)
            return [];

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return diagnostics.WhereAsArray(diagnostic => span.IntersectsWith(diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text)));
    }

    public static async Task<ImmutableArray<DiagnosticData>> GetCachedCopilotDiagnosticsAsync(this TextDocument document, CancellationToken cancellationToken)
    {
        if (document is not Document sourceDocument)
            return ImmutableArray<DiagnosticData>.Empty;

        var copilotCodeAnalysisService = sourceDocument.GetLanguageService<ICopilotCodeAnalysisService>();
        if (copilotCodeAnalysisService is null)
            return ImmutableArray<DiagnosticData>.Empty;

        var promptTitles = await copilotCodeAnalysisService.GetAvailablePromptTitlesAsync(sourceDocument, cancellationToken).ConfigureAwait(false);
        var copilotDiagnostics = await copilotCodeAnalysisService.GetCachedDocumentDiagnosticsAsync(sourceDocument, promptTitles, cancellationToken).ConfigureAwait(false);
        return copilotDiagnostics.SelectAsArray(d => DiagnosticData.Create(d, sourceDocument));
    }
}
