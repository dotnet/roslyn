using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Services.Shared.Utilities
{
    internal class IdentityEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly IEqualityComparer<T> Instance = new IdentityEqualityComparer<T>();

        private IdentityEqualityComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            return x == y;
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
