// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design.Serialization;
using System.Runtime;
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
        public IOperation InvocationOperation { get; }

        public InvocationCountAbstractValue(IOperation operation)
        {
            InvocationOperation = operation;
        }

        public bool Equals(IAbstractAnalysisValue other)
        {
            if (other is InvocationCountAbstractValue otherValue)
            {
                return InvocationOperation.Equals(otherValue.InvocationOperation);
            }

            return false;
        }

        public IAbstractAnalysisValue GetNegatedValue()
        {
            return this;
        }
    }
}