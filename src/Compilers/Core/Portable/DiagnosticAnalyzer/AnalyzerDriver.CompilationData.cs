// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        internal class CompilationData
        {
            /// <summary>
            /// Cached semantic model for the compilation trees.
            /// PERF: This cache enables us to re-use semantic model's bound node cache across analyzer execution and diagnostic queries.
            /// </summary>
            private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModelsMap;

            private readonly Dictionary<SyntaxReference, DeclarationAnalysisData> _declarationAnalysisDataMap;
            private readonly ObjectPool<DeclarationAnalysisData> _declarationAnalysisDataPool;

            public CompilationData(Compilation comp)
            {
                _semanticModelsMap = new Dictionary<SyntaxTree, SemanticModel>();
                this.SuppressMessageAttributeState = new SuppressMessageAttributeState(comp);
                _declarationAnalysisDataMap = new Dictionary<SyntaxReference, DeclarationAnalysisData>();
                _declarationAnalysisDataPool = new ObjectPool<DeclarationAnalysisData>(() => new DeclarationAnalysisData());
            }

            public SuppressMessageAttributeState SuppressMessageAttributeState { get; }

            public SemanticModel GetOrCreateCachedSemanticModel(SyntaxTree tree, Compilation compilation, CancellationToken cancellationToken)
            {
                SemanticModel model;
                lock (_semanticModelsMap)
                {
                    if (_semanticModelsMap.TryGetValue(tree, out model) && model.Compilation == compilation)
                    {
                        return model;
                    }
                }

                model = compilation.GetSemanticModel(tree);

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

            internal DeclarationAnalysisData GetOrComputeDeclarationAnalysisData(
                SyntaxReference declaration,
                Func<Func<DeclarationAnalysisData>, DeclarationAnalysisData> computeDeclarationAnalysisData,
                bool cacheAnalysisData)
            {
                if (!cacheAnalysisData)
                {
                    return computeDeclarationAnalysisData(_declarationAnalysisDataPool.Allocate);
                }

                DeclarationAnalysisData data;
                lock (_declarationAnalysisDataMap)
                {
                    if (_declarationAnalysisDataMap.TryGetValue(declaration, out data))
                    {
                        return data;
                    }
                }

                data = computeDeclarationAnalysisData(_declarationAnalysisDataPool.Allocate);

                lock (_declarationAnalysisDataMap)
                {
                    _declarationAnalysisDataMap[declaration] = data;
                }

                return data;
            }

            internal void ClearDeclarationAnalysisData(SyntaxReference declaration)
            {
                DeclarationAnalysisData declarationData;
                lock (_declarationAnalysisDataMap)
                {
                    if (!_declarationAnalysisDataMap.TryGetValue(declaration, out declarationData))
                    {
                        return;
                    }

                    _declarationAnalysisDataMap.Remove(declaration);

                    declarationData.Free();
                    _declarationAnalysisDataPool.Free(declarationData);
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
    }
}