using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class RelationshipSet : IInvocationSet
    {
        public ImmutableHashSet<IInvocationSet> InvocationSets { get; }

        public InvocationSetKind Kind { get; }
    }
}
