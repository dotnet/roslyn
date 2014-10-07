using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A retainer that computes its value from a computation function.
    /// The value is not stored.
    /// </summary>
    internal class ComputedValueSource<TValue> : ValueSource<TValue>
    {
        private readonly Func<CancellationToken, TValue> computation;

        public ComputedValueSource(Func<CancellationToken, TValue> computation)
        {
            this.computation = computation;
        }

        public override bool TryGetValue(out TValue value)
        {
            value = default(TValue);
            return false;
        }

        public override TValue GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.computation(cancellationToken);
        }

        public override Task<TValue> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Factory.StartNew(() => this.computation(cancellationToken), cancellationToken);
        }
    }
}