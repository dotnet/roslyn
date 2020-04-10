﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class AnalyzerAction
    {
        private readonly DiagnosticAnalyzer _analyzer;

        internal AnalyzerAction(DiagnosticAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        internal DiagnosticAnalyzer Analyzer { get { return _analyzer; } }
    }

    internal sealed class SymbolAnalyzerAction : AnalyzerAction
    {
        public Action<SymbolAnalysisContext> Action { get; }
        public ImmutableArray<SymbolKind> Kinds { get; }

        public SymbolAnalyzerAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
            Kinds = kinds;
        }
    }

    internal sealed class SymbolStartAnalyzerAction : AnalyzerAction
    {
        public Action<SymbolStartAnalysisContext> Action { get; }
        public SymbolKind Kind { get; }

        public SymbolStartAnalyzerAction(Action<SymbolStartAnalysisContext> action, SymbolKind kind, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
            Kind = kind;
        }
    }

    internal sealed class SymbolEndAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SymbolAnalysisContext> _action;

        public SymbolEndAnalyzerAction(Action<SymbolAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<SymbolAnalysisContext> Action { get { return _action; } }
    }

    internal sealed class SyntaxNodeAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        private readonly Action<SyntaxNodeAnalysisContext> _action;
        private readonly ImmutableArray<TLanguageKindEnum> _kinds;

        public SyntaxNodeAnalyzerAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
            _kinds = kinds;
        }

        public Action<SyntaxNodeAnalysisContext> Action { get { return _action; } }
        public ImmutableArray<TLanguageKindEnum> Kinds { get { return _kinds; } }
    }

    internal sealed class OperationBlockStartAnalyzerAction : AnalyzerAction
    {
        private readonly Action<OperationBlockStartAnalysisContext> _action;

        public OperationBlockStartAnalyzerAction(Action<OperationBlockStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<OperationBlockStartAnalysisContext> Action => _action;
    }

    internal sealed class OperationBlockAnalyzerAction : AnalyzerAction
    {
        private readonly Action<OperationBlockAnalysisContext> _action;

        public OperationBlockAnalyzerAction(Action<OperationBlockAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<OperationBlockAnalysisContext> Action => _action;
    }

    internal sealed class OperationAnalyzerAction : AnalyzerAction
    {
        private readonly Action<OperationAnalysisContext> _action;
        private readonly ImmutableArray<OperationKind> _kinds;

        public OperationAnalyzerAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
            _kinds = kinds;
        }

        public Action<OperationAnalysisContext> Action { get { return _action; } }
        public ImmutableArray<OperationKind> Kinds { get { return _kinds; } }
    }

    internal sealed class CompilationStartAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CompilationStartAnalysisContext> _action;

        public CompilationStartAnalyzerAction(Action<CompilationStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<CompilationStartAnalysisContext> Action { get { return _action; } }
    }

    internal sealed class CompilationAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CompilationAnalysisContext> _action;

        public CompilationAnalyzerAction(Action<CompilationAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<CompilationAnalysisContext> Action { get { return _action; } }
    }

    internal sealed class SemanticModelAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SemanticModelAnalysisContext> _action;

        public SemanticModelAnalyzerAction(Action<SemanticModelAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<SemanticModelAnalysisContext> Action { get { return _action; } }
    }

    internal sealed class SyntaxTreeAnalyzerAction : AnalyzerAction
    {
        private readonly Action<SyntaxTreeAnalysisContext> _action;

        public SyntaxTreeAnalyzerAction(Action<SyntaxTreeAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<SyntaxTreeAnalysisContext> Action { get { return _action; } }
    }

    internal sealed class CodeBlockStartAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        private readonly Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> _action;

        public CodeBlockStartAnalyzerAction(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> Action { get { return _action; } }
    }

    internal sealed class CodeBlockAnalyzerAction : AnalyzerAction
    {
        private readonly Action<CodeBlockAnalysisContext> _action;

        public CodeBlockAnalyzerAction(Action<CodeBlockAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            _action = action;
        }

        public Action<CodeBlockAnalysisContext> Action { get { return _action; } }
    }
}
