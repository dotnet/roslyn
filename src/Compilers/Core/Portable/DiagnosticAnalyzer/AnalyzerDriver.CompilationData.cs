// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        protected static readonly ConditionalWeakTable<Compilation, CompilationData> s_compilationDataCache = new ConditionalWeakTable<Compilation, CompilationData>();

        internal class CompilationData
        {
            /// <summary>
            /// Cached semantic model for the compilation trees.
            /// PERF: This cache enables us to re-use semantic model's bound node cache across analyzer execution and diagnostic queries.
            /// </summary>
            private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModelsMap;

            /// <summary>
            /// Cached syntax references for a symbol for the lifetime of symbol declared event.
            /// PERF: This cache reduces allocations for computing declaring syntax references for a symbol.
            /// </summary>
            private readonly Dictionary<ISymbol, ImmutableArray<SyntaxReference>> _symbolDeclarationsMap;

            public CompilationData(Compilation comp)
            {
                _semanticModelsMap = new Dictionary<SyntaxTree, SemanticModel>();
                _symbolDeclarationsMap = new Dictionary<ISymbol, ImmutableArray<SyntaxReference>>();
                this.SuppressMessageAttributeState = new SuppressMessageAttributeState(comp);
                this.DeclarationAnalysisDataMap = new Dictionary<SyntaxReference, DeclarationAnalysisData>();
            }

            public SuppressMessageAttributeState SuppressMessageAttributeState { get; }
            public Dictionary<SyntaxReference, DeclarationAnalysisData> DeclarationAnalysisDataMap { get; }

            public SemanticModel GetOrCreateCachedSemanticModel(SyntaxTree tree, Compilation compilation, CancellationToken cancellationToken)
            {
                SemanticModel model;
                lock (_semanticModelsMap)
                {
                    if (_semanticModelsMap.TryGetValue(tree, out model))
                    {
                        return model;
                    }
                }


                model = compilation.GetSemanticModel(tree);

                // Invoke GetDiagnostics to populate the compilation's CompilationEvent queue.
                model.GetDiagnostics(null, cancellationToken);

                lock (_semanticModelsMap)
                {
                    _semanticModelsMap[tree] = model;
                }

                return model;
            }

            public bool RemoveCachedSemanticModel(SyntaxTree tree)
            {
                lock (_semanticModelsMap)
                {
                    return _semanticModelsMap.Remove(tree);
                }
            }

            public bool TryGetCachedDeclaringReferences(ISymbol symbol, out ImmutableArray<SyntaxReference> declaringReferences)
            {
                lock (_symbolDeclarationsMap)
                {
                    if (!_symbolDeclarationsMap.TryGetValue(symbol, out declaringReferences))
                    {
                        declaringReferences = default(ImmutableArray<SyntaxReference>);
                        return false;
                    }

                    return true;
                }
            }

            public void CacheDeclaringReferences(ISymbol symbol, ImmutableArray<SyntaxReference> declaringReferences)
            {
                lock (_symbolDeclarationsMap)
                {
                    _symbolDeclarationsMap[symbol] = declaringReferences;
                }
            }

            public bool RemoveCachedDeclaringReferences(ISymbol symbol)
            {
                lock (_symbolDeclarationsMap)
                {
                    return _symbolDeclarationsMap.Remove(symbol);
                }
            }
        }

        internal class DeclarationAnalysisData
        {
            /// <summary>
            /// GetSyntax() for the given SyntaxReference.
            /// </summary>
            public SyntaxNode DeclaringReferenceSyntax { get; set; }

            /// <summary>
            /// Topmost declaration node for analysis.
            /// </summary>
            public SyntaxNode TopmostNodeForAnalysis { get; set; }

            /// <summary>
            /// All member declarations within the declaration.
            /// </summary>
            public List<DeclarationInfo> DeclarationsInNode { get; }

            /// <summary>
            /// All descendant nodes for syntax node actions.
            /// </summary>
            public List<SyntaxNode> DescendantNodesToAnalyze { get; }

            /// <summary>
            /// Flag indicating if this is a partial analysis.
            /// </summary>
            public bool IsPartialAnalysis { get; set; }

            public DeclarationAnalysisData()
            {
                this.DeclarationsInNode = new List<DeclarationInfo>();
                this.DescendantNodesToAnalyze = new List<SyntaxNode>();
            }

            public void Free()
            {
                DeclaringReferenceSyntax = null;
                TopmostNodeForAnalysis = null;
                DeclarationsInNode.Clear();
                DescendantNodesToAnalyze.Clear();
                IsPartialAnalysis = false;
            }
        }

        internal static CompilationData GetOrCreateCachedCompilationData(Compilation compilation)
        {
            return s_compilationDataCache.GetValue(compilation, c => new CompilationData(c));
        }

        internal static bool RemoveCachedCompilationData(Compilation compilation)
        {
            return s_compilationDataCache.Remove(compilation);
        }

        public static SemanticModel GetOrCreateCachedSemanticModel(SyntaxTree tree, Compilation compilation, CancellationToken cancellationToken)
        {
            var compilationData = GetOrCreateCachedCompilationData(compilation);
            return compilationData.GetOrCreateCachedSemanticModel(tree, compilation, cancellationToken);
        }

        public static bool RemoveCachedSemanticModel(SyntaxTree tree, Compilation compilation)
        {
            CompilationData compilationData;
            return s_compilationDataCache.TryGetValue(compilation, out compilationData) &&
                compilationData.RemoveCachedSemanticModel(tree);
        }

        public static bool TryGetCachedDeclaringReferences(ISymbol symbol, Compilation compilation, out ImmutableArray<SyntaxReference> declaringReferences)
        {
            var compilationData = GetOrCreateCachedCompilationData(compilation);
            return compilationData.TryGetCachedDeclaringReferences(symbol, out declaringReferences);
        }

        public static void CacheDeclaringReferences(ISymbol symbol, Compilation compilation, ImmutableArray<SyntaxReference> declaringReferences)
        {
            var compilationData = GetOrCreateCachedCompilationData(compilation);
            compilationData.CacheDeclaringReferences(symbol, declaringReferences);
        }

        public static bool RemoveCachedDeclaringReferences(ISymbol symbol, Compilation compilation)
        {
            CompilationData compilationData;
            return s_compilationDataCache.TryGetValue(compilation, out compilationData) &&
                compilationData.RemoveCachedDeclaringReferences(symbol);
        }
    }
}