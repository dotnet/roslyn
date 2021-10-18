// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisValueDomain : AbstractValueDomain<InvocationCountAnalysisValue>
    {
        public InvocationCountAnalysisValueDomain()
        {
        }

        public override InvocationCountAnalysisValue UnknownOrMayBeValue => throw new NotImplementedException();

        public override InvocationCountAnalysisValue Bottom => throw new NotImplementedException();

        public override int Compare(InvocationCountAnalysisValue oldValue, InvocationCountAnalysisValue newValue, bool assertMonotonicity)
        {
            throw new NotImplementedException();
        }

        public override InvocationCountAnalysisValue Merge(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            throw new NotImplementedException();
        }
    }
}
