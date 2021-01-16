// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CoreCopyAnalysisData = DictionaryAnalysisData<AnalysisEntity, CopyAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

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

            public override CoreCopyAnalysisData Merge(CoreCopyAnalysisData value1, CoreCopyAnalysisData value2)
            {
                CopyAnalysisData.AssertValidCopyAnalysisData(value1);
                CopyAnalysisData.AssertValidCopyAnalysisData(value2);

                var result = new DictionaryAnalysisData<AnalysisEntity, CopyAbstractValue>();
                foreach (var kvp in value1)
                {
                    var key = kvp.Key;
                    var value1 = kvp.Value;

                    // If the key exists in both maps, use the merged value.
                    // Otherwise, use the default value.
                    CopyAbstractValue mergedValue;
                    if (value2.TryGetValue(key, out var value2))
                    {
                        mergedValue = ValueDomain.Merge(value1, value2);
                    }
                    else
                    {
                        mergedValue = _getDefaultCopyValue(key);
                    }

                    result.Add(key, mergedValue);
                }

                foreach (var kvp in value2)
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