using System;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This retainer computes a value lazily and holds onto it strongly.
    /// </summary>
    internal class LazyRetainer<T> : Retainer<T> where T : class
    {
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private Func<CancellationToken, Services.Host.IRetainer<T>> computeRetainer;
        private Services.Host.IRetainer<T> retainer;

        public LazyRetainer(Func<CancellationToken, Services.Host.IRetainer<T>> computeRetainer)
        {
            this.computeRetainer = computeRetainer;
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (this.gate.DisposableWait(cancellationToken))
            {
                if (this.computeRetainer != null)
                {
                    this.retainer = this.computeRetainer(cancellationToken);
                    this.computeRetainer = null;
                }
            }

            return this.retainer.GetValue(cancellationToken);
        }

        public override bool TryGetValue(out T value)
        {
            if (this.retainer != null && this.retainer.TryGetValue(out value))
            {
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
    }
}