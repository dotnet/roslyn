// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountValueDomain : AbstractValueDomain<InvocationCountAbstractValue>
    {
        public static InvocationCountValueDomain Instance = new();

        public override InvocationCountAbstractValue Bottom => InvocationCountAbstractValue.Zero;

        public override InvocationCountAbstractValue UnknownOrMayBeValue => InvocationCountAbstractValue.Unknown;

        public override InvocationCountAbstractValue Merge(InvocationCountAbstractValue value1, InvocationCountAbstractValue value2) =>
            (value1.Kind, value2.Kind) switch
            {
                (InvocationCountAbstractValueKind.Unknown, _) => InvocationCountAbstractValue.Unknown,
                (_, InvocationCountAbstractValueKind.Unknown) => InvocationCountAbstractValue.Unknown,
                (InvocationCountAbstractValueKind.Zero, _) => value2,
                (_, InvocationCountAbstractValueKind.Zero) => value1,
                (InvocationCountAbstractValueKind.MoreThanOneTime, _) => InvocationCountAbstractValue.MoreThanOneTime,
                (_, InvocationCountAbstractValueKind.MoreThanOneTime) => InvocationCountAbstractValue.MoreThanOneTime,
                (InvocationCountAbstractValueKind.OneTime, InvocationCountAbstractValueKind.OneTime) => InvocationCountAbstractValue.MoreThanOneTime,
                _ => throw new ArgumentException($"Unexpected combinations of {value1} and {value2}"),
            };

        public override int Compare(InvocationCountAbstractValue oldValue, InvocationCountAbstractValue newValue, bool assertMonotonicity)
        {
            var result = oldValue.Kind - newValue.Kind;
            if (result > 0)
            {
                FireNonMonotonicAssertIfNeeded(assertMonotonicity);
            }

            return result;
        }
    }
}