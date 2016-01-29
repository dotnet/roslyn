// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provides custom values associated with <see cref="SourceText"/> instances using the given 'computeValue' delegate.
    /// </summary>
    public sealed class SourceTextValueProvider<TValue>
    {
        internal AnalysisValueProvider<SourceText, TValue> CoreValueProvider { get; private set; }

        /// <summary>
        /// Provides custom values associated with <see cref="SourceText"/> instances using the given <paramref name="computeValue"/>.
        /// </summary>
        /// <param name="computeValue">Delegate to compute value associated with a given <see cref="SourceText"/> instance.</param>
        /// <param name="sourceTextComparer">Optional equality comparer to determine equivalent <see cref="SourceText"/> instances that have the same value.
        /// If no comparer is provided, then <see cref="SourceTextComparer"/> is used by default.</param>
        public SourceTextValueProvider(Func<SourceText, TValue> computeValue, IEqualityComparer<SourceText> sourceTextComparer = null)
        {
            CoreValueProvider = new AnalysisValueProvider<SourceText, TValue>(computeValue, sourceTextComparer ?? SourceTextComparer.Instance);
        }
    }
}
