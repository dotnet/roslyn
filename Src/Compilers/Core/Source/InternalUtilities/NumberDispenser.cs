using System.Runtime.CompilerServices;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class NumberDispenser
    {
        // maps an object to its counter
        private static readonly ConditionalWeakTable<object, StrongBox<int>> counterRegistry =
            new ConditionalWeakTable<object, StrongBox<int>>();

        private static readonly ConditionalWeakTable<object, StrongBox<int>>.CreateValueCallback counterFactory =
            _ => new StrongBox<int>();

        /// <summary>
        /// Returns a sequential number from a counter associated with an object.
        /// I.E. When called with same object, results will be 0,1,2,3,4,5 ....
        /// 
        /// Note this is supposed to be used with objects that only _sometimes_ need a counter.
        /// If a counter is needed frequently, add it to the object.
        /// </summary>
        internal static int GetNextNumber(object counterOwner)
        {
            return Interlocked.Increment(ref counterRegistry.GetValue(counterOwner, counterFactory).Value) - 1;
        }
    }
}
