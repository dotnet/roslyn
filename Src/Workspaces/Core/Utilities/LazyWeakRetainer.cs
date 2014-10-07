using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This retainer computes values lazily and holds onto them weakly. If the value is accessed
    /// after it has been reclaimed then it is computed again.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class LazyWeakRetainer<T> : Retainer<T> where T : class
    {
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private readonly Func<CancellationToken, T> computeValue;
        private WeakReference<T> weakValue;

        [Obsolete("This doesn't seem to be used anywhere. Please add tests if you use it.")]
        public LazyWeakRetainer(Func<CancellationToken, T> computeValue)
        {
            this.computeValue = computeValue;
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (this.gate.DisposableWait(cancellationToken))
            {
                if (this.weakValue != null)
                {
                    T value;
                    if (this.weakValue.TryGetTarget(out value))
                    {
                        return value;
                    }
                }

                var newValue = this.computeValue(cancellationToken);
                this.weakValue = new WeakReference<T>(newValue);
                return newValue;
            }
        }

        public override bool TryGetValue(out T value)
        {
            if (this.weakValue != null)
            {
                if (this.weakValue.TryGetTarget(out value))
                {
                    return true;
                }
            }

            value = default(T);
            return false;
        }
    }
}