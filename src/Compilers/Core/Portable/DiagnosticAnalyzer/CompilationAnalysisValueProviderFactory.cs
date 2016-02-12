// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class CompilationAnalysisValueProviderFactory
    {
        private Dictionary<object, object> _lazySharedStateProviderMap;

        public CompilationAnalysisValueProvider<TKey, TValue> GetValueProvider<TKey, TValue>(AnalysisValueProvider<TKey, TValue> analysisSharedStateProvider)
            where TKey : class
        {
            if (_lazySharedStateProviderMap == null)
            {
                Interlocked.CompareExchange(ref _lazySharedStateProviderMap, new Dictionary<object, object>(), null);
            }

            object value;
            lock (_lazySharedStateProviderMap)
            {
                if (!_lazySharedStateProviderMap.TryGetValue(analysisSharedStateProvider, out value))
                {
                    value = new CompilationAnalysisValueProvider<TKey, TValue>(analysisSharedStateProvider);
                    _lazySharedStateProviderMap[analysisSharedStateProvider] = value;
                }
            }

            return value as CompilationAnalysisValueProvider<TKey, TValue>;
        }
    }
}
