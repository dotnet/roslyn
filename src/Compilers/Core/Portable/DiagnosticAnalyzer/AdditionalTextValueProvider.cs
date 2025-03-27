// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provides custom values associated with <see cref="AdditionalText"/> instances using the given computeValue delegate.
    /// </summary>
    public sealed class AdditionalTextValueProvider<TValue>
    {
        internal readonly AnalysisValueProvider<AdditionalText, TValue> CoreValueProvider;

        /// <summary>
        /// Provides custom values associated with <see cref="AdditionalText"/> instances using the given <paramref name="computeValue"/>.
        /// </summary>
        /// <param name="computeValue">Delegate to compute the value associated with a given <see cref="AdditionalText"/> instance.</param>
        /// <param name="additionalTextComparer">Optional equality comparer to determine equivalent <see cref="AdditionalText"/> instances that have the same value.
        /// If no comparer is provided, then <see cref="EqualityComparer{T}.Default"/> is used by default.</param>
        public AdditionalTextValueProvider(Func<AdditionalText, TValue> computeValue, IEqualityComparer<AdditionalText>? additionalTextComparer = null)
        {
            CoreValueProvider = new AnalysisValueProvider<AdditionalText, TValue>(computeValue, additionalTextComparer ?? EqualityComparer<AdditionalText>.Default);
        }
    }
}
