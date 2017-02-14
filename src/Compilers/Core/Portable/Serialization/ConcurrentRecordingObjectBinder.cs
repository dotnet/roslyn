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
    internal sealed class ConcurrentRecordingObjectBinder : RecordingObjectBinder
    {
        private readonly ConcurrentDictionary<TypeKey, Type> _typeMap =
            new ConcurrentDictionary<TypeKey, Type>();

        private readonly ConcurrentDictionary<Type, Func<ObjectReader, object>> _readerMap =
            new ConcurrentDictionary<Type, Func<ObjectReader, object>>();

        public override Type GetType(string assemblyName, string typeName)
        {
            Type type;
            if (!_typeMap.TryGetValue(new TypeKey(assemblyName, typeName), out type))
            {
                Debug.Assert(false, assemblyName + "/" + typeName + " don't exist");
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

        public override void Record(Type type)
        {
            if (type != null)
            {
                var key = new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName);
                _typeMap.TryAdd(key, type);
            }
        }

        public override void Record(object instance)
        {
            if (instance != null)
            {
                var type = instance.GetType();

                var readable = instance as IObjectReadable;
                if (readable != null)
                {
                    if (HasConstructor(type))
                    {
                        return;
                    }

                    _readerMap.TryAdd(type, readable.GetReader());
                }

                Record(type);
            }
        }
    }
}
