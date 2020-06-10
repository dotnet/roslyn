// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public SourceOrNonSourceFile? FilterFileOpt { get; }
        public TextSpan? FilterSpanOpt { get; }

        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

        /// <summary>
        /// Syntax trees on which we need to perform syntax analysis.
        /// </summary>
        public IEnumerable<SyntaxTree> SyntaxTrees { get; }

        /// <summary>
        /// Non-source files on which we need to perform analysis.
        /// </summary>
        public IEnumerable<AdditionalText> NonSourceFiles { get; }

        public bool ConcurrentAnalysis { get; }

        /// <summary>
        /// True if we need to categorize diagnostics into local and non-local diagnostics and track the analyzer reporting each diagnostic.
        /// </summary>
        public bool CategorizeDiagnostics { get; }

        /// <summary>
        /// True if we need to perform only syntax analysis for a single tree or non-source file.
        /// </summary>
        public bool IsSyntaxOnlyTreeAnalysis { get; }

        /// <summary>
        /// True if we need to perform analysis for a single tree or non-source file.
        /// </summary>
        public bool IsTreeAnalysis => FilterFileOpt != null;

        public AnalysisScope(Compilation compilation, AnalyzerOptions? analyzerOptions, ImmutableArray<DiagnosticAnalyzer> analyzers, bool concurrentAnalysis, bool categorizeDiagnostics)
            : this(compilation.SyntaxTrees, analyzerOptions?.AdditionalFiles ?? ImmutableArray<AdditionalText>.Empty,
                  analyzers, filterTreeOpt: null, filterSpanOpt: null, isSyntaxOnlyTreeAnalysis: false, concurrentAnalysis, categorizeDiagnostics)
        {
        }

        public AnalysisScope(ImmutableArray<DiagnosticAnalyzer> analyzers, SourceOrNonSourceFile filterTree, TextSpan? filterSpan, bool syntaxAnalysis, bool concurrentAnalysis, bool categorizeDiagnostics)
            : this(filterTree?.SourceTree != null ? SpecializedCollections.SingletonEnumerable(filterTree.SourceTree) : SpecializedCollections.EmptyEnumerable<SyntaxTree>(),
                   filterTree?.NonSourceFile != null ? SpecializedCollections.SingletonEnumerable(filterTree.NonSourceFile) : SpecializedCollections.EmptyEnumerable<AdditionalText>(),
                   analyzers, filterTree, filterSpan, syntaxAnalysis, concurrentAnalysis, categorizeDiagnostics)
        {
            Debug.Assert(filterTree != null);
        }

        private AnalysisScope(IEnumerable<SyntaxTree> trees, IEnumerable<AdditionalText> nonSourceFiles, ImmutableArray<DiagnosticAnalyzer> analyzers, SourceOrNonSourceFile? filterTreeOpt, TextSpan? filterSpanOpt, bool isSyntaxOnlyTreeAnalysis, bool concurrentAnalysis, bool categorizeDiagnostics)
        {
            SyntaxTrees = trees;
            NonSourceFiles = nonSourceFiles;
            Analyzers = analyzers;
            FilterFileOpt = filterTreeOpt;
            FilterSpanOpt = filterSpanOpt;
            IsSyntaxOnlyTreeAnalysis = isSyntaxOnlyTreeAnalysis;
            ConcurrentAnalysis = concurrentAnalysis;
            CategorizeDiagnostics = categorizeDiagnostics;
        }

        public AnalysisScope WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            return new AnalysisScope(SyntaxTrees, NonSourceFiles, analyzers, FilterFileOpt, FilterSpanOpt, IsSyntaxOnlyTreeAnalysis, ConcurrentAnalysis, CategorizeDiagnostics);
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
            return FilterFileOpt == null || FilterFileOpt.SourceTree == tree;
        }

        public bool ShouldAnalyze(AdditionalText file)
        {
            return FilterFileOpt == null || FilterFileOpt.NonSourceFile == file;
        }

        public bool ShouldAnalyze(ISymbol symbol)
        {
            if (FilterFileOpt == null)
            {
                return true;
            }

            if (FilterFileOpt.SourceTree == null)
            {
                return false;
            }

            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree != null && FilterFileOpt.SourceTree == location.SourceTree && ShouldInclude(location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ShouldAnalyze(SyntaxNode node)
        {
            if (FilterFileOpt == null)
            {
                return true;
            }

            if (FilterFileOpt.SourceTree == null)
            {
                return false;
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
            if (FilterFileOpt == null)
            {
                return true;
            }

            if (diagnostic.Location.IsInSource)
            {
                if (FilterFileOpt?.SourceTree == null ||
                    diagnostic.Location.SourceTree != FilterFileOpt.SourceTree)
                {
                    return false;
                }
            }
            else if (diagnostic.Location is ExternalFileLocation externalFileLocation)
            {
                if (FilterFileOpt?.NonSourceFile == null ||
                    !PathUtilities.Comparer.Equals(externalFileLocation.FilePath, FilterFileOpt.NonSourceFile.Path))
                {
                    return false;
                }
            }

            return ShouldInclude(diagnostic.Location.SourceSpan);
        }
    }
}
