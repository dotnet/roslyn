// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal struct ObjectBinderSnapshot
    {
        public readonly int Version;

        private readonly Dictionary<Type, int> _typeToIndex;
        private readonly ImmutableArray<Type> _types;
        private readonly ImmutableArray<Func<ObjectReader, object>> _typeReaders;

        public ObjectBinderSnapshot(
            int version,
            Dictionary<Type, int> typeToIndex,
            ImmutableArray<Type> types,
            ImmutableArray<Func<ObjectReader, object>> typeReaders)
        {
            Version = version;
            _typeToIndex = typeToIndex;
            _types = types;
            _typeReaders = typeReaders;
        }

        public int GetTypeId(Type type)
            => _typeToIndex[type];

        public Type GetTypeFromId(int typeId)
            => _types[typeId];

        public Func<ObjectReader, object> GetTypeReaderFromId(int typeId)
            => _typeReaders[typeId];
    }
}