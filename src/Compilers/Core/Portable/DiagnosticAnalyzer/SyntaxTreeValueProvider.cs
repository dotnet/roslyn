// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provides custom values associated with <see cref="SyntaxTree"/> instances using the given 'computeValue' delegate.
    /// </summary>
    public sealed class SyntaxTreeValueProvider<TValue>
    {
        internal AnalysisValueProvider<SyntaxTree, TValue> CoreValueProvider { get; private set; }

        /// <summary>
        /// Provides values associated with <see cref="SyntaxTree"/> instances using the given <paramref name="computeValue"/>.
        /// </summary>
        /// <param name="computeValue">Delegate to compute value associated with a given <see cref="SyntaxTree"/> instance.</param>
        /// <param name="syntaxTreeComparer">Optional equality comparer to determine equivalent <see cref="SyntaxTree"/> instances that have the same value.
        /// If no comparer is provided, then <see cref="SyntaxTreeComparer"/> is used by default.</param>
        public SyntaxTreeValueProvider(Func<SyntaxTree, TValue> computeValue, IEqualityComparer<SyntaxTree> syntaxTreeComparer = null)
        {
            CoreValueProvider = new AnalysisValueProvider<SyntaxTree, TValue>(computeValue, syntaxTreeComparer ?? SyntaxTreeComparer.Instance);
        }
    }
}
