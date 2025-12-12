// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    public partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// Abstract value domain to merge and compare <see cref="NullAbstractValue"/> values.
        /// </summary>
        private sealed class NullAbstractValueDomain : AbstractValueDomain<NullAbstractValue>
        {
            public static NullAbstractValueDomain Default = new();

            private NullAbstractValueDomain() { }

            public override NullAbstractValue Bottom => NullAbstractValue.Undefined;

            public override NullAbstractValue UnknownOrMayBeValue => NullAbstractValue.MaybeNull;

            public override int Compare(NullAbstractValue oldValue, NullAbstractValue newValue, bool assertMonotonicity)
            {
                return Comparer<NullAbstractValue>.Default.Compare(oldValue, newValue);
            }

            public override NullAbstractValue Merge(NullAbstractValue value1, NullAbstractValue value2)
            {
                NullAbstractValue result;

                if (value1 == NullAbstractValue.MaybeNull ||
                    value2 == NullAbstractValue.MaybeNull)
                {
                    result = NullAbstractValue.MaybeNull;
                }
                else if (value1 is NullAbstractValue.Invalid or NullAbstractValue.Undefined)
                {
                    result = value2;
                }
                else if (value2 is NullAbstractValue.Invalid or NullAbstractValue.Undefined)
                {
                    result = value1;
                }
                else if (value1 != value2)
                {
                    // One of the values must be 'Null' and other value must be 'NotNull'.
                    result = NullAbstractValue.MaybeNull;
                }
                else
                {
                    result = value1;
                }

                return result;
            }
        }
    }
}
