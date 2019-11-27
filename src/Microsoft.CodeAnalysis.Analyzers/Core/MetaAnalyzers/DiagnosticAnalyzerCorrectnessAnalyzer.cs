// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities.Extensions;
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
        internal static readonly string CodeBlockAnalysisContextFullName = typeof(CodeBlockAnalysisContext).FullName;
        internal static readonly string OperationBlockStartAnalysisContextFullName = typeof(OperationBlockStartAnalysisContext).FullName;
        internal static readonly string OperationBlockAnalysisContextFullName = typeof(OperationBlockAnalysisContext).FullName;
        internal static readonly string OperationAnalysisContextFullName = typeof(OperationAnalysisContext).FullName;
        internal static readonly string SymbolKindFullName = typeof(SymbolKind).FullName;

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
        internal const string SupportedDiagnosticsName = nameof(SupportedDiagnostics);
        internal const string TLanguageKindEnumName = @"TLanguageKindEnum";

#pragma warning disable RS1026 // Enable concurrent execution
        public override void Initialize(AnalysisContext context)
#pragma warning restore RS1026 // Enable concurrent execution
        {
            // CONSIDER: Make all the subtypes thread safe to enable concurrent analyzer callbacks.
            //context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? diagnosticAnalyzer = compilationContext.Compilation.GetOrCreateTypeByMetadataName(DiagnosticAnalyzerTypeFullName);
                INamedTypeSymbol? diagnosticAnalyzerAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(DiagnosticAnalyzerAttributeFullName);

                if (diagnosticAnalyzer == null || diagnosticAnalyzerAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing Microsoft.CodeAnalysis which defines DiagnosticAnalyzer.
                    return;
                }

                DiagnosticAnalyzerSymbolAnalyzer? analyzer = GetDiagnosticAnalyzerSymbolAnalyzer(compilationContext, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
                if (analyzer != null)
                {
                    compilationContext.RegisterSymbolAction(c => analyzer.AnalyzeSymbol(c), SymbolKind.NamedType);
                }
            });
        }

        protected abstract DiagnosticAnalyzerSymbolAnalyzer? GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute);
    }
}
