// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

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
            public static ValueContentAbstractValueDomain Default = new ValueContentAbstractValueDomain();

            private ValueContentAbstractValueDomain() { }

            public override ValueContentAbstractValue Bottom => ValueContentAbstractValue.UndefinedState;

            public override ValueContentAbstractValue UnknownOrMayBeValue => ValueContentAbstractValue.MayBeContainsNonLiteralState;

            public override int Compare(ValueContentAbstractValue oldValue, ValueContentAbstractValue newValue, bool assertMonotonicity)
            {
                Debug.Assert(oldValue != null);
                Debug.Assert(newValue != null);

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
