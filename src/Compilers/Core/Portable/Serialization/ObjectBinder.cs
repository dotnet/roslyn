// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A type that provides object and type encoding/decoding
    /// </summary>
    internal abstract class ObjectBinder
    {
        /// <summary>
        /// Gets the <see cref="Type"/> corresponding to the specified <see cref="TypeKey"/>.
        /// </summary>
        public abstract Type GetType(TypeKey key);

        /// <summary>
        /// Gets the <see cref="TypeKey"/> for the specified <see cref="Type"/>.
        /// </summary>
        public virtual TypeKey GetTypeKey(Type type)
        {
            return new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName);
        }

        /// <summary>
        /// Gets a function that reads an type's members from an <see cref="ObjectReader"/> and constructs an instance with those members.
        /// </summary>
        public abstract Func<ObjectReader, object> GetReader(Type type);

        /// <summary>
        /// Gets a function that writes an object's members to a <see cref="ObjectWriter"/>.
        /// </summary>
        public virtual Action<ObjectWriter, object> GetWriter(object instance)
        {          
            if (instance is IObjectWritable)
            {
                return (w, i) => ((IObjectWritable)i).WriteTo(w); // static delegate should be cached
            }
            else
            {
                return null;
            }
        }
    }
}
