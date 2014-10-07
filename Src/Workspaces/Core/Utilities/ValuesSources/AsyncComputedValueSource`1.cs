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
    internal class AsyncComputedValueSource<TValue> : ValueSource<TValue>
    {
        private readonly AsyncLazy<TValue> lazyComputation;

        public AsyncComputedValueSource(Func<CancellationToken, Task<TValue>> computation)
        {
            this.lazyComputation = new AsyncLazy<TValue>(computation, cacheResult: true);
        }

        public override bool TryGetValue(out TValue value)
        {
            return this.lazyComputation.TryGetValue(out value);
        }

        public override TValue GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.lazyComputation.GetValueAsync(cancellationToken).WaitAndGetResult(cancellationToken);
        }

        public override Task<TValue> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.lazyComputation.GetValueAsync(cancellationToken);
        }
    }
}