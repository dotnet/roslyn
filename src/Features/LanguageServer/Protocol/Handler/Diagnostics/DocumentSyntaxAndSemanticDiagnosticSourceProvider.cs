// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider : AbstractDocumentDiagnosticSourceProvider<TextDocument>
{
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
    private readonly DiagnosticKind _kind;

    public AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(IDiagnosticAnalyzerService diagnosticAnalyzerService,
        DiagnosticKind kind, string sourceName) : base(sourceName)
    {
        _diagnosticAnalyzerService = diagnosticAnalyzerService;
        _kind = kind;
    }

    protected override IDiagnosticSource? CreateDiagnosticSource(TextDocument document)
        => new DocumentDiagnosticSource(_diagnosticAnalyzerService, _kind, document);

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
