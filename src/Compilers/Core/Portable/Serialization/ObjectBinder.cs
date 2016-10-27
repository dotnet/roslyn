// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A type that provides object and type encoding/decoding.
    /// </summary>
    internal abstract class ObjectBinder
    {
        /// <summary>
        /// Gets the <see cref="Type"/> corresponding to the specified <see cref="TypeKey"/>.
        /// Returns false if no type corresponds to the key.
        /// </summary>
        public abstract bool TryGetType(TypeKey key, out Type type);

        /// <summary>
        /// Gets the <see cref="TypeKey"/> for the specified <see cref="Type"/>.
        /// Returns false if the type cannot be serialized. 
        /// </summary>
        public virtual bool TryGetTypeKey(Type type, out TypeKey key)
        {
            key = new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName);
            return true;
        }

        /// <summary>
        /// Gets a function that reads an type's members from an <see cref="ObjectReader"/> and constructs an instance with those members.
        /// Returns false if the type cannot be deserialized.
        /// </summary>
        public abstract bool TryGetReader(Type type, out Func<ObjectReader, object> reader);

        /// <summary>
        /// Gets a function that writes an object's members to a <see cref="ObjectWriter"/>.
        /// Returns false if the type cannot be serialized.
        /// </summary>
        public virtual bool TryGetWriter(object instance, out Action<ObjectWriter, object> writer)
        {          
            if (instance is IObjectWritable)
            {
                writer = (w, i) => ((IObjectWritable)i).WriteTo(w); // static delegate should be cached
                return true;
            }
            else
            {
                writer = null;
                return false;
            }
        }
    }
}
