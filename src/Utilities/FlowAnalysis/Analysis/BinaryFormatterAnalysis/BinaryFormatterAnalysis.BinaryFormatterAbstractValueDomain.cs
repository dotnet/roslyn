// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    internal partial class BinaryFormatterAnalysis
    {
        /// <summary>
        /// Abstract value domain for <see cref="BinaryFormatterAnalysis"/> to merge and compare <see cref="BinaryFormatterAbstractValue"/> values.
        /// </summary>
        private class BinaryFormatterAbstractValueDomain : AbstractValueDomain<BinaryFormatterAbstractValue>
        {
            public static BinaryFormatterAbstractValueDomain Default = new BinaryFormatterAbstractValueDomain();

            private BinaryFormatterAbstractValueDomain() { }

            public override BinaryFormatterAbstractValue Bottom => BinaryFormatterAbstractValue.NotApplicable;

            public override BinaryFormatterAbstractValue UnknownOrMayBeValue => BinaryFormatterAbstractValue.NotApplicable;

            public override int Compare(BinaryFormatterAbstractValue oldValue, BinaryFormatterAbstractValue newValue)
            {
                return Comparer<BinaryFormatterAbstractValue>.Default.Compare(oldValue, newValue);
            }

            public override BinaryFormatterAbstractValue Merge(BinaryFormatterAbstractValue value1, BinaryFormatterAbstractValue value2)
            {
                if (value1 == value2)
                {
                    return value1;
                }
                else
                {
                    return BinaryFormatterAbstractValue.MaybeFlagged;
                }
            }
        }
    }
}
