// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(QuickInfoProviderNames.DiagnosticAnalyzer, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = QuickInfoProviderNames.Syntactic)]
    internal class CSharpDiagnosticAnalyzerQuickInfoProvider : CommonQuickInfoProvider
    {
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpDiagnosticAnalyzerQuickInfoProvider(IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }
        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var pragmaWarningDiagnosticId = token.Parent switch
            {
                PragmaWarningDirectiveTriviaSyntax pragmaWarning => pragmaWarning.ErrorCodes.FirstOrDefault() as IdentifierNameSyntax,
                IdentifierNameSyntax { Parent: PragmaWarningDirectiveTriviaSyntax _ } identifier => identifier,
                _ => null,
            };

            if (pragmaWarningDiagnosticId != null)
            {
                // First look in the analyzer diagnostics of the document. By doing so we get detailed information about the actual
                // diagnostic message and the code that is affected by the diagnostic id.
                var diagnostics = await _diagnosticAnalyzerService.GetDiagnosticsAsync(document.Project.Solution, document.Project.Id, document.Id,
                    includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                var supressedDiagnostic = diagnostics.FirstOrDefault(d => d.Id == pragmaWarningDiagnosticId.Identifier.ValueText);
                if (supressedDiagnostic != null)
                {
                    var relatedSpans = supressedDiagnostic.HasTextSpan
                        ? new[] { supressedDiagnostic.GetTextSpan() }
                        : Array.Empty<TextSpan>();
                    return CreateQuickInfo(pragmaWarningDiagnosticId,
                        supressedDiagnostic.Message ?? supressedDiagnostic.Title ?? supressedDiagnostic.Id,
                        relatedSpans);
                }
                else
                {
                    // The diagnostic id from the pragma could not be found in the analyzer diagnostics of the document
                    // We now try to find it in the SupportedDiagnostics of all referenced analyzers.
                    var analyzerReferences = document.Project.AnalyzerReferences.Union(document.Project.Solution.AnalyzerReferences);
                    var supportedDiagnostics = from r in analyzerReferences
                                               from a in r.GetAnalyzersForAllLanguages()
                                               from d in a.SupportedDiagnostics
                                               select d;
                    var diagnosticDescriptor = supportedDiagnostics.FirstOrDefault(d => d.Id == pragmaWarningDiagnosticId.Identifier.ValueText);
                    if (diagnosticDescriptor != null)
                    {
                        return CreateQuickInfo(pragmaWarningDiagnosticId, diagnosticDescriptor.Title.ToString());
                    }
                }
            }

            return null;
        }

        private static QuickInfoItem CreateQuickInfo(IdentifierNameSyntax pragmaWarningDiagnosticId, string description,
            params TextSpan[] relatedSpans)
        {
            return QuickInfoItem.Create(pragmaWarningDiagnosticId.Span, sections: new[]
                {
                    QuickInfoSection.Create(QuickInfoSectionKinds.Description, new[]
                    {
                        new TaggedText(TextTags.Text, description)
                    }.ToImmutableArray())
                }.ToImmutableArray(), relatedSpans: relatedSpans.ToImmutableArray());
        }
    }
}
