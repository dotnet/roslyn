// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal struct TypeWithModifiers : IEquatable<TypeWithModifiers>
    {
        public readonly TypeSymbol Type;
        public readonly ImmutableArray<CustomModifier> CustomModifiers;

        public TypeWithModifiers(TypeSymbol type, ImmutableArray<CustomModifier> customModifiers)
        {
            Debug.Assert((object)type != null);
            this.Type = type;
            this.CustomModifiers = customModifiers.NullToEmpty();
        }

        public TypeWithModifiers(TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            this.Type = type;
            this.CustomModifiers = ImmutableArray<CustomModifier>.Empty;
        }

#pragma warning disable 809
        [Obsolete("Use the strongly typed overload.", true)]
        public override bool Equals(object obj)
        {
            return (obj is TypeWithModifiers) && Equals((TypeWithModifiers)obj);
        }
#pragma warning restore 809

        public bool Equals(TypeWithModifiers other)
        {
            return Equals(other, ignoreDynamic: false);
        }

        public bool Equals(TypeWithModifiers other, bool ignoreDynamic)
        {
            return ((object)this.Type == null ? (object)other.Type == null : this.Type.Equals(other.Type, ignoreDynamic: ignoreDynamic)) &&
                   (this.CustomModifiers.IsDefault ?
                      other.CustomModifiers.IsDefault :
                      (!other.CustomModifiers.IsDefault && this.CustomModifiers.SequenceEqual(other.CustomModifiers)));
        }

        public static bool operator ==(TypeWithModifiers x, TypeWithModifiers y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(TypeWithModifiers x, TypeWithModifiers y)
        {
            return !x.Equals(y);
        }

        public override int GetHashCode()
        {
            return Roslyn.Utilities.Hash.Combine(this.Type, Roslyn.Utilities.Hash.CombineValues(this.CustomModifiers));
        }

        public bool Is(TypeSymbol other)
        {
            return this.Type == other && this.CustomModifiers.IsEmpty;
        }

        [Obsolete("Use Is method.", true)]
        public bool Equals(TypeSymbol other)
        {
            return this.Is(other);
        }

        /// <summary>
        /// Extract type under assumption that there should be no custom modifiers.
        /// The method asserts otherwise.
        /// </summary>
        public TypeSymbol AsTypeSymbolOnly()
        {
            Debug.Assert(this.CustomModifiers.IsEmpty);
            return this.Type;
        }

        public TypeWithModifiers SubstituteType(AbstractTypeMap typeMap)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = typeMap.SubstituteType(this.Type);
            if (!newTypeWithModifiers.Is(this.Type) || newCustomModifiers != this.CustomModifiers)
            {
                return new TypeWithModifiers(newTypeWithModifiers.Type, newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
            }
            else
            {
                return this; // substitution had no effect on the type or modifiers
            }
        }
    }
}