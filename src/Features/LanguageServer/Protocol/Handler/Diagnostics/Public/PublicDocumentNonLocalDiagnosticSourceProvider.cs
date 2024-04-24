// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

[Export(typeof(IDiagnosticSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PublicDocumentNonLocalDiagnosticSourceProvider(
    [Import] IGlobalOptionService globalOptions,
    [Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
    : AbstractDocumentDiagnosticSourceProvider<TextDocument>(NonLocal)
{
    public const string NonLocal = "NonLocal_B69807DB-28FB-4846-884A-1152E54C8B62";

    protected override IDiagnosticSource? CreateDiagnosticSource(TextDocument textDocument)
    {
        // Non-local document diagnostics are reported only when full solution analysis is enabled for analyzer execution.
        if (globalOptions.GetBackgroundAnalysisScope(textDocument.Project.Language) != BackgroundAnalysisScope.FullSolution)
        {
            return null;
        }

        return new NonLocalDocumentDiagnosticSource(textDocument, diagnosticAnalyzerService, ShouldIncludeAnalyzer);

        // NOTE: Compiler does not report any non-local diagnostics, so we bail out for compiler analyzer.
        bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer) => !analyzer.IsCompilerAnalyzer();
    }
}
