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
                var diagnostics = await _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(document.Project.Solution.Workspace, document.Project.Id, document.Id,
                    includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                var findDiagnostic = diagnostics.FirstOrDefault(d => d.Id == pragmaWarningDiagnosticId.Identifier.ValueText);
                if (findDiagnostic != null)
                {
                    return QuickInfoItem.Create(pragmaWarningDiagnosticId.Span, sections: new[]
                        {
                            QuickInfoSection.Create(QuickInfoSectionKinds.Description, new[]
                            {
                                new TaggedText(TextTags.Text, findDiagnostic.Message)
                            }.ToImmutableArray())
                        }.ToImmutableArray(), relatedSpans: new[] { findDiagnostic.GetTextSpan() }.ToImmutableArray());
                }
            }

            return null;
        }
    }
}
