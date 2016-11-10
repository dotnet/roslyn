// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A binder that gathers type/reader mappings during object writing
    /// </summary>
    internal sealed class SimpleRecordingObjectBinder : RecordingObjectBinder
    {
        private readonly Dictionary<TypeKey, Type> _typeMap =
            new Dictionary<TypeKey, Type>();

        private readonly Dictionary<Type, Func<ObjectReader, object>> _readerMap =
            new Dictionary<Type, Func<ObjectReader, object>>();

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
                if (!_typeMap.ContainsKey(key))
                {
                    _typeMap.Add(key, type);
                }
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
                        Debug.Assert(_typeMap.ContainsKey(new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName)));
                        return;
                    }

                    if (!_readerMap.ContainsKey(type))
                    {
                        _readerMap.Add(type, readable.GetReader());
                    }
                }

                Record(type);
            }
        }
    }
}
