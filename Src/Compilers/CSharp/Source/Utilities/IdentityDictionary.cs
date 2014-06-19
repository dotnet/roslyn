using System.Collections.Generic;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal sealed class IdentityDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        where TKey : class
    {
        public IdentityDictionary()
            : base(IdentityComparer.Instance)
        {
        }
    }
}
