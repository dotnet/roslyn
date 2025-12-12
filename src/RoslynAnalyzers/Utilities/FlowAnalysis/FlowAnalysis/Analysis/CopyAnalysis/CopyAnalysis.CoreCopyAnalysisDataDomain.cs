// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using CoreCopyAnalysisData = DictionaryAnalysisData<AnalysisEntity, CopyAbstractValue>;

    public partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// An abstract analysis domain implementation for <see cref="CoreCopyAnalysisData"/>.
        /// </summary>
        private sealed class CoreCopyAnalysisDataDomain : MapAbstractDomain<AnalysisEntity, CopyAbstractValue>
        {
            private readonly Func<AnalysisEntity, CopyAbstractValue> _getDefaultCopyValue;

            public CoreCopyAnalysisDataDomain(AbstractValueDomain<CopyAbstractValue> valueDomain, Func<AnalysisEntity, CopyAbstractValue> getDefaultCopyValue)
                : base(valueDomain)
            {
                _getDefaultCopyValue = getDefaultCopyValue;
            }

#pragma warning disable CA1725 // Parameter names should match base declaration
            public override CoreCopyAnalysisData Merge(CoreCopyAnalysisData map1, CoreCopyAnalysisData map2)
#pragma warning restore CA1725 // Parameter names should match base declaration
            {
                CopyAnalysisData.AssertValidCopyAnalysisData(map1);
                CopyAnalysisData.AssertValidCopyAnalysisData(map2);

                var result = new DictionaryAnalysisData<AnalysisEntity, CopyAbstractValue>();
                foreach (var kvp in map1)
                {
                    var key = kvp.Key;
                    var value1 = kvp.Value;

                    // If the key exists in both maps, use the merged value.
                    // Otherwise, use the default value.
                    CopyAbstractValue mergedValue;
                    if (map2.TryGetValue(key, out var value2))
                    {
                        mergedValue = ValueDomain.Merge(value1, value2);
                    }
                    else
                    {
                        mergedValue = _getDefaultCopyValue(key);
                    }

                    result.Add(key, mergedValue);
                }

                foreach (var kvp in map2)
                {
                    if (!result.ContainsKey(kvp.Key))
                    {
                        result.Add(kvp.Key, _getDefaultCopyValue(kvp.Key));
                    }
                }

                CopyAnalysisData.AssertValidCopyAnalysisData(result);
                return result;
            }
        }
    }
}
