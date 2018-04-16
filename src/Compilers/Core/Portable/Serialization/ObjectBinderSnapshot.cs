// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal readonly struct ObjectBinderSnapshot
    {
        private readonly Dictionary<Type, int> _typeToIndex;
        private readonly ImmutableArray<Type> _types;
        private readonly ImmutableArray<Func<ObjectReader, IObjectWritable>> _typeReaders;

        public ObjectBinderSnapshot(
            Dictionary<Type, int> typeToIndex,
            List<Type> types,
            List<Func<ObjectReader, IObjectWritable>> typeReaders)
        {
            _typeToIndex = new Dictionary<Type, int>(typeToIndex);
            _types = types.ToImmutableArray();
            _typeReaders = typeReaders.ToImmutableArray();
        }

        public int GetTypeId(Type type)
            => _typeToIndex[type];

        public Type GetTypeFromId(int typeId)
            => _types[typeId];

        public Func<ObjectReader, IObjectWritable> GetTypeReaderFromId(int typeId)
            => _typeReaders[typeId];
    }
}
