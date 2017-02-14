// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="ObjectBinder"/> that records runtime types and object readers during object writing so they
    /// can be used to read back objects later.
    /// </summary>
    /// <remarks>
    /// This binder records runtime types an object readers as a way to avoid needing to describe all serialization types up front
    /// or using reflection to determine them on demand.
    /// </remarks>
    internal sealed class ObjectBinder
    {
        private readonly Dictionary<TypeKey, Type> _typeMap_mustLock = new Dictionary<TypeKey, Type>();

        private readonly ConcurrentDictionary<Type, Func<ObjectReader, object>> _readerMap =
            new ConcurrentDictionary<Type, Func<ObjectReader, object>>(concurrencyLevel: 2, capacity: 64);

        /// <summary>
        /// Gets the <see cref="Type"/> corresponding to the specified <see cref="TypeKey"/>.
        /// Returns false if no type corresponds to the key.
        /// </summary>
        public bool TryGetType(TypeKey key, out Type type)
        {
            lock (_typeMap_mustLock)
            {
                return _typeMap_mustLock.TryGetValue(key, out type);
            }
        }

        public bool TryGetReader(Type type, out Func<ObjectReader, object> reader)
            => _readerMap.TryGetValue(type, out reader);

        private void RecordType(Type type, TypeKey key)
        {
            if (type != null)
            {
                lock (_typeMap_mustLock)
                {
                    _typeMap_mustLock[key] = type;
                }
            }
        }

        private void RecordReader(object instance)
        {
            if (instance != null)
            {
                var type = instance.GetType();

                var key = GetAndRecordTypeKey(type);

                var readable = instance as IObjectReadable;
                if (readable != null)
                {
                    if (_readerMap.ContainsKey(type))
                    {
#if DEBUG
                        lock (_typeMap_mustLock)
                        {
                            Debug.Assert(_typeMap_mustLock.ContainsKey(key));
                        }
#endif
                    }
                    else
                    {
                        _readerMap.TryAdd(type, readable.GetReader());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="TypeKey"/> for the specified <see cref="Type"/>.
        /// </summary>
        public TypeKey GetAndRecordTypeKey(Type type)
        {
            var key = new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName); ;
            RecordType(type, key);
            return key;
        }

        private static readonly Action<ObjectWriter, object> s_writer
            = (w, i) => ((IObjectWritable)i).WriteTo(w);

        /// <summary>
        /// Gets a function that writes an object's members to a <see cref="ObjectWriter"/>.
        /// Returns false if the type cannot be serialized.
        /// </summary>
        public bool TryGetWriter(Object instance, out Action<ObjectWriter, object> writer)
        {
            RecordReader(instance);

            if (instance is IObjectWritable)
            {
                writer = s_writer;
                return true;
            }
            else
            {
                writer = null;
                return false;
            }
        }
    }
}