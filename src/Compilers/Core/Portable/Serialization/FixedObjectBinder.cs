// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    /// <summary>
    /// An <see cref="ObjectBinder"/> with a fixed set of type and reader mappings.
    /// </summary>
    internal class FixedObjectBinder : ObjectBinder
    {
        private readonly ImmutableDictionary<TypeKey, Type> _typeMap;
        private readonly ImmutableDictionary<Type, Func<ObjectReader, object>> _readerMap;

        public FixedObjectBinder(
            ImmutableDictionary<TypeKey, Type> typeMap,
            ImmutableDictionary<Type, Func<ObjectReader, object>> readerMap)
        {
            _typeMap = typeMap ?? ImmutableDictionary<TypeKey, Type>.Empty;
            _readerMap = readerMap ?? ImmutableDictionary<Type, Func<ObjectReader, object>>.Empty;
        }

        public static readonly FixedObjectBinder Empty = new FixedObjectBinder(null, null);

        public override bool TryGetType(TypeKey key, out Type type)
        {
            return _typeMap.TryGetValue(key, out type);
        }

        public override bool TryGetTypeKey(Type type, out TypeKey key)
        {
            // do not let types have keys that cannot be reverse mapped.
            return base.TryGetTypeKey(type, out key) && _typeMap.ContainsKey(key);
        }

        public override bool TryGetWriter(Object instance, out Action<ObjectWriter, Object> writer)
        {
            // don't let objects be written that do not have known readers.
            return base.TryGetWriter(instance, out writer) && _readerMap.ContainsKey(instance.GetType());
        }

        public override bool TryGetReader(Type type, out Func<ObjectReader, object> reader)
        {
            return _readerMap.TryGetValue(type, out reader);
        }
    }
}
