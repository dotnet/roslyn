// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.GlobalFlowStateDictionaryAnalysis
{
    internal class GlobalFlowStateDictionaryAnalysisValueDomain : AbstractValueDomain<GlobalFlowStateDictionaryAnalysisValue>
    {
        public static readonly GlobalFlowStateDictionaryAnalysisValueDomain Instance = new();

        private GlobalFlowStateDictionaryAnalysisValueDomain()
        {
        }

        public override GlobalFlowStateDictionaryAnalysisValue UnknownOrMayBeValue => GlobalFlowStateDictionaryAnalysisValue.Unknown;

        public override GlobalFlowStateDictionaryAnalysisValue Bottom => GlobalFlowStateDictionaryAnalysisValue.Empty;

        public override int Compare(GlobalFlowStateDictionaryAnalysisValue oldValue, GlobalFlowStateDictionaryAnalysisValue newValue, bool assertMonotonicity)
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

        public override GlobalFlowStateDictionaryAnalysisValue Merge(GlobalFlowStateDictionaryAnalysisValue value1, GlobalFlowStateDictionaryAnalysisValue value2)
        {
            if (value1 == null)
            {
                return value2;
            }

            if (value2 == null)
            {
                return value1;
            }

            if (value1.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Unknown || value2.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Unknown)
            {
                return GlobalFlowStateDictionaryAnalysisValue.Unknown;
            }

            if (value1.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Empty)
            {
                return value2;
            }

            if (value2.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Empty)
            {
                return value1;
            }

            return GlobalFlowStateDictionaryAnalysisValue.Merge(value1, value2);
        }

        public static GlobalFlowStateDictionaryAnalysisValue Intersect(GlobalFlowStateDictionaryAnalysisValue value1, GlobalFlowStateDictionaryAnalysisValue value2)
        {
            if (value1 == null)
            {
                return value2;
            }

            if (value2 == null)
            {
                return value1;
            }

            if (value1.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Unknown || value2.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Unknown)
            {
                return GlobalFlowStateDictionaryAnalysisValue.Unknown;
            }

            return GlobalFlowStateDictionaryAnalysisValue.Intersect(value1, value2);
        }
    }
}
