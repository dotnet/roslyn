// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class AnalyzerAction
    {
        private readonly DiagnosticAnalyzer analyzer;

        internal AnalyzerAction(DiagnosticAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        internal DiagnosticAnalyzer Analyzer { get { return this.analyzer; } }
    }

    internal sealed class SymbolAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SymbolAnalysisContext> action;
        private readonly ImmutableArray<SymbolKind> kinds;

        public SymbolAnalyzerAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
            this.kinds = kinds;
        }

        public Action<SymbolAnalysisContext> Action { get { return this.action; } }
        public ImmutableArray<SymbolKind> Kinds { get { return this.kinds; } }
    }

    internal sealed class SyntaxNodeAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        private readonly Action<SyntaxNodeAnalysisContext> action;
        private readonly ImmutableArray<TLanguageKindEnum> kinds;

        public SyntaxNodeAnalyzerAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
            this.kinds = kinds;
        }

        public Action<SyntaxNodeAnalysisContext> Action { get { return this.action; } }
        public ImmutableArray<TLanguageKindEnum> Kinds { get { return this.kinds; } }
    }

    internal sealed class CompilationStartAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CompilationStartAnalysisContext> action;

        public CompilationStartAnalyzerAction(Action<CompilationStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CompilationStartAnalysisContext> Action { get { return this.action; } }
    }

    internal sealed class CompilationEndAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CompilationEndAnalysisContext> action;

        public CompilationEndAnalyzerAction(Action<CompilationEndAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CompilationEndAnalysisContext> Action { get { return this.action; } }
    }

    internal sealed class SemanticModelAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SemanticModelAnalysisContext> action;

        public SemanticModelAnalyzerAction(Action<SemanticModelAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<SemanticModelAnalysisContext> Action { get { return this.action; } }
    }

    internal sealed class SyntaxTreeAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SyntaxTreeAnalysisContext> action;

        public SyntaxTreeAnalyzerAction(Action<SyntaxTreeAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<SyntaxTreeAnalysisContext> Action { get { return this.action; } }
    }

    internal sealed class CodeBlockStartAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        private readonly Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action;

        public CodeBlockStartAnalyzerAction(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> Action { get { return this.action; } }
    }

    internal sealed class CodeBlockEndAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        private readonly Action<CodeBlockEndAnalysisContext> action;

        public CodeBlockEndAnalyzerAction(Action<CodeBlockEndAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CodeBlockEndAnalysisContext> Action { get { return this.action; } }
    }
}
