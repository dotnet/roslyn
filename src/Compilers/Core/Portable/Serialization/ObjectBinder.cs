// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A type that controls how types are found and objects are serialized.
    /// </summary>
    internal abstract class ObjectBinder
    {
        public abstract Type GetType(TypeKey key);

        public virtual TypeKey GetTypeKey(Type type)
        {
            return new TypeKey(type.GetTypeInfo().Assembly.FullName, type.FullName);
        }

        public abstract Func<ObjectReader, object> GetReader(Type type);

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
