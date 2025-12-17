// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics;

[Shared]
[ExportLanguageService(typeof(FSharpUnusedOpensDiagnosticAnalyzerService), LanguageNames.FSharp)]
internal class FSharpUnusedOpensDiagnosticAnalyzerService : ILanguageService
{
    private readonly IFSharpUnusedOpensDiagnosticAnalyzer _analyzer;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpUnusedOpensDiagnosticAnalyzerService(IFSharpUnusedOpensDiagnosticAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(DiagnosticDescriptor descriptor, Document document, CancellationToken cancellationToken)
    {
        return _analyzer.AnalyzeSemanticsAsync(descriptor, document, cancellationToken);
    }
}

[DiagnosticAnalyzer(LanguageNames.FSharp)]
internal class FSharpUnusedOpensDeclarationsDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
{
    private readonly DiagnosticDescriptor _descriptor =
        new(
                IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                ExternalAccessFSharpResources.RemoveUnusedOpens,
                ExternalAccessFSharpResources.UnusedOpens,
                DiagnosticCategory.Style, DiagnosticSeverity.Hidden, isEnabledByDefault: true, customTags: FSharpDiagnosticCustomTags.Unnecessary);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_descriptor];

    public override int Priority => 90; // Default = 50

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(TextDocument textDocument, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var analyzer = textDocument.Project.Services.GetService<FSharpUnusedOpensDiagnosticAnalyzerService>();
        return analyzer is null || textDocument is not Document document
            ? []
            : await analyzer.AnalyzeSemanticsAsync(_descriptor, document, cancellationToken).ConfigureAwait(false);
    }

    public DiagnosticAnalyzerCategory GetAnalyzerCategory()
    {
        return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}
