
namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataAbstractValueDomain : AbstractValueDomain<TaintedDataAbstractValue>
        {
            public static readonly TaintedDataAbstractValueDomain Default = new TaintedDataAbstractValueDomain();

            public override TaintedDataAbstractValue UnknownOrMayBeValue => TaintedDataAbstractValue.Unknown;

            public override TaintedDataAbstractValue Bottom => TaintedDataAbstractValue.Unknown;


            public override int Compare(TaintedDataAbstractValue oldValue, TaintedDataAbstractValue newValue)
            {
                // The newly computed abstract values for each basic block
                // must be always greater or equal than the previous value
                // to ensure termination.
                
                // Unknown < NotTainted < Tainted
                return oldValue.Kind.CompareTo(newValue.Kind);
            }

            public override TaintedDataAbstractValue Merge(TaintedDataAbstractValue value1, TaintedDataAbstractValue value2)
            {
                //     U N T
                //   +------
                // U | U N T
                // N | N N T
                // T | T T T

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

                return this.Compare(value1, value2) >= 0 ? value1 : value2;
            }
        }
    }
}
