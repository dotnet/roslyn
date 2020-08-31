// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CachingAnalyzerConfigSet
    {
        private readonly ConcurrentDictionary<string, AnalyzerConfigOptionsResult> _sourcePathToResult = new ConcurrentDictionary<string, AnalyzerConfigOptionsResult>();
        private readonly Func<string, AnalyzerConfigOptionsResult> _computeFunction;
        private readonly AnalyzerConfigSet _underlyingSet;

        public AnalyzerConfigOptionsResult GlobalConfigOptions => _underlyingSet.GlobalConfigOptions;

        public CachingAnalyzerConfigSet(AnalyzerConfigSet underlyingSet)
        {
            _underlyingSet = underlyingSet;
            _computeFunction = _underlyingSet.GetOptionsForSourcePath;
        }

        public AnalyzerConfigOptionsResult GetOptionsForSourcePath(string sourcePath)
        {
            return _sourcePathToResult.GetOrAdd(sourcePath, _computeFunction);
        }
    }
}
