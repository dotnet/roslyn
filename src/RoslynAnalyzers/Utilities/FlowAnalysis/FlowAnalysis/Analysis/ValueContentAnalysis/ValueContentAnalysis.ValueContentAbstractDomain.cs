// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    public partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="ValueContentAnalysis"/> to merge and compare <see cref="ValueContentAbstractValue"/> values.
        /// </summary>
        private sealed class ValueContentAbstractValueDomain : AbstractValueDomain<ValueContentAbstractValue>
        {
            public static ValueContentAbstractValueDomain Default = new();

            private ValueContentAbstractValueDomain() { }

            public override ValueContentAbstractValue Bottom => ValueContentAbstractValue.UndefinedState;

            public override ValueContentAbstractValue UnknownOrMayBeValue => ValueContentAbstractValue.MayBeContainsNonLiteralState;

            public override int Compare(ValueContentAbstractValue oldValue, ValueContentAbstractValue newValue, bool assertMonotonicity)
            {
                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.NonLiteralState == newValue.NonLiteralState)
                {
                    if (oldValue.IsLiteralState)
                    {
                        if (oldValue.LiteralValues.SetEquals(newValue.LiteralValues))
                        {
                            return 0;
                        }
                        else if (oldValue.LiteralValues.IsSubsetOf(newValue.LiteralValues))
                        {
                            return -1;
                        }
                        else
                        {
                            FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                            return 1;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (oldValue.NonLiteralState < newValue.NonLiteralState)
                {
                    return -1;
                }
                else
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    return 1;
                }
            }

            public override ValueContentAbstractValue Merge(ValueContentAbstractValue value1, ValueContentAbstractValue value2)
            {
                return value1.Merge(value2);
            }
        }
    }
}
