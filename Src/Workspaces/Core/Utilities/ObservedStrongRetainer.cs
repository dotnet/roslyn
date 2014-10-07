using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This retainer as strong retention on top of another retainer.
    /// </summary>
    /// <remarks>When a value is accessed/observed it a strong reference is held.</remarks>
    [ExcludeFromCodeCoverage]
    internal class ObservedStrongRetainer<T> : Retainer<T>
    {
        private readonly Retainer<T> retainer;
        private T observedValue;

        [Obsolete]
        public ObservedStrongRetainer(Retainer<T> retainer)
        {
            this.retainer = retainer;
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = this.retainer.GetValue(cancellationToken);
            this.observedValue = result;
            return result;
        }

        public override bool TryGetValue(out T value)
        {
            if (this.retainer.TryGetValue(out value))
            {
                this.observedValue = value;
                return true;
            }

            return false;
        }
    }
}