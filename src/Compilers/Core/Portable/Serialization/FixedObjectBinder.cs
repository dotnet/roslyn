// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    /// <summary>
    /// An <see cref="ObjectBinder"/> with a fixed set of type and reader mappings.
    /// </summary>
    internal class NullObjectBinder : ObjectBinder
    {
        private NullObjectBinder()
        {
        }

        public static readonly NullObjectBinder Empty = new NullObjectBinder();

        public override bool TryGetType(TypeKey key, out Type type)
        {
            type = null;
            return false;
        }

        public override bool TryGetTypeKey(Type type, out TypeKey key)
        {
            key = default(TypeKey);
            return false;
        }

        public override bool TryGetWriter(Object instance, out Action<ObjectWriter, Object> writer)
        {
            writer = null;
            return false;
        }

        public override bool TryGetReader(Type type, out Func<ObjectReader, object> reader)
        {
            reader = null;
            return false;
        }
    }
}