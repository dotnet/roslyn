using System;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal partial class PooledStringBuilder
    {
        public static PooledStringBuilderDisposer GetInstance(out PooledStringBuilder instance)
        {
            instance = GetInstance();
            return new PooledStringBuilderDisposer(instance);
        }

        internal struct PooledStringBuilderDisposer : IDisposable
        {
            private PooledStringBuilder _pooledObject;

            public PooledStringBuilderDisposer(PooledStringBuilder instance)
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
