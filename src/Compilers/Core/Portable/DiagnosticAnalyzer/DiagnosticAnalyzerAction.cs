// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class AnalyzerAction
    {
        internal DiagnosticAnalyzer Analyzer { get; }

        internal AnalyzerAction(DiagnosticAnalyzer analyzer)
        {
            Analyzer = analyzer;
        }
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
        public Action<SymbolAnalysisContext> Action { get; }

        public SymbolEndAnalyzerAction(Action<SymbolAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class SyntaxNodeAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        public Action<SyntaxNodeAnalysisContext> Action { get; }
        public ImmutableArray<TLanguageKindEnum> Kinds { get; }

        public SyntaxNodeAnalyzerAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
            Kinds = kinds;
        }
    }

    internal sealed class OperationBlockStartAnalyzerAction : AnalyzerAction
    {
        public Action<OperationBlockStartAnalysisContext> Action { get; }

        public OperationBlockStartAnalyzerAction(Action<OperationBlockStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class OperationBlockAnalyzerAction : AnalyzerAction
    {
        public Action<OperationBlockAnalysisContext> Action { get; }

        public OperationBlockAnalyzerAction(Action<OperationBlockAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class OperationAnalyzerAction : AnalyzerAction
    {
        public Action<OperationAnalysisContext> Action { get; }
        public ImmutableArray<OperationKind> Kinds { get; }

        public OperationAnalyzerAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> kinds, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
            Kinds = kinds;
        }
    }

    internal sealed class CompilationStartAnalyzerAction : AnalyzerAction
    {
        public Action<CompilationStartAnalysisContext> Action { get; }

        public CompilationStartAnalyzerAction(Action<CompilationStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class CompilationAnalyzerAction : AnalyzerAction
    {
        public Action<CompilationAnalysisContext> Action { get; }

        public CompilationAnalyzerAction(Action<CompilationAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class SemanticModelStartAnalyzerAction : AnalyzerAction
    {
        public Action<SemanticModelStartAnalysisContext> Action { get; }

        public SemanticModelStartAnalyzerAction(Action<SemanticModelStartAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class SemanticModelAnalyzerAction : AnalyzerAction
    {
        public Action<SemanticModelAnalysisContext> Action { get; }

        public SemanticModelAnalyzerAction(Action<SemanticModelAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class SyntaxTreeAnalyzerAction : AnalyzerAction
    {
        public Action<SyntaxTreeAnalysisContext> Action { get; }

        public SyntaxTreeAnalyzerAction(Action<SyntaxTreeAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class AdditionalFileAnalyzerAction : AnalyzerAction
    {
        public Action<AdditionalFileAnalysisContext> Action { get; }

        public AdditionalFileAnalyzerAction(Action<AdditionalFileAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class CodeBlockStartAnalyzerAction<TLanguageKindEnum> : AnalyzerAction where TLanguageKindEnum : struct
    {
        public Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> Action { get; }

        public CodeBlockStartAnalyzerAction(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }

    internal sealed class CodeBlockAnalyzerAction : AnalyzerAction
    {
        public Action<CodeBlockAnalysisContext> Action { get; }

        public CodeBlockAnalyzerAction(Action<CodeBlockAnalysisContext> action, DiagnosticAnalyzer analyzer)
            : base(analyzer)
        {
            Action = action;
        }
    }
}
