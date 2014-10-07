using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities
{
    [ExcludeFromCodeCoverage]
    internal class CancellableFuture<TArg, T>
    {
        private NonReentrantLock gate;
        private Func<TArg, CancellationToken, T> valueFactory;
        private T value;

        [Obsolete]
        public CancellableFuture(Func<TArg, CancellationToken, T> valueFactory)
        {
            this.gate = new NonReentrantLock();
            this.valueFactory = valueFactory;
        }

        public CancellableFuture(T value)
        {
            this.value = value;
        }

        public T GetValue(TArg arg, CancellationToken cancellationToken)
        {
            var gate = this.gate;
            if (gate != null)
            {
                using (gate.DisposableWait(cancellationToken))
                {
                    if (this.valueFactory != null)
                    {
                        this.value = this.valueFactory(arg, cancellationToken);
                        Interlocked.Exchange(ref this.valueFactory, null);
                    }

                    Interlocked.Exchange(ref this.gate, null);
                }
            }

            return this.value;
        }

        public bool HasValue
        {
            get { return this.gate == null; }
        }
    }
}
