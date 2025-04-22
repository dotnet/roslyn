// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class PredicatedAnalysisData<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// Analysis data predicated by true/false value of an <see cref="AnalysisEntity"/>.
        /// Used to improve the preciseness of analysis when we can apply the <see cref="TruePredicatedData"/> or <see cref="FalsePredicatedData"/>
        /// on the control flow paths where the corresponding <see cref="AnalysisEntity"/> is known to have <see langword="true"/> or <see langword="false"/> value respectively.
        /// </summary>
        protected sealed class PerEntityPredicatedAnalysisData : IDisposable
        {
            public PerEntityPredicatedAnalysisData(DictionaryAnalysisData<TKey, TValue>? truePredicatedData, DictionaryAnalysisData<TKey, TValue>? falsePredicatedData)
            {
                Debug.Assert(truePredicatedData != null || falsePredicatedData != null);

                if (truePredicatedData != null)
                {
                    TruePredicatedData = [.. truePredicatedData];
                }

                if (falsePredicatedData != null)
                {
                    FalsePredicatedData = [.. falsePredicatedData];
                }
            }

            public PerEntityPredicatedAnalysisData(PerEntityPredicatedAnalysisData fromData)
                : this(fromData.TruePredicatedData, fromData.FalsePredicatedData)
            {
            }

            /// <summary>
            /// Analysis data for <see langword="true"/> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <see langword="null"/> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <see langword="true"/>.
            /// </summary>
            public DictionaryAnalysisData<TKey, TValue>? TruePredicatedData { get; private set; }

            /// <summary>
            /// Analysis data for <see langword="false"/> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <see langword="null"/> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <see langword="false"/>.
            /// </summary>
            public DictionaryAnalysisData<TKey, TValue>? FalsePredicatedData { get; private set; }

            public void Dispose()
            {
                TruePredicatedData?.Dispose();
                TruePredicatedData = null;
                FalsePredicatedData?.Dispose();
                FalsePredicatedData = null;
            }
        }
    }
}
