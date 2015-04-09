// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract partial class DiagnosticAnalyzerCorrectnessAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly string DiagnosticAnalyzerTypeFullName = typeof(DiagnosticAnalyzer).FullName;
        internal static readonly string DiagnosticAnalyzerAttributeFullName = typeof(DiagnosticAnalyzerAttribute).FullName;
        internal static readonly string DiagnosticFullName = typeof(Diagnostic).FullName;
        internal static readonly string DiagnosticDescriptorFullName = typeof(DiagnosticDescriptor).FullName;
        internal static readonly string LocalizableStringFullName = typeof(LocalizableString).FullName;

        internal static readonly string AnalysisContextFullName = typeof(AnalysisContext).FullName;
        internal static readonly string CompilationStartAnalysisContextFullName = typeof(CompilationStartAnalysisContext).FullName;
        internal static readonly string CompilationEndAnalysisContextFullName = typeof(CompilationAnalysisContext).FullName;
        internal static readonly string SemanticModelAnalysisContextFullName = typeof(SemanticModelAnalysisContext).FullName;
        internal static readonly string SymbolAnalysisContextFullName = typeof(SymbolAnalysisContext).FullName;
        internal static readonly string SyntaxNodeAnalysisContextFullName = typeof(SyntaxNodeAnalysisContext).FullName;
        internal static readonly string SyntaxTreeAnalysisContextFullName = typeof(SyntaxTreeAnalysisContext).FullName;
        internal static readonly string CodeBlockStartAnalysisContextFullName = typeof(CodeBlockStartAnalysisContext<>).FullName;
        internal static readonly string CodeBlockEndAnalysisContextFullName = typeof(CodeBlockAnalysisContext).FullName;
        internal static readonly string SymbolKindFullName = typeof(SymbolKind).FullName;

        internal static readonly string RegisterSyntaxNodeActionName = nameof(AnalysisContext.RegisterSyntaxNodeAction);
        internal static readonly string RegisterSymbolActionName = nameof(AnalysisContext.RegisterSymbolAction);
        internal static readonly string RegisterCodeBlockStartActionName = nameof(AnalysisContext.RegisterCodeBlockStartAction);
        internal static readonly string RegisterCodeBlockActionName = nameof(AnalysisContext.RegisterCodeBlockAction);
        internal static readonly string RegisterCompilationStartActionName = nameof(AnalysisContext.RegisterCompilationStartAction);
        internal static readonly string RegisterCompilationActionName = nameof(AnalysisContext.RegisterCompilationAction);
        internal static readonly string ReportDiagnosticName = nameof(CompilationAnalysisContext.ReportDiagnostic);
        internal static readonly string SupportedDiagnosticsName = nameof(DiagnosticAnalyzer.SupportedDiagnostics);
        internal static readonly string TLanguageKindEnumName = @"TLanguageKindEnum";

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var diagnosticAnalyzer = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerTypeFullName);
                var diagnosticAnalyzerAttribute = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerAttributeFullName);

                if (diagnosticAnalyzer == null || diagnosticAnalyzerAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing Microsoft.CodeAnalysis which defines DiagnosticAnalyzer.
                    return;
                }

                var analyzer = GetCompilationAnalyzer(compilationContext.Compilation, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
                if (analyzer == null)
                {
                    return;
                }

                compilationContext.RegisterSymbolAction(c => analyzer.AnalyzeSymbol(c), SymbolKind.NamedType);
            });
        }

        protected abstract CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute);
    }
}
