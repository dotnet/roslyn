// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
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
            private readonly ObjectPool<DeclarationAnalysisDataBuilder> _declarationAnalysisDataBuilderPool;

            public CompilationData(Compilation comp)
            {
                _semanticModelsMap = new Dictionary<SyntaxTree, SemanticModel>();
                this.SuppressMessageAttributeState = new SuppressMessageAttributeState(comp);
                _declarationAnalysisDataMap = new Dictionary<SyntaxReference, DeclarationAnalysisData>();
                _declarationAnalysisDataBuilderPool = new ObjectPool<DeclarationAnalysisDataBuilder>(() => new DeclarationAnalysisDataBuilder());
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
                Func<Func<DeclarationAnalysisDataBuilder>, DeclarationAnalysisDataBuilder> computeDeclarationAnalysisData,
                bool cacheAnalysisData)
            {
                DeclarationAnalysisDataBuilder dataBuilder = null;
                try
                {
                    if (!cacheAnalysisData)
                    {
                        dataBuilder = computeDeclarationAnalysisData(_declarationAnalysisDataBuilderPool.Allocate);
                        return DeclarationAnalysisData.CreateFrom(dataBuilder);
                    }

                    DeclarationAnalysisData data;
                    lock (_declarationAnalysisDataMap)
                    {
                        if (_declarationAnalysisDataMap.TryGetValue(declaration, out data))
                        {
                            return data;
                        }
                    }

                    dataBuilder = computeDeclarationAnalysisData(_declarationAnalysisDataBuilderPool.Allocate);

                    lock (_declarationAnalysisDataMap)
                    {
                        if (!_declarationAnalysisDataMap.TryGetValue(declaration, out data))
                        {
                            data = DeclarationAnalysisData.CreateFrom(dataBuilder);
                            _declarationAnalysisDataMap.Add(declaration, data);
                        }
                    }

                    return data;
                }
                finally
                {
                    if (dataBuilder != null)
                    {
                        dataBuilder.Free();
                        _declarationAnalysisDataBuilderPool.Free(dataBuilder);
                    }
                }
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
                }
            }
        }
    }
}
