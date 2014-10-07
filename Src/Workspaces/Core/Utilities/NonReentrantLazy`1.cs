using System;
using System.Threading;

namespace Roslyn.Utilities
{
    internal class NonReentrantLazy<T>
    {
        private NonReentrantLock gate;
        private Func<T> valueFactory;
        private T value;

        public NonReentrantLazy(Func<T> valueFactory)
        {
            this.gate = new NonReentrantLock();
            this.valueFactory = valueFactory;
        }

        public NonReentrantLazy(T value)
        {
            this.value = value;
        }

        public T Value
        {
            get
            {
                var gate = this.gate;
                if (gate != null)
                {
                    using (gate.DisposableWait(CancellationToken.None))
                    {
                        if (this.valueFactory != null)
                        {
                            this.value = this.valueFactory();
                            Interlocked.Exchange(ref this.valueFactory, null);
                        }

                        Interlocked.Exchange(ref this.gate, null);
                    }
                }

                return this.value;
            }
        }

        public bool HasValue
        {
            get
            {
                return this.gate == null;
            }
        }
    }
}
