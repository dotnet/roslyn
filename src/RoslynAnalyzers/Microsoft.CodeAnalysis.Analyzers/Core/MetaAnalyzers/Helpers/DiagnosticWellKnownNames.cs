// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Helpers
{
    internal static class DiagnosticWellKnownNames
    {
        internal const string RegisterSyntaxNodeActionName = nameof(AnalysisContext.RegisterSyntaxNodeAction);
        internal const string RegisterSymbolActionName = nameof(AnalysisContext.RegisterSymbolAction);
        internal const string RegisterCodeBlockStartActionName = nameof(AnalysisContext.RegisterCodeBlockStartAction);
        internal const string RegisterCodeBlockEndActionName = nameof(CodeBlockStartAnalysisContext<>.RegisterCodeBlockEndAction);
        internal const string RegisterCodeBlockActionName = nameof(AnalysisContext.RegisterCodeBlockAction);
        internal const string RegisterOperationBlockStartActionName = nameof(AnalysisContext.RegisterOperationBlockStartAction);
        internal const string RegisterOperationBlockEndActionName = nameof(OperationBlockStartAnalysisContext.RegisterOperationBlockEndAction);
        internal const string RegisterOperationBlockActionName = nameof(AnalysisContext.RegisterOperationBlockAction);
        internal const string RegisterOperationActionName = nameof(AnalysisContext.RegisterOperationAction);
        internal const string RegisterCompilationStartActionName = nameof(AnalysisContext.RegisterCompilationStartAction);
        internal const string RegisterCompilationEndActionName = nameof(CompilationStartAnalysisContext.RegisterCompilationEndAction);
        internal const string RegisterCompilationActionName = nameof(AnalysisContext.RegisterCompilationAction);
        internal const string ReportDiagnosticName = nameof(CompilationAnalysisContext.ReportDiagnostic);
        internal const string SupportedDiagnosticsName = nameof(DiagnosticAnalyzer.SupportedDiagnostics);
        internal const string TLanguageKindEnumName = @"TLanguageKindEnum";
    }
}
