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
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(
    IDiagnosticAnalyzerService diagnosticAnalyzerService, DiagnosticKind kind, string sourceName)
    : IDiagnosticSourceProvider
{
    public bool IsDocument => true;
    public string Name => sourceName;

    public bool IsEnabled(ClientCapabilities clientCapabilities) => true;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        if (context.GetTrackedDocument<TextDocument>() is { } document)
        {
            return new([new DocumentDiagnosticSource(diagnosticAnalyzerService, kind, document)]);
        }

        return new([]);
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class DocumentCompilerSyntaxDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.CompilerSyntax, PullDiagnosticCategories.DocumentCompilerSyntax)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class DocumentCompilerSemanticDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(
            diagnosticAnalyzerService, DiagnosticKind.CompilerSemantic, PullDiagnosticCategories.DocumentCompilerSemantic)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class DocumentAnalyzerSyntaxDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.AnalyzerSyntax, PullDiagnosticCategories.DocumentAnalyzerSyntax)
    {
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class DocumentAnalyzerSemanticDiagnosticSourceProvider([Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
        : AbstractDocumentSyntaxAndSemanticDiagnosticSourceProvider(diagnosticAnalyzerService,
            DiagnosticKind.AnalyzerSemantic, PullDiagnosticCategories.DocumentAnalyzerSemantic)
    {
    }
}
