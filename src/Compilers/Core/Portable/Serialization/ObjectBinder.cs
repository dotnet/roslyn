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
    internal static class ObjectBinder
    {
#if false
        private readonly ConcurrentDictionary<Type, Func<ObjectReader, object>> _readerMap =
            new ConcurrentDictionary<Type, Func<ObjectReader, object>>(concurrencyLevel: 2, capacity: 64);

        private readonly object _gate = new object();
        private readonly Dictionary<Type, int> _typeToIndex = new Dictionary<Type, int>();
        private readonly List<Type> _types = new List<Type>();

        public bool TryGetReader(Type type, out Func<ObjectReader, object> reader)
            => _readerMap.TryGetValue(type, out reader);

        private void RecordReader(object instance)
        {
            if (instance != null)
            {
                var type = instance.GetType();
                var typeKey = GetOrCreateTypeId(type);

                if (!_readerMap.ContainsKey(type))
                {
                    var readable = instance as IObjectReadable;
                    _readerMap.TryAdd(type, readable?.GetReader());
                }
            }
        }
        
        public int GetOrCreateTypeId(Type type)
        {
            lock (_gate)
            {
                if (!_typeToIndex.TryGetValue(type, out var index))
                {
                    index = _types.Count;
                    _types.Add(type);
                }

                return index;
            }
        }

        public Type GetType(int index)
        {
            lock (_gate)
            {
                return _types[index];
            }
        }

        private static readonly Action<ObjectWriter, object> s_writer
            = (w, i) => ((IObjectWritable)i).WriteTo(w);

        /// <summary>
        /// Gets a function that writes an object's members to a <see cref="ObjectWriter"/>.
        /// Returns false if the type cannot be serialized.
        /// </summary>
        public bool TryGetWriter(object instance, out Action<ObjectWriter, object> writer)
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
#endif
        private static object s_gate = new object();
        private static readonly Dictionary<Type, int> s_typeToIndex = new Dictionary<Type, int>();
        private static readonly List<Type> s_types;

        public static int GetTypeId(Type type)
        {
            lock (s_gate)
            {
                if (!s_typeToIndex.TryGetValue(type, out var index))
                {
                    index = s_types.Count;
                    s_types.Add(type);
                    s_typeToIndex.Add(type, index);
                }

                return index;
            }
        }

        public static Type GetTypeFromId(int typeId)
        {
            lock (s_gate)
            {
                return s_types[typeId];
            }
        }

        public static Func<ObjectReader, object> GetTypeReader(Type type)
        {
            throw new NotImplementedException();
        }
    }
}