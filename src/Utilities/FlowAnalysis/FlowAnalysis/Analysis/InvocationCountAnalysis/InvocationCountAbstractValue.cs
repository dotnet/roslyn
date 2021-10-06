// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal enum InvocationTimes
    {
        Zero,
        OneTime,
        MoreThanOneTime,
        Unknown
    }

    internal class InvocationCountAbstractValue : IAbstractAnalysisValue
    {
        public AnalysisEntity InvocationInstance { get; }

        public InvocationTimes InvocationTimes { get; }

        public InvocationCountAbstractValue(AnalysisEntity invocationInstance, InvocationTimes invocationTimes)
        {
            InvocationInstance = invocationInstance;
            InvocationTimes = invocationTimes;
        }

        public bool Equals(IAbstractAnalysisValue other)
        {
            if (other is InvocationCountAbstractValue otherValue)
            {
                return otherValue.InvocationInstance.Equals(InvocationInstance) && otherValue.InvocationTimes == InvocationTimes;
            }

            return false;
        }

        public IAbstractAnalysisValue GetNegatedValue()
        {
            return this;
        }
    }
}