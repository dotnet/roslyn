// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisValueDomain : AbstractValueDomain<InvocationCountAnalysisValue>
    {
        public static readonly InvocationCountAnalysisValueDomain Instance = new();

        private InvocationCountAnalysisValueDomain()
        {
        }

        public override InvocationCountAnalysisValue UnknownOrMayBeValue => InvocationCountAnalysisValue.Unknown;

        public override InvocationCountAnalysisValue Bottom => InvocationCountAnalysisValue.Empty;

        public override int Compare(InvocationCountAnalysisValue oldValue, InvocationCountAnalysisValue newValue, bool assertMonotonicity)
        {
            if (ReferenceEquals(oldValue, newValue))
            {
                return 0;
            }

            if (oldValue.Kind == newValue.Kind)
            {
                return oldValue.Equals(newValue) ? 0 : -1;
            }

            if (oldValue.Kind < newValue.Kind)
            {
                return -1;
            }

            FireNonMonotonicAssertIfNeeded(assertMonotonicity);
            return 1;
        }

        public override InvocationCountAnalysisValue Merge(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            if (value1 == null)
            {
                return value2;
            }

            if (value2 == null)
            {
                return value1;
            }

            if (value1.Kind == InvocationCountAnalysisValueKind.Unknown || value2.Kind == InvocationCountAnalysisValueKind.Unknown)
            {
                return InvocationCountAnalysisValue.Unknown;
            }

            if (value1.Kind == InvocationCountAnalysisValueKind.Empty)
            {
                return value2;
            }

            if (value2.Kind == InvocationCountAnalysisValueKind.Empty)
            {
                return value1;
            }

            return InvocationCountAnalysisValue.Merge(value1, value2);
        }

        public static InvocationCountAnalysisValue Intersect(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            if (value1 == null)
            {
                return value2;
            }

            if (value2 == null)
            {
                return value1;
            }

            if (value1.Kind == InvocationCountAnalysisValueKind.Unknown || value2.Kind == InvocationCountAnalysisValueKind.Unknown)
            {
                return InvocationCountAnalysisValue.Unknown;
            }

            return InvocationCountAnalysisValue.Intersect(value1, value2);
        }
    }
}
