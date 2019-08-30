// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public readonly struct TypeInfo : IEquatable<TypeInfo>
    {
        internal static readonly TypeInfo None = new TypeInfo(type: null, convertedType: null, nullability: default, convertedNullability: default);

        /// <summary>
        /// The type of the expression represented by the syntax node. For expressions that do not
        /// have a type, null is returned. If the type could not be determined due to an error, then
        /// an IErrorTypeSymbol is returned.
        /// </summary>
        public ITypeSymbol? Type { get; }

        /// <summary>
        /// The top-level nullability information of the expression represented by the syntax node.
        /// </summary>
        public NullabilityInfo Nullability { get; }

        /// <summary>
        /// The type of the expression after it has undergone an implicit conversion. If the type
        /// did not undergo an implicit conversion, returns the same as Type.
        /// </summary>
        public ITypeSymbol? ConvertedType { get; }

        /// <summary>
        /// The top-level nullability of the expression after it has undergone an implicit conversion.
        /// For most expressions, this will be the same as the type. It can change in situations such
        /// as implicit user-defined conversions that have a nullable return type.
        /// </summary>
        public NullabilityInfo ConvertedNullability { get; }

        internal TypeInfo(ITypeSymbol? type, ITypeSymbol? convertedType, NullabilityInfo nullability, NullabilityInfo convertedNullability)
            : this()
        {
            this.Type = type;
            this.Nullability = nullability;
            this.ConvertedType = convertedType;
            this.ConvertedNullability = convertedNullability;
        }

        public bool Equals(TypeInfo other)
        {
            return object.Equals(this.Type, other.Type)
                && object.Equals(this.ConvertedType, other.ConvertedType)
                && this.Nullability.Equals(other.Nullability)
                && this.ConvertedNullability.Equals(other.ConvertedNullability);
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeInfo && this.Equals((TypeInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ConvertedType,
                Hash.Combine(this.Type,
                Hash.Combine(this.Nullability.GetHashCode(),
                this.ConvertedNullability.GetHashCode())));
        }
    }
}
