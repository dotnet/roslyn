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
    /// A retainer that computes its value from a retained value and a computation function.
    /// The value is not stored.
    /// </summary>
    internal class ComputedValueSource<TValue1, TValue2> : ValueSource<TValue2>
    {
        private readonly IValueSource<TValue1> valueSource;
        private readonly Func<TValue1, CancellationToken, TValue2> computation;

        public ComputedValueSource(
            IValueSource<TValue1> valueSource, 
            Func<TValue1, CancellationToken, TValue2> computation)
        {
            this.valueSource = valueSource;
            this.computation = computation;
        }

        public override bool TryGetValue(out TValue2 value)
        {
            // we never have this value already computed
            value = default(TValue2);
            return false;
        }

        public override TValue2 GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.computation(this.valueSource.GetValue(cancellationToken), cancellationToken);
        }

        public override Task<TValue2> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // get the text async and then parse it.
            return this.valueSource.GetValueAsync(cancellationToken).SafeContinueWith(value1Task =>
            {
                var value1 = value1Task.Result;
                return this.computation(value1, cancellationToken);
            }, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
}