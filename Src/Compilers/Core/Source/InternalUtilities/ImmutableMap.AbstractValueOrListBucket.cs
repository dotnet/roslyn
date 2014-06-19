using System;

namespace System.Collections.Immutable
{
    internal partial class ImmutableDictionary<K, V>
    {
        private abstract class AbstractValueOrListBucket : AbstractBucket
        {
            internal int Hash { get; private set; }

            internal AbstractValueOrListBucket(int hash)
            {
                this.Hash = hash;
            }
        }
    }
}