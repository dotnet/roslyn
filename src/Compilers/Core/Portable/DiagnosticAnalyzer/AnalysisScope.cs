// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Scope for analyzer execution.
    /// This scope could either be the entire compilation for all analyzers (command line build) or
    /// could be scoped to a specific tree/span and/or a subset of analyzers (CompilationWithAnalyzers).
    /// </summary>
    internal class AnalysisScope
    {
        public SyntaxTree FilterTreeOpt { get; }
        public TextSpan? FilterSpanOpt { get; }

        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

        /// <summary>
        /// Syntax trees on which we need to perform syntax analysis.
        /// </summary>
        public IEnumerable<SyntaxTree> SyntaxTrees { get; }

        public bool ConcurrentAnalysis { get; }

        /// <summary>
        /// True if we need to categorize diagnostics into local and non-local diagnostics and track the analyzer reporting each diagnostic.
        /// </summary>
        public bool CategorizeDiagnostics { get; }

        /// <summary>
        /// True if we need to perform only syntax analysis for a single tree.
        /// </summary>
        public bool IsSyntaxOnlyTreeAnalysis { get; }

        /// <summary>
        /// True if we need to perform analysis for a single tree.
        /// </summary>
        public bool IsTreeAnalysis => FilterTreeOpt != null;

        public AnalysisScope(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, bool concurrentAnalysis, bool categorizeDiagnostics)
            : this(compilation.SyntaxTrees, analyzers, filterTreeOpt: null, filterSpanOpt: null, isSyntaxOnlyTreeAnalysis: false, concurrentAnalysis: concurrentAnalysis, categorizeDiagnostics: categorizeDiagnostics)
        {
        }

        public AnalysisScope(ImmutableArray<DiagnosticAnalyzer> analyzers, SyntaxTree filterTree, TextSpan? filterSpan, bool syntaxAnalysis, bool concurrentAnalysis, bool categorizeDiagnostics)
            : this(SpecializedCollections.SingletonEnumerable(filterTree), analyzers, filterTree, filterSpan, syntaxAnalysis, concurrentAnalysis, categorizeDiagnostics)
        {
            Debug.Assert(filterTree != null);
        }

        private AnalysisScope(IEnumerable<SyntaxTree> trees, ImmutableArray<DiagnosticAnalyzer> analyzers, SyntaxTree filterTreeOpt, TextSpan? filterSpanOpt, bool isSyntaxOnlyTreeAnalysis, bool concurrentAnalysis, bool categorizeDiagnostics)
        {
            SyntaxTrees = trees;
            Analyzers = analyzers;
            FilterTreeOpt = filterTreeOpt;
            FilterSpanOpt = filterSpanOpt;
            IsSyntaxOnlyTreeAnalysis = isSyntaxOnlyTreeAnalysis;
            ConcurrentAnalysis = concurrentAnalysis;
            CategorizeDiagnostics = categorizeDiagnostics;
        }

        public AnalysisScope WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            return new AnalysisScope(SyntaxTrees, analyzers, FilterTreeOpt, FilterSpanOpt, IsSyntaxOnlyTreeAnalysis, ConcurrentAnalysis, CategorizeDiagnostics);
        }

        public static bool ShouldSkipSymbolAnalysis(SymbolDeclaredCompilationEvent symbolEvent)
        {
            // Skip symbol actions for implicitly declared symbols and non-source symbols.
            return symbolEvent.Symbol.IsImplicitlyDeclared || symbolEvent.DeclaringSyntaxReferences.All(s => s.SyntaxTree == null);
        }

        public static bool ShouldSkipDeclarationAnalysis(ISymbol symbol)
        {
            // Skip syntax actions for implicitly declared symbols, except for implicitly declared global namespace symbols.
            return symbol.IsImplicitlyDeclared &&
                !((symbol.Kind == SymbolKind.Namespace && ((INamespaceSymbol)symbol).IsGlobalNamespace));
        }

        public bool ShouldAnalyze(SyntaxTree tree)
        {
            return FilterTreeOpt == null || FilterTreeOpt == tree;
        }

        public bool ShouldAnalyze(ISymbol symbol)
        {
            if (FilterTreeOpt == null)
            {
                return true;
            }

            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree != null && FilterTreeOpt == location.SourceTree && ShouldInclude(location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ShouldAnalyze(SyntaxNode node)
        {
            if (FilterTreeOpt == null)
            {
                return true;
            }

            return ShouldInclude(node.FullSpan);
        }

        private bool ShouldInclude(TextSpan filterSpan)
        {
            return !FilterSpanOpt.HasValue || FilterSpanOpt.Value.IntersectsWith(filterSpan);
        }

        public bool ContainsSpan(TextSpan filterSpan)
        {
            return !FilterSpanOpt.HasValue || FilterSpanOpt.Value.Contains(filterSpan);
        }

        public bool ShouldInclude(Diagnostic diagnostic)
        {
            if (FilterTreeOpt == null)
            {
                return true;
            }

            if (!diagnostic.Location.IsInSource || diagnostic.Location.SourceTree != FilterTreeOpt)
            {
                return false;
            }

            return ShouldInclude(diagnostic.Location.SourceSpan);
        }
    }
}
