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
        if (context.TextDocument is null)
            return new([]);

        var source = new DocumentDiagnosticSource(_diagnosticAnalyzerService, _kind, context.TextDocument);
        return new([source]);
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentCompilerSyntaxDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.CompilerSyntax, PullDiagnosticCategories.DocumentCompilerSyntax)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentCompilerSemanticDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.CompilerSemantic, PullDiagnosticCategories.DocumentCompilerSemantic)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentAnalyzerSyntaxDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.AnalyzerSyntax, PullDiagnosticCategories.DocumentAnalyzerSyntax)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DocumentAnalyzerSemanticDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.AnalyzerSemantic, PullDiagnosticCategories.DocumentAnalyzerSemantic)
    {
    }
}
