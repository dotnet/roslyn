// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataAbstractValueDomain : AbstractValueDomain<TaintedDataAbstractValue>
        {
            public static readonly TaintedDataAbstractValueDomain Default = new();

            public override TaintedDataAbstractValue UnknownOrMayBeValue => TaintedDataAbstractValue.NotTainted;

            public override TaintedDataAbstractValue Bottom => TaintedDataAbstractValue.NotTainted;

            public override int Compare(TaintedDataAbstractValue oldValue, TaintedDataAbstractValue newValue, bool assertMonotonicity)
            {
                // The newly computed abstract values for each basic block
                // must be always greater or equal than the previous value
                // to ensure termination.

                // NotTainted < Tainted
                if (oldValue.Kind == TaintedDataAbstractValueKind.Tainted && newValue.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    return SetAbstractDomain<SymbolAccess>.Default.Compare(oldValue.SourceOrigins, newValue.SourceOrigins);
                }
                else
                {
                    return oldValue.Kind.CompareTo(newValue.Kind);
                }
            }

            public override TaintedDataAbstractValue Merge(TaintedDataAbstractValue value1, TaintedDataAbstractValue value2)
            {
                //     N T
                //   +----
                // N | N T
                // T | T T

                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }

                if (value1.Kind == TaintedDataAbstractValueKind.Tainted && value2.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    // If both are tainted, we need to merge their origins.
                    return TaintedDataAbstractValue.MergeTainted(value1, value2);
                }

                return value1.Kind > value2.Kind ? value1 : value2;
            }
        }
    }
}
