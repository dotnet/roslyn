// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics.Internal
{
    // ToDo: Figure out how to make these types internal.

    public abstract class AnalyzerAction
    {
        private readonly DiagnosticAnalyzer analyzer;

        public AnalyzerAction(DiagnosticAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public DiagnosticAnalyzer Analyzer { get { return this.analyzer; } }
    }

    public sealed class SymbolAnalyzerAction : AnalyzerAction
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

    public sealed class SyntaxNodeAnalyzerAction<TSyntaxKind> : AnalyzerAction
    {
        private readonly Action<SyntaxNodeAnalysisContext> action;
        private readonly ImmutableArray<TSyntaxKind> kinds;

        public SyntaxNodeAnalyzerAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TSyntaxKind> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
            this.kinds = kinds;
        }

        public Action<SyntaxNodeAnalysisContext> Action { get { return this.action; } }
        public ImmutableArray<TSyntaxKind> Kinds { get { return this.kinds; } }
    }

    public sealed class CompilationStartAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CompilationStartAnalysisContext> action;

        public CompilationStartAnalyzerAction(Action<CompilationStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CompilationStartAnalysisContext> Action { get { return this.action; } }
    }

    public sealed class CompilationEndAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CompilationEndAnalysisContext> action;

        public CompilationEndAnalyzerAction(Action<CompilationEndAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CompilationEndAnalysisContext> Action { get { return this.action; } }
    }

    public sealed class SemanticModelAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SemanticModelAnalysisContext> action;

        public SemanticModelAnalyzerAction(Action<SemanticModelAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<SemanticModelAnalysisContext> Action { get { return this.action; } }
    }

    public sealed class SyntaxTreeAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SyntaxTreeAnalysisContext> action;

        public SyntaxTreeAnalyzerAction(Action<SyntaxTreeAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<SyntaxTreeAnalysisContext> Action { get { return this.action; } }
    }

    public sealed class CodeBlockStartAnalyzerAction<TSyntaxKind> : AnalyzerAction
    {
        private readonly Action<CodeBlockStartAnalysisContext<TSyntaxKind>> action;

        public CodeBlockStartAnalyzerAction(Action<CodeBlockStartAnalysisContext<TSyntaxKind>> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            this.action = action;
        }

        public Action<CodeBlockStartAnalysisContext<TSyntaxKind>> Action { get { return this.action; } }
    }

    public sealed class CodeBlockEndAnalyzerAction<TSyntaxKind> : AnalyzerAction
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
