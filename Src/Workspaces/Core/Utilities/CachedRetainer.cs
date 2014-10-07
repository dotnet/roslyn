using System;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This retainer adds caching behavior to another retainer. 
    /// </summary>
    /// <remarks>Whenever a value is accessed it is added to a cache.</remarks>
    internal class CachedRetainer<T> : Retainer<T> where T : class
    {
        private readonly Retainer<T> retainer;
        private readonly ICache<T> cache;

        public CachedRetainer(Retainer<T> retainer, ICache<T> cache)
        {
            this.retainer = retainer;
            this.cache = cache;
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            var value = this.retainer.GetValue(cancellationToken);
            if (value != null)
            {
                this.cache.Add(value);
            }

            return value;
        }

        public override bool TryGetValue(out T value)
        {
            if (this.retainer.TryGetValue(out value))
            {
                if (value != null)
                {
                    this.cache.Add(value);
                }

                return true;
            }

            return false;
        }
    }
}