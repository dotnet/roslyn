// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A binder that gathers type/reader mappings during object writing
    /// </summary>
    internal class ConcurrentRecordingObjectBinder : RecordingObjectBinder
    {
        private readonly ConcurrentDictionary<TypeKey, Type> typeMap =
            new ConcurrentDictionary<TypeKey, Type>();

        private readonly ConcurrentDictionary<Type, Func<ObjectReader, object>> readerMap =
            new ConcurrentDictionary<Type, Func<ObjectReader, object>>();

        public override Type GetType(string assemblyName, string typeName)
        {
            Type type;
            this.typeMap.TryGetValue(new TypeKey(assemblyName, typeName), out type);
            return type;
        }

        public override Func<ObjectReader, object> GetReader(Type type)
        {
            Func<ObjectReader, object> reader;
            this.readerMap.TryGetValue(type, out reader);
            return reader;
        }

        private bool HasType(Type type)
        {
            return this.typeMap.ContainsKey(new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName));
        }

        private bool HasConstructor(Type type)
        {
            return this.readerMap.ContainsKey(type);
        }

        public override void Record(Type type)
        {
            if (type != null)
            {
                if (!HasType(type))
                {
                    var key = new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName);
                    this.typeMap.TryAdd(key, type);
                }
            }
        }

        public override void Record(object instance)
        {
            if (instance != null)
            {
                var type = instance.GetType();
                Record(type);

                var readable = instance as IObjectReadable;
                if (readable != null && !HasConstructor(type))
                {
                    this.readerMap.TryAdd(type, readable.GetReader());
                }
            }
        }
    }
}