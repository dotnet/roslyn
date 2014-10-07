using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A value source that guards access to another value source so that
    /// only one computation happens at a time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class GuardedValueSource<T> : AbstractGuardedValueSource<T>
    {
        private readonly IValueSource<T> valueSource;

        public GuardedValueSource(IValueSource<T> valueSource)
        {
            this.valueSource = valueSource;
        }

        public override bool TryGetValue(out T value)
        {
            return this.valueSource.TryGetValue(out value);
        }

        protected override T GetGuardedValue(CancellationToken cancellationToken)
        {
            return this.valueSource.GetValue(cancellationToken);
        }

        protected override Task<T> GetGuardedValueAsync(CancellationToken cancellationToken)
        {
            return this.valueSource.GetValueAsync(cancellationToken);
        }
    }
}