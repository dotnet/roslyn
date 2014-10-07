using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal class UnwrappedValueSource<T> : ValueSource<T>
    {
        private readonly IValueSource<IValueSource<T>> source;

        public UnwrappedValueSource(IValueSource<IValueSource<T>> source)
        {
            this.source = source;
        }

        public override bool TryGetValue(out T value)
        {
            value = default(T);
            IValueSource<T> valueSource;

            return this.source.TryGetValue(out valueSource)
                && valueSource.TryGetValue(out value);
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.source.GetValue(cancellationToken).GetValue(cancellationToken);
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.source.GetValueAsync(cancellationToken)
                .SafeContinueWith(task => task.Result.GetValueAsync(cancellationToken), cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default)
                .Unwrap();
        }
    }
}
