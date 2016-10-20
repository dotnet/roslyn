// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A binder that gathers type/reader mappings during object writing
    /// </summary>
    internal sealed class ConcurrentRecordingObjectBinder : ObjectBinder
    {
        private readonly ConcurrentDictionary<TypeKey, Type> _typeMap =
            new ConcurrentDictionary<TypeKey, Type>();

        private readonly ConcurrentDictionary<Type, Func<ObjectReader, object>> _readerMap =
            new ConcurrentDictionary<Type, Func<ObjectReader, object>>();

        public override Type GetType(TypeKey key)
        {
            Type type;
            if (!_typeMap.TryGetValue(key, out type))
            {
                Debug.Assert(false, key.AssemblyName + "/" + key.TypeName + " don't exist");
            }

            return type;
        }

        public override Func<ObjectReader, object> GetReader(Type type)
        {
            Func<ObjectReader, object> reader;
            if (!_readerMap.TryGetValue(type, out reader))
            {
                Debug.Assert(false, type.ToString() + " reader doesn't exist");
            }

            return reader;
        }

        private bool HasConstructor(Type type)
        {
            return _readerMap.ContainsKey(type);
        }

        public override TypeKey GetTypeKey(Type type)
        {
            var key = base.GetTypeKey(type);
            RecordType(type, key);
            return key;
        }

        public override Action<ObjectWriter, Object> GetWriter(Object instance)
        {
            RecordReader(instance);
            return base.GetWriter(instance);
        }

        private void RecordType(Type type, TypeKey key)
        {
            if (type != null)
            {
                _typeMap.TryAdd(key, type);
            }
        }

        private void RecordReader(object instance)
        {
            if (instance != null)
            {
                var type = instance.GetType();
                var key = GetTypeKey(type); // side-effect records type too

                var readable = instance as IObjectReadable;
                if (readable != null)
                {
                    if (HasConstructor(type))
                    {
                        Debug.Assert(_typeMap.ContainsKey(key));
                        return;
                    }

                    _readerMap.TryAdd(type, readable.GetReader());
                }
            }
        }
    }
}
