using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    internal class LoadedTextRetainer : ValueSource<TextAndVersion>
    {
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private TextLoader loader;
        private TextAndVersion value;

        public LoadedTextRetainer(TextLoader loader)
        {
            this.loader = loader;
        }

        public override TextAndVersion GetValue(CancellationToken cancellationToken)
        {
            using (this.gate.DisposableWait(cancellationToken))
            {
                if (this.value == null)
                {
                    var tav = this.loader.LoadTextAndVersion(cancellationToken);
                    Interlocked.CompareExchange(ref this.value, tav, null);
                }

                return this.value;
            }
        }

        public override bool TryGetValue(out TextAndVersion value)
        {
            if (this.value != null)
            {
                value = this.value;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public override async Task<TextAndVersion> GetValueAsync(CancellationToken cancellationToken)
        {
            if (this.value == null)
            {
                var textAndVersion = await this.loader.LoadTextAndVersionAsync(cancellationToken).ConfigureAwait(false);
                Interlocked.CompareExchange(ref this.value, textAndVersion, null);
            }

            return this.value;
        }
    }
}