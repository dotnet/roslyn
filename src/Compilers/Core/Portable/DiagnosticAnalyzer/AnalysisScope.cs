﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        private readonly Lazy<ImmutableHashSet<DiagnosticAnalyzer>> _lazyAnalyzersSet;

        public SourceOrAdditionalFile? FilterFileOpt { get; }
        public TextSpan? FilterSpanOpt { get; }

        public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

        /// <summary>
        /// Syntax trees on which we need to perform syntax analysis.
        /// </summary>
        public IEnumerable<SyntaxTree> SyntaxTrees { get; }

        /// <summary>
        /// Non-source files on which we need to perform analysis.
        /// </summary>
        public IEnumerable<AdditionalText> AdditionalFiles { get; }

        public bool ConcurrentAnalysis { get; }

        /// <summary>
        /// True if we need to categorize diagnostics into local and non-local diagnostics and track the analyzer reporting each diagnostic.
        /// </summary>
        public bool CategorizeDiagnostics { get; }

        /// <summary>
        /// True if we need to perform only syntax analysis for a single source or additional file.
        /// </summary>
        public bool IsSyntacticSingleFileAnalysis { get; }

        /// <summary>
        /// True if we need to perform analysis for a single source or additional file.
        /// </summary>
        public bool IsSingleFileAnalysis => FilterFileOpt != null;

        /// <summary>
        /// Flag indicating if this is a partial analysis for the corresponding <see cref="CompilationWithAnalyzers"/>,
        /// i.e. <see cref="IsSingleFileAnalysis"/> is true and/or <see cref="Analyzers"/> is a subset of <see cref="CompilationWithAnalyzers.Analyzers"/>.
        /// </summary>
        public bool IsPartialAnalysis { get; }

        /// <summary>
        /// True if we are performing semantic analysis for a single source file with a single analyzer in scope,
        /// which is a <see cref="CompilerDiagnosticAnalyzer"/>.
        /// </summary>
        public bool IsSemanticSingleFileAnalysisForCompilerAnalyzer =>
            IsSingleFileAnalysis && !IsSyntacticSingleFileAnalysis && Analyzers is [CompilerDiagnosticAnalyzer];

        public AnalysisScope(Compilation compilation, AnalyzerOptions? analyzerOptions, ImmutableArray<DiagnosticAnalyzer> analyzers, bool hasAllAnalyzers, bool concurrentAnalysis, bool categorizeDiagnostics)
            : this(compilation.SyntaxTrees, analyzerOptions?.AdditionalFiles ?? ImmutableArray<AdditionalText>.Empty,
                   analyzers, isPartialAnalysis: !hasAllAnalyzers, filterFile: null, filterSpanOpt: null, isSyntacticSingleFileAnalysis: false, concurrentAnalysis: concurrentAnalysis, categorizeDiagnostics: categorizeDiagnostics)
        {
        }

        public AnalysisScope(ImmutableArray<DiagnosticAnalyzer> analyzers, SourceOrAdditionalFile filterFile, TextSpan? filterSpan, bool isSyntacticSingleFileAnalysis, bool concurrentAnalysis, bool categorizeDiagnostics)
            : this(filterFile.SourceTree != null ? SpecializedCollections.SingletonEnumerable(filterFile.SourceTree) : SpecializedCollections.EmptyEnumerable<SyntaxTree>(),
                   filterFile.AdditionalFile != null ? SpecializedCollections.SingletonEnumerable(filterFile.AdditionalFile) : SpecializedCollections.EmptyEnumerable<AdditionalText>(),
                   analyzers, isPartialAnalysis: true, filterFile, filterSpan, isSyntacticSingleFileAnalysis, concurrentAnalysis, categorizeDiagnostics)
        {
        }

        private AnalysisScope(IEnumerable<SyntaxTree> trees, IEnumerable<AdditionalText> additionalFiles, ImmutableArray<DiagnosticAnalyzer> analyzers, bool isPartialAnalysis, SourceOrAdditionalFile? filterFile, TextSpan? filterSpanOpt, bool isSyntacticSingleFileAnalysis, bool concurrentAnalysis, bool categorizeDiagnostics)
        {
            Debug.Assert(isPartialAnalysis || FilterFileOpt == null);
            Debug.Assert(isPartialAnalysis || FilterSpanOpt == null);
            Debug.Assert(isPartialAnalysis || !isSyntacticSingleFileAnalysis);

            if (filterSpanOpt.HasValue)
            {
                Debug.Assert(filterFile.HasValue);
                Debug.Assert(filterFile.GetValueOrDefault().SourceTree != null);

                // PERF: Clear out filter span if the span length is equal to the entire tree span, and the filter span starts at 0.
                //       We are basically analyzing the entire tree, and clearing out the filter span
                //       avoids span intersection checks for each symbol/node/operation in the tree
                //       to determine if it falls in the analysis scope.
                if (filterSpanOpt.GetValueOrDefault().Start == 0 && filterSpanOpt.GetValueOrDefault().Length == filterFile.GetValueOrDefault().SourceTree!.Length)
                {
                    filterSpanOpt = null;
                }
            }

            SyntaxTrees = trees;
            AdditionalFiles = additionalFiles;
            Analyzers = analyzers;
            IsPartialAnalysis = isPartialAnalysis;
            FilterFileOpt = filterFile;
            FilterSpanOpt = filterSpanOpt;
            IsSyntacticSingleFileAnalysis = isSyntacticSingleFileAnalysis;
            ConcurrentAnalysis = concurrentAnalysis;
            CategorizeDiagnostics = categorizeDiagnostics;

            _lazyAnalyzersSet = new Lazy<ImmutableHashSet<DiagnosticAnalyzer>>(CreateAnalyzersSet);
        }

        private ImmutableHashSet<DiagnosticAnalyzer> CreateAnalyzersSet() => Analyzers.ToImmutableHashSet();

        public bool Contains(DiagnosticAnalyzer analyzer)
        {
            if (!IsPartialAnalysis)
            {
                Debug.Assert(_lazyAnalyzersSet.Value.Contains(analyzer));
                return true;
            }

            return _lazyAnalyzersSet.Value.Contains(analyzer);
        }

        public AnalysisScope WithAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers, bool hasAllAnalyzers)
        {
            var isPartialAnalysis = IsSingleFileAnalysis || !hasAllAnalyzers;
            return new AnalysisScope(SyntaxTrees, AdditionalFiles, analyzers, isPartialAnalysis, FilterFileOpt, FilterSpanOpt, IsSyntacticSingleFileAnalysis, ConcurrentAnalysis, CategorizeDiagnostics);
        }

        public AnalysisScope WithFilterSpan(TextSpan? filterSpan)
            => new AnalysisScope(SyntaxTrees, AdditionalFiles, Analyzers, IsPartialAnalysis, FilterFileOpt, filterSpan, IsSyntacticSingleFileAnalysis, ConcurrentAnalysis, CategorizeDiagnostics);

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
            return !FilterFileOpt.HasValue || FilterFileOpt.GetValueOrDefault().SourceTree == tree;
        }

        public bool ShouldAnalyze(AdditionalText file)
        {
            return !FilterFileOpt.HasValue || FilterFileOpt.GetValueOrDefault().AdditionalFile == file;
        }

        public bool ShouldAnalyze(
            SymbolDeclaredCompilationEvent symbolEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopmostNodeForAnalysis,
            CancellationToken cancellationToken)
        {
            if (!FilterFileOpt.HasValue)
            {
                return true;
            }

            var filterTree = FilterFileOpt.GetValueOrDefault().SourceTree;
            if (filterTree == null)
            {
                return false;
            }

            foreach (var syntaxRef in symbolEvent.DeclaringSyntaxReferences)
            {
                if (syntaxRef.SyntaxTree == filterTree)
                {
                    var node = getTopmostNodeForAnalysis(symbolEvent.Symbol, syntaxRef, symbolEvent.Compilation, cancellationToken);
                    if (ShouldInclude(node.FullSpan))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ShouldAnalyze(SyntaxNode node)
        {
            if (!FilterFileOpt.HasValue)
            {
                return true;
            }

            if (FilterFileOpt.GetValueOrDefault().SourceTree == null)
            {
                return false;
            }

            return ShouldInclude(node.FullSpan);
        }

        public bool ShouldInclude(TextSpan filterSpan)
        {
            return !FilterSpanOpt.HasValue || FilterSpanOpt.GetValueOrDefault().IntersectsWith(filterSpan);
        }

        public bool ContainsSpan(TextSpan filterSpan)
        {
            return !FilterSpanOpt.HasValue || FilterSpanOpt.GetValueOrDefault().Contains(filterSpan);
        }

        public bool ShouldInclude(Diagnostic diagnostic)
        {
            if (!FilterFileOpt.HasValue)
            {
                return true;
            }

            var filterFile = FilterFileOpt.GetValueOrDefault();
            if (diagnostic.Location.IsInSource)
            {
                if (diagnostic.Location.SourceTree != filterFile.SourceTree)
                {
                    return false;
                }
            }
            else if (diagnostic.Location is ExternalFileLocation externalFileLocation)
            {
                if (filterFile.AdditionalFile == null ||
                    !PathUtilities.Comparer.Equals(externalFileLocation.GetLineSpan().Path, filterFile.AdditionalFile.Path))
                {
                    return false;
                }
            }

            return ShouldInclude(diagnostic.Location.SourceSpan);
        }
    }
}
