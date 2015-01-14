// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    internal abstract class ObjectBinder
    {
        public abstract Type GetType(string assemblyName, string typeName);
        public abstract Func<ObjectReader, object> GetReader(Type type);

        internal struct TypeKey : IEquatable<TypeKey>
        {
            internal readonly string AssemblyName;
            internal readonly string TypeName;

            public TypeKey(string assemblyName, string typeName)
            {
                this.AssemblyName = assemblyName;
                this.TypeName = typeName;
            }

            public bool Equals(TypeKey other)
            {
                return this.AssemblyName == other.AssemblyName
                    && this.TypeName == other.TypeName;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeKey && this.Equals((TypeKey)obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.AssemblyName.GetHashCode(), this.TypeName.GetHashCode());
            }
        }
    }
}
