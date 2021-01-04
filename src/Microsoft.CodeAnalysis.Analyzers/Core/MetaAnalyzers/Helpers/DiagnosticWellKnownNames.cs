// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Helpers
{
    internal static class DiagnosticWellKnownNames
    {
        internal const string RegisterSyntaxNodeActionName = nameof(AnalysisContext.RegisterSyntaxNodeAction);
        internal const string RegisterSymbolActionName = nameof(AnalysisContext.RegisterSymbolAction);
        internal const string RegisterCodeBlockStartActionName = nameof(AnalysisContext.RegisterCodeBlockStartAction);
        internal const string RegisterCodeBlockEndActionName = nameof(CodeBlockStartAnalysisContext<int>.RegisterCodeBlockEndAction);
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
