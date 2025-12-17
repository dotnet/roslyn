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
[ExportLanguageService(typeof(FSharpDocumentDiagnosticAnalyzerService), LanguageNames.FSharp)]
internal class FSharpDocumentDiagnosticAnalyzerService : ILanguageService
{
    private readonly IFSharpDocumentDiagnosticAnalyzer _analyzer;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpDocumentDiagnosticAnalyzerService(IFSharpDocumentDiagnosticAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
    {
        return _analyzer.AnalyzeSemanticsAsync(document, cancellationToken);
    }

    public Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
    {
        return _analyzer.AnalyzeSyntaxAsync(document, cancellationToken);
    }
}

[DiagnosticAnalyzer(LanguageNames.FSharp)]
internal class FSharpDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
{
    private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

    public FSharpDocumentDiagnosticAnalyzer()
    {
        _supportedDiagnostics = CreateSupportedDiagnostics();
    }

    public static ImmutableArray<DiagnosticDescriptor> CreateSupportedDiagnostics()
    {
        // We are constructing our own descriptors at run-time. Compiler service is already doing error formatting and localization.
        var dummyDescriptors = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
        for (var i = 0; i <= 10000; i++)
        {
            dummyDescriptors.Add(new DiagnosticDescriptor(String.Format("FS{0:D4}", i), String.Empty, String.Empty, String.Empty, DiagnosticSeverity.Error, true, null, null));
        }
        return dummyDescriptors.ToImmutable();
    }

    public bool IsHighPriority => false;

    public override int Priority => 10; // Default = 50

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(TextDocument textDocument, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var analyzer = textDocument.Project.Services.GetService<FSharpDocumentDiagnosticAnalyzerService>();
        return analyzer is null || textDocument is not Document document
            ? []
            : await analyzer.AnalyzeSemanticsAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(TextDocument textDocument, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var analyzer = textDocument.Project.Services.GetService<FSharpDocumentDiagnosticAnalyzerService>();
        return analyzer is null || textDocument is not Document document
            ? []
            : await analyzer.AnalyzeSyntaxAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public DiagnosticAnalyzerCategory GetAnalyzerCategory()
    {
        return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}
