// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        protected sealed class PerEntityPredicatedAnalysisData
        {
            public PerEntityPredicatedAnalysisData(IDictionary<TKey, TValue> truePredicatedData, IDictionary<TKey, TValue> falsePredicatedData)
            {
                Debug.Assert(truePredicatedData != null || falsePredicatedData != null);

                TruePredicatedData = truePredicatedData;
                FalsePredicatedData = falsePredicatedData;
            }

            /// <summary>
            /// Analysis data for <code>true</code> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <code>null</code> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <code>true</code>.
            /// </summary>
            public IDictionary<TKey, TValue> TruePredicatedData { get; }

            /// <summary>
            /// Analysis data for <code>false</code> value of the corresponding <see cref="AnalysisEntity"/> on which this data is predicated.
            /// <code>null</code> value indicates the corresponding <see cref="AnalysisEntity"/> can never be <code>false</code>.
            /// </summary>
            public IDictionary<TKey, TValue> FalsePredicatedData { get; }
        }
    }
}
