using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Roslyn.Utilities
{
    internal class SerializableDataStorage : MarshalByRefObject
    {
        private NonReentrantLock textGate = new NonReentrantLock();
        private int nextStorageHandle = 1;
        private Dictionary<int, SerializableData> map = new Dictionary<int, SerializableData>();

        public override object InitializeLifetimeService()
        {
            // don't let this object timeout
            return null;
        }

        public int Store(SerializableData data)
        {
            using (textGate.DisposableWait())
            {
                var handle = nextStorageHandle++;
                map.Add(handle, data);
                return handle;
            }
        }

        public SerializableData Retrieve(int storageHandle)
        {
            using (textGate.DisposableWait())
            {
                SerializableData data;
                map.TryGetValue(storageHandle, out data);
                return data;
            }
        }

        public void Delete(int storageHandle)
        {
            using (textGate.DisposableWait())
            {
                map.Remove(storageHandle);
            }
        }
    }
}