
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
                throw new System.NotImplementedException();
            }
        }
    }
}
