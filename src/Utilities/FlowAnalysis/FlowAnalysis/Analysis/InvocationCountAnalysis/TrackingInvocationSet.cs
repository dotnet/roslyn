using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class TrackingInvocationSet : IInvocationSet
    {
        public ImmutableDictionary<IOperation, InvocationCount> CountedInvocationOperations { get; }

        public InvocationSetKind Kind => InvocationSetKind.Invocations;

    }
}
