using System;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal partial class PooledHashSet<T>
    {
        public static PooledHashSetDisposer GetInstance(out PooledHashSet<T> instance)
        {
            instance = GetInstance();
            return new PooledHashSetDisposer(instance);
        }

        internal struct PooledHashSetDisposer : IDisposable
        {
            private PooledHashSet<T> _pooledObject;

            public PooledHashSetDisposer(PooledHashSet<T> instance)
            {
                _pooledObject = instance;
            }

            public void Dispose()
            {
                var pooledObject = _pooledObject;
                if (pooledObject != null)
                {
                    pooledObject.Free();
                    _pooledObject = null;
                }
            }
        }
    }
}
