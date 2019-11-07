// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class PredicatedAnalysisData<TKey, TValue>
    {
        /// <summary>
        /// Analysis data predicated by true/false value of an <see cref="AnalysisEntity"/>.
        /// Used to improve the preciseness of analysis when we can apply the <see cref="TruePredicatedData"/> or <see cref="FalsePredicatedData"/>
        /// on the control flow paths where the corresonding <see cref="AnalysisEntity"/> is known to have <code>true</code> or <code>false</code> value respectively.
        /// </summary>
        protected sealed class PerEntityPredicatedAnalysisData : IDisposable
        {
            public PerEntityPredicatedAnalysisData(DictionaryAnalysisData<TKey, TValue>? truePredicatedData, DictionaryAnalysisData<TKey, TValue>? falsePredicatedData)
            {
                Debug.Assert(truePredicatedData != null || falsePredicatedData != null);

                if (truePredicatedData != null)
                {
                    TruePredicatedData = new DictionaryAnalysisData<TKey, TValue>(truePredicatedData);
                }

                if (falsePredicatedData != null)
                {
                    FalsePredicatedData = new DictionaryAnalysisData<TKey, TValue>(falsePredicatedData);
                }
            }

            public PerEntityPredicatedAnalysisData(PerEntityPredicatedAnalysisData fromData)
                : this(fromData.TruePredicatedData, fromData.FalsePredicatedData)
            {
            }

            /// <summary>
            /// Analysis data for <code>true</code> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <code>null</code> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <code>true</code>.
            /// </summary>
            public DictionaryAnalysisData<TKey, TValue>? TruePredicatedData { get; private set; }

            /// <summary>
            /// Analysis data for <code>false</code> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <code>null</code> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <code>false</code>.
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
