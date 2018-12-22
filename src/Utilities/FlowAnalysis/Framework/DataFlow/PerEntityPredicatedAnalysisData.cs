// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    internal abstract partial class PredicatedAnalysisData<TKey, TValue>
    {
        /// <summary>
        /// Analysis data predicated by true/false value of an <see cref="AnalysisEntity"/>.
        /// Used to improve the preciseness of analysis when we can apply the <see cref="TruePredicatedData"/> or <see cref="FalsePredicatedData"/>
        /// on the control flow paths where the corresonding <see cref="AnalysisEntity"/> is known to have <code>true</code> or <code>false</code> value respectively.
        /// </summary>
        protected sealed class PerEntityPredicatedAnalysisData : IDisposable
        {
#if DEBUG
            private bool _disposed;
#endif

            public PerEntityPredicatedAnalysisData(DictionaryAnalysisData<TKey, TValue> truePredicatedData, DictionaryAnalysisData<TKey, TValue> falsePredicatedData)
            {
                Debug.Assert(truePredicatedData != null || falsePredicatedData != null);

                TruePredicatedData = truePredicatedData;
                FalsePredicatedData = falsePredicatedData;
            }

            public PerEntityPredicatedAnalysisData(PerEntityPredicatedAnalysisData fromData)
            {
                if (fromData.TruePredicatedData != null)
                {
                    TruePredicatedData = new DictionaryAnalysisData<TKey, TValue>(fromData.TruePredicatedData);
                }

                if (fromData.FalsePredicatedData != null)
                {
                    FalsePredicatedData = new DictionaryAnalysisData<TKey, TValue>(fromData.FalsePredicatedData);
                }
            }

            /// <summary>
            /// Analysis data for <code>true</code> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <code>null</code> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <code>true</code>.
            /// </summary>
            public DictionaryAnalysisData<TKey, TValue> TruePredicatedData { get; }

            /// <summary>
            /// Analysis data for <code>false</code> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <code>null</code> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <code>false</code>.
            /// </summary>
            public DictionaryAnalysisData<TKey, TValue> FalsePredicatedData { get; }

            public void Dispose()
            {
                TruePredicatedData?.Dispose();
                FalsePredicatedData?.Dispose();
#if DEBUG
                Debug.Assert(!_disposed);
                _disposed = true;
#endif
            }
        }
    }
}
