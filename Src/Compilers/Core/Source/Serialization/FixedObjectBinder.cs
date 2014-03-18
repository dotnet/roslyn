// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A binder that used a predetermine list of types and reader functions.
    /// </summary>
    internal class FixedObjectBinder : ObjectBinder
    {
        private readonly ImmutableDictionary<TypeKey, Type> typeMap;
        private readonly ImmutableDictionary<Type, Func<ObjectReader, object>> readerMap;

        public FixedObjectBinder(ImmutableDictionary<Type, Func<ObjectReader, object>> readerMap)
        {
            this.readerMap = readerMap;
            this.typeMap = readerMap.Keys.ToImmutableDictionary(t => new TypeKey(t.GetTypeInfo().Assembly.FullName, t.FullName));
        }

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
    }
}
