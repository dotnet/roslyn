using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    internal partial class BinaryFormatterAnalysis
    {
        private sealed class CoreBinaryFormatterAnalysisDataDomain : AnalysisEntityMapAbstractDomain<BinaryFormatterAbstractValue>
        {
            public static readonly CoreBinaryFormatterAnalysisDataDomain Instance = new CoreBinaryFormatterAnalysisDataDomain(BinaryFormatterAbstractValueDomain.Default);

            private CoreBinaryFormatterAnalysisDataDomain(BinaryFormatterAbstractValueDomain valueDomain) : base(valueDomain)
            {
            }

            protected override bool CanSkipNewEntry(AnalysisEntity analysisEntity, BinaryFormatterAbstractValue value)
            {
                return value == BinaryFormatterAbstractValue.Unknown;
            }

            protected override BinaryFormatterAbstractValue GetDefaultValue(AnalysisEntity analysisEntity)
            {
                return BinaryFormatterAbstractValue.Unknown;
            }
        }
    }
}
