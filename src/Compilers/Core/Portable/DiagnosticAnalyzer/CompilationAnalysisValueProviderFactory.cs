// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
