using System;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal partial class ArrayBuilder<T>
    {
        public static ArrayBuilderDisposer GetInstance(int capacity, out ArrayBuilder<T> instance)
        {
            instance = GetInstance(capacity);
            return new ArrayBuilderDisposer(instance);
        }

        public static ArrayBuilderDisposer GetInstance(out ArrayBuilder<T> instance)
        {
            instance = GetInstance();
            return new ArrayBuilderDisposer(instance);
        }

        public static ArrayBuilderDisposer GetInstance(int capacity, T fillWithValue, out ArrayBuilder<T> instance)
        {
            instance = GetInstance(capacity, fillWithValue);
            return new ArrayBuilderDisposer(instance);
        }

        internal struct ArrayBuilderDisposer : IDisposable
        {
            private ArrayBuilder<T> _pooledObject;

            public ArrayBuilderDisposer(ArrayBuilder<T> instance)
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
