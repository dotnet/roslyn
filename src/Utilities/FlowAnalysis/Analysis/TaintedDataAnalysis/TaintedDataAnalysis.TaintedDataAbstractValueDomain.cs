
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
                throw new System.NotImplementedException();
            }

            public override TaintedDataAbstractValue Merge(TaintedDataAbstractValue value1, TaintedDataAbstractValue value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == TaintedDataAbstractValueKind.Tainted || value2.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    return value1;
                }
                else if (value1.Kind == TaintedDataAbstractValueKind.Unknown || value2.Kind == TaintedDataAbstractValueKind.Unknown)
                {
                    return TaintedDataAbstractValue.Unknown;
                }

                throw new System.NotImplementedException();
            }
        }
    }
}
