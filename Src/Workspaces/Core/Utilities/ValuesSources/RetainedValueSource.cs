using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This retainer computes a access a value on demand from an underlying value source, likely computing the value,
    /// and then holds onto it strongly.
    /// </summary>
    internal class RetainedValueSource<T> : AbstractGuardedValueSource<T>
    {
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private IValueSource<T> valueSource;
        private T value;

        public RetainedValueSource(IValueSource<T> valueSource)
        {
            Contract.ThrowIfNull(valueSource);
            this.valueSource = valueSource;
        }

        public override bool TryGetValue(out T value)
        {
            if (this.valueSource == null)
            {
                value = this.value;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        protected override T GetGuardedValue(CancellationToken cancellationToken)
        {
            IValueSource<T> localValueSource;

            using (gate.DisposableWait(cancellationToken))
            {
                if (this.valueSource != null)
                {
                    localValueSource = valueSource;
                }
                else
                {
                    return this.value;
                }
            }

            return localValueSource.GetValue(cancellationToken);
        }

        protected override Task<T> GetGuardedValueAsync(CancellationToken cancellationToken)
        {
            using (gate.DisposableWait(cancellationToken))
            {
                if (this.valueSource != null)
                {
                    return this.valueSource.GetValueAsync(cancellationToken);
                }
                else
                {
                    return Task.FromResult(this.value);
                }
            }
        }

        protected override T TranslateGuardedValue(T value)
        {
            using (gate.DisposableWait())
            {
                if (this.valueSource != null)
                {
                    this.value = value;
                    this.valueSource = null;
                }

                return this.value;
            }
        }
    }
}