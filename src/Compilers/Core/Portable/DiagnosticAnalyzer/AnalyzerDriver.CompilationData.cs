// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        internal class CompilationData
        {
            private readonly Dictionary<SyntaxReference, DeclarationAnalysisData> _declarationAnalysisDataMap;

            public CompilationData(Compilation compilation)
            {
                Debug.Assert(compilation.SemanticModelProvider is CachingSemanticModelProvider);

                SemanticModelProvider = (CachingSemanticModelProvider)compilation.SemanticModelProvider;
                this.SuppressMessageAttributeState = new SuppressMessageAttributeState(compilation);
                _declarationAnalysisDataMap = new Dictionary<SyntaxReference, DeclarationAnalysisData>();
            }

            public CachingSemanticModelProvider SemanticModelProvider { get; }
            public SuppressMessageAttributeState SuppressMessageAttributeState { get; }

            internal DeclarationAnalysisData GetOrComputeDeclarationAnalysisData(
                SyntaxReference declaration,
                Func<DeclarationAnalysisData> computeDeclarationAnalysisData,
                bool cacheAnalysisData)
            {
                if (!cacheAnalysisData)
                {
                    return computeDeclarationAnalysisData();
                }

                lock (_declarationAnalysisDataMap)
                {
                    if (_declarationAnalysisDataMap.TryGetValue(declaration, out var cachedData))
                    {
                        return cachedData;
                    }
                }

                DeclarationAnalysisData data = computeDeclarationAnalysisData();

                lock (_declarationAnalysisDataMap)
                {
                    if (!_declarationAnalysisDataMap.TryGetValue(declaration, out var existingData))
                    {
                        _declarationAnalysisDataMap.Add(declaration, data);
                    }
                    else
                    {
                        data = existingData;
                    }
                }

                return data;
            }

            internal void ClearDeclarationAnalysisData(SyntaxReference declaration)
            {
                lock (_declarationAnalysisDataMap)
                {
                    _declarationAnalysisDataMap.Remove(declaration);
                }
            }
        }
    }
}
