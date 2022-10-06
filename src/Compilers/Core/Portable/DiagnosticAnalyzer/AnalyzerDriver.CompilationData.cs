// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        /// <summary>
        /// Stores <see cref="DeclarationAnalysisData"/> for symbols declared in the compilation.
        /// This allows us to avoid recomputing this data across analyzer execution for different analyzers
        /// on the same symbols. This cached compilation data is strongly held by the associated
        /// <see cref="CompilationWithAnalyzers"/> object.
        /// </summary>
        internal class CompilationData
        {
            private readonly Dictionary<(ISymbol symbol, int declarationIndex), DeclarationAnalysisData> _declarationAnalysisDataMap;

            public CompilationData(Compilation compilation)
            {
                Debug.Assert(compilation.SemanticModelProvider is CachingSemanticModelProvider);

                SemanticModelProvider = (CachingSemanticModelProvider)compilation.SemanticModelProvider;
                this.SuppressMessageAttributeState = new SuppressMessageAttributeState(compilation);
                _declarationAnalysisDataMap = new Dictionary<(ISymbol symbol, int declarationIndex), DeclarationAnalysisData>();
            }

            public CachingSemanticModelProvider SemanticModelProvider { get; }
            public SuppressMessageAttributeState SuppressMessageAttributeState { get; }

            internal DeclarationAnalysisData GetOrComputeDeclarationAnalysisData(
                ISymbol declaredSymbol,
                int declarationIndex,
                Func<DeclarationAnalysisData> computeDeclarationAnalysisData,
                bool cacheAnalysisData)
            {
                if (!cacheAnalysisData)
                {
                    return computeDeclarationAnalysisData();
                }

                var key = (declaredSymbol, declarationIndex);
                lock (_declarationAnalysisDataMap)
                {
                    if (_declarationAnalysisDataMap.TryGetValue(key, out var cachedData))
                    {
                        return cachedData;
                    }
                }

                DeclarationAnalysisData data = computeDeclarationAnalysisData();

                lock (_declarationAnalysisDataMap)
                {
                    if (!_declarationAnalysisDataMap.TryGetValue(key, out var existingData))
                    {
                        _declarationAnalysisDataMap.Add(key, data);
                    }
                    else
                    {
                        data = existingData;
                    }
                }

                return data;
            }

            internal void ClearDeclarationAnalysisData(ISymbol declaredSymbol, int declarationIndex)
            {
                lock (_declarationAnalysisDataMap)
                {
                    _declarationAnalysisDataMap.Remove((declaredSymbol, declarationIndex));
                }
            }
        }
    }
}
