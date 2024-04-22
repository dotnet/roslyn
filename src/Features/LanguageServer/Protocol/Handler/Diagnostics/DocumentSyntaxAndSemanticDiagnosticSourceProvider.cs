// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider : AbstractDocumentDiagnosticSourceProvider
{
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
    private readonly DiagnosticKind _kind;

    public AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(IDiagnosticAnalyzerService diagnosticAnalyzerService,
        DiagnosticKind kind, string sourceName) : base(sourceName)
    {
        _diagnosticAnalyzerService = diagnosticAnalyzerService;
        _kind = kind;
    }

    public override ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (GetOpenDocument(context) is not TextDocument textDocument)
            return new([]);

        var source = new DocumentDiagnosticSource(_diagnosticAnalyzerService, _kind, textDocument);
        return new([source]);
    }

    [ExportDiagnosticSourceProvider, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentCompilerSyntaxDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.CompilerSyntax, PullDiagnosticCategories.DocumentCompilerSyntax)
    {
    }

    [ExportDiagnosticSourceProvider, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentCompilerSemanticDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.CompilerSemantic, PullDiagnosticCategories.DocumentCompilerSemantic)
    {
    }

    [ExportDiagnosticSourceProvider, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentAnalyzerSyntaxDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.AnalyzerSyntax, PullDiagnosticCategories.DocumentAnalyzerSyntax)
    {
    }

    [ExportDiagnosticSourceProvider, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentAnalyzerSemanticDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.AnalyzerSemantic, PullDiagnosticCategories.DocumentAnalyzerSemantic)
    {
    }
}
