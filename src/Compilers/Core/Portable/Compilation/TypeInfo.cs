// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public struct TypeInfo : IEquatable<TypeInfo>
    {
        internal static readonly TypeInfo None = new TypeInfo(null, null);

        /// <summary>
        /// The type of the expression represented by the syntax node. For expressions that do not
        /// have a type, null is returned. If the type could not be determined due to an error, then
        /// an IErrorTypeSymbol is returned.
        /// </summary>
        public ITypeSymbol Type { get; }

        /// <summary>
        /// The inferred nullability of the expression represented by the syntax node.
        /// </summary>
        public Nullability Nullability { get; }

        /// <summary>
        /// The type of the expression after it has undergone an implicit conversion. If the type
        /// did not undergo an implicit conversion, returns the same as Type.
        /// </summary>
        public ITypeSymbol ConvertedType { get; }

        /// <summary>
        /// The nullability of the expression after conversions have been applied. For most expressions,
        /// this will be identical to <see cref="Nullability"/>, with the exception being user-defined
        /// conversions that have differing return and input nullability.
        /// </summary>
        public Nullability ConvertedNullability { get; }

        internal TypeInfo(ITypeSymbol type, ITypeSymbol convertedType, Nullability nullability = Nullability.NotComputed, Nullability convertedNullability = Nullability.NotComputed)
            : this()
        {
            this.Type = type;
            this.ConvertedType = convertedType;
            this.Nullability = nullability;
            this.ConvertedNullability = convertedNullability;
        }

        public bool Equals(TypeInfo other)
        {
            return object.Equals(this.Type, other.Type)
                && object.Equals(this.ConvertedType, other.ConvertedType);
        }

        public override bool Equals(object obj)
        {
            return obj is TypeInfo && this.Equals((TypeInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ConvertedType, Hash.Combine(this.Type, 0));
        }
    }
}
