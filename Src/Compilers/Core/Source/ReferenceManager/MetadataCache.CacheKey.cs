using System;

namespace Roslyn.Compilers
{
    partial class MetadataCache
    {
        /// <summary>
        /// Represents a key for a file in AssemblyManager's cache.
        /// </summary>
        internal abstract class CacheKey : IEquatable<CacheKey>
        {
            protected static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

            public abstract bool Equals(CacheKey other);
        }
    }
}