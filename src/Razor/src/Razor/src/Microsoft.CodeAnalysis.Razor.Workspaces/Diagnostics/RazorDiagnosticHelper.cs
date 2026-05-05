// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Diagnostics;

internal static class RazorDiagnosticHelper
{
    public static async Task<LspDiagnostic[]?> GetRazorDiagnosticsAsync(IDocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
    {
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = codeDocument.Source.Text;
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        var diagnostics = csharpDocument.Diagnostics;

        if (diagnostics.Length == 0)
        {
            return null;
        }

        return Convert(diagnostics, sourceText, documentSnapshot);
    }

    public static VSDiagnosticProjectInformation[] GetProjectInformation(IDocumentSnapshot? documentSnapshot)
    {
        if (documentSnapshot is null)
        {
            return [];
        }

        return [new VSDiagnosticProjectInformation()
                {
                    Context = null,
                    ProjectIdentifier = documentSnapshot.Project.IntermediateOutputPath,
                    ProjectName = documentSnapshot.Project.DisplayName
                }];
    }

    internal static LspDiagnostic[] Convert(ImmutableArray<RazorDiagnostic> diagnostics, SourceText sourceText, IDocumentSnapshot documentSnapshot)
    {
        var convertedDiagnostics = new LspDiagnostic[diagnostics.Length];

        var i = 0;
        foreach (var diagnostic in diagnostics)
        {
            convertedDiagnostics[i++] = ConvertToVSDiagnostic(diagnostic, sourceText, documentSnapshot);
        }

        return convertedDiagnostics;
    }

    // Internal for testing
    internal static LspDiagnosticSeverity ConvertSeverity(RazorDiagnosticSeverity severity)
    {
        return severity switch
        {
            RazorDiagnosticSeverity.Error => LspDiagnosticSeverity.Error,
            RazorDiagnosticSeverity.Warning => LspDiagnosticSeverity.Warning,
            _ => LspDiagnosticSeverity.Information,
        };
    }

    // Internal for testing
    internal static LspRange? ConvertSpanToRange(SourceSpan sourceSpan, SourceText sourceText)
    {
        if (sourceSpan == SourceSpan.Undefined)
        {
            return null;
        }

        var spanStartIndex = Math.Min(sourceSpan.AbsoluteIndex, sourceText.Length);
        var spanEndIndex = Math.Min(sourceSpan.AbsoluteIndex + sourceSpan.Length, sourceText.Length);

        return sourceText.GetRange(spanStartIndex, spanEndIndex);
    }

    // Internal for testing
    internal static VSDiagnostic ConvertToVSDiagnostic(RazorDiagnostic razorDiagnostic, SourceText sourceText, IDocumentSnapshot? documentSnapshot)
    {
        var diagnostic = new VSDiagnostic()
        {
            Message = razorDiagnostic.GetMessage(CultureInfo.InvariantCulture),
            Code = razorDiagnostic.Id,
            Source = LanguageServerConstants.RazorDiagnosticSource,
            Severity = ConvertSeverity(razorDiagnostic.Severity),
            // This is annotated as not null, but we have tests that validate the behaviour when
            // we pass in null here
            Range = ConvertSpanToRange(razorDiagnostic.Span, sourceText)!,
            Projects = GetProjectInformation(documentSnapshot)
        };

        return diagnostic;
    }
}
