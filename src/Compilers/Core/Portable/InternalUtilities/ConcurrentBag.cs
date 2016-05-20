using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.InternalUtilities
{
    internal class ConcurrentBag<T>
        : System.Collections.Concurrent.ConcurrentBag<T>, ICollection<T>
    {
        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
    }
}
