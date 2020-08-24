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
            var (pragmaWarning, errorCode) = token.Parent switch
            {
                PragmaWarningDirectiveTriviaSyntax directive when IsDisablePragma(directive)
                    => (directive, directive.ErrorCodes.FirstOrDefault() as IdentifierNameSyntax),
                IdentifierNameSyntax { Parent: PragmaWarningDirectiveTriviaSyntax directive } identifier when IsDisablePragma(directive)
                    => (directive, identifier),
                _ => default,
            };

            if (errorCode != null)
            {
                // First look in the analyzer diagnostics of the document. By doing so we get detailed information about the actual
                // diagnostic message and the code that is affected by the diagnostic id.
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root != null)
                {
                    var range = TextSpan.FromBounds(pragmaWarning.FullSpan.End, root.FullSpan.End);
                    var diagnostics = await _diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(document, range,
                        diagnosticIdOpt: errorCode.Identifier.ValueText,
                        includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var supressedDiagnostic = diagnostics.FirstOrDefault();
                    if (supressedDiagnostic != null)
                    {
                        var relatedSpans = supressedDiagnostic.HasTextSpan
                            ? new[] { supressedDiagnostic.GetTextSpan() }
                            : Array.Empty<TextSpan>();
                        return CreateQuickInfo(errorCode,
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
                        var diagnosticDescriptor = supportedDiagnostics.FirstOrDefault(d => d.Id == errorCode.Identifier.ValueText);
                        if (diagnosticDescriptor != null)
                        {
                            var description =
                                diagnosticDescriptor.Title.ToStringOrNull() ??
                                diagnosticDescriptor.Description.ToStringOrNull() ??
                                diagnosticDescriptor.MessageFormat.ToStringOrNull() ??
                                diagnosticDescriptor.Id;

                            return CreateQuickInfo(errorCode, description);
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsDisablePragma(PragmaWarningDirectiveTriviaSyntax directive)
            => directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);

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

    internal static class LocalizableStringExtensions
    {
        public static string? ToStringOrNull(this LocalizableString @this)
        {
            var result = @this.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }

            return result;
        }
    }
}
