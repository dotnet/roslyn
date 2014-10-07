using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A retainer that computes its value from two inputs and a computation function.
    /// The value is not stored.
    /// </summary>
    internal class ComputedValueSource<TValue1, TValue2, TValue3> : ValueSource<TValue3>
    {
        private readonly IValueSource<TValue1> valueSource1;
        private readonly IValueSource<TValue2> valueSource2;
        private readonly Func<TValue1, TValue2, CancellationToken, TValue3> computation;

        public ComputedValueSource(
            IValueSource<TValue1> valueSource1,
            IValueSource<TValue2> valueSource2,
            Func<TValue1, TValue2, CancellationToken, TValue3> computation)
        {
            this.valueSource1 = valueSource1;
            this.valueSource2 = valueSource2;
            this.computation = computation;
        }

        public override bool TryGetValue(out TValue3 value)
        {
            value = default(TValue3);
            return false;
        }

        public override TValue3 GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.computation(
                        this.valueSource1.GetValue(cancellationToken), 
                        this.valueSource2.GetValue(cancellationToken),
                        cancellationToken);
        }

        public override Task<TValue3> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // get the text async and then parse it.
            return this.valueSource1.GetValueAsync(cancellationToken).SafeContinueWith(
                value1Task => this.valueSource2.GetValueAsync(cancellationToken)
                                .SafeContinueWith(
                                    value2Task => Tuple.Create(value1Task.Result, value2Task.Result), 
                                    cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default),
                    cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .Unwrap()
                .SafeContinueWith(tupleTask =>
                {
                    var tuple = tupleTask.Result;
                    return this.computation(tuple.Item1, tuple.Item2, cancellationToken);
                }, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
}