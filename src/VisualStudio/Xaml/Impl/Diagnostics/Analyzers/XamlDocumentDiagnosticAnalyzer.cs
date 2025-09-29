// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.VisualStudio.LanguageServices.Xaml;

namespace Microsoft.CodeAnalysis.Xaml.Diagnostics.Analyzers;

[DiagnosticAnalyzer(StringConstants.XamlLanguageName)]
internal sealed class XamlDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => XamlProjectService.AnalyzerService?.SupportedDiagnostics ?? [];

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
        => XamlProjectService.AnalyzerService is null || textDocument is not Document document
            ? []
            : await XamlProjectService.AnalyzerService.AnalyzeSyntaxAsync(document, cancellationToken).ConfigureAwait(false);

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
        => XamlProjectService.AnalyzerService is null || textDocument is not Document document
            ? []
            : await XamlProjectService.AnalyzerService.AnalyzeSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
}
