// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct CSharpTypeInfo : IEquatable<CSharpTypeInfo>
    {
        internal static readonly CSharpTypeInfo None = new CSharpTypeInfo(type: null, convertedType: null, nullability: default, convertedNullability: default, Conversion.Identity);

        // should be best guess if there is one, or error type if none.
        /// <summary>
        /// The type of the expression represented by the syntax node. For expressions that do not
        /// have a type, null is returned. If the type could not be determined due to an error, than
        /// an object derived from ErrorTypeSymbol is returned.
        /// </summary>
        public readonly TypeSymbol Type;

        public readonly NullabilityInfo Nullability;

        /// <summary>
        /// The type of the expression after it has undergone an implicit conversion. If the type
        /// did not undergo an implicit conversion, returns the same as Type.
        /// </summary>
        public readonly TypeSymbol ConvertedType;

        public readonly NullabilityInfo ConvertedNullability;

        /// <summary>
        /// If the expression underwent an implicit conversion, return information about that
        /// conversion. Otherwise, returns an identity conversion.
        /// </summary>
        public readonly Conversion ImplicitConversion;

        internal CSharpTypeInfo(TypeSymbol type, TypeSymbol convertedType, NullabilityInfo nullability, NullabilityInfo convertedNullability, Conversion implicitConversion)
        {
            // When constructing the result for the Caas API, we expose the underlying symbols that
            // may have been hidden under error type, if the error type was immediate. We will
            // expose error types that were constructed, or type parameters of constructed types.
            this.Type = type.GetNonErrorGuess() ?? type;
            this.ConvertedType = convertedType.GetNonErrorGuess() ?? convertedType;
            this.Nullability = nullability;
            this.ConvertedNullability = convertedNullability;
            this.ImplicitConversion = implicitConversion;
        }

        public static implicit operator TypeInfo(CSharpTypeInfo info)
        {
            return new TypeInfo(info.Type?.GetITypeSymbol(info.Nullability.FlowState.ToAnnotation()), info.ConvertedType?.GetITypeSymbol(info.ConvertedNullability.FlowState.ToAnnotation()),
                                info.Nullability, info.ConvertedNullability);
        }

        public override bool Equals(object obj)
        {
            return obj is CSharpTypeInfo && Equals((CSharpTypeInfo)obj);
        }

        public bool Equals(CSharpTypeInfo other)
        {
            return this.ImplicitConversion.Equals(other.ImplicitConversion)
                && TypeSymbol.Equals(this.Type, other.Type, TypeCompareKind.ConsiderEverything2)
                && TypeSymbol.Equals(this.ConvertedType, other.ConvertedType, TypeCompareKind.ConsiderEverything2)
                && this.Nullability.Equals(other.Nullability)
                && this.ConvertedNullability.Equals(other.ConvertedNullability);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ConvertedType,
                                Hash.Combine(this.Type,
                                             Hash.Combine(this.Nullability.GetHashCode(),
                                                          Hash.Combine(this.ConvertedNullability.GetHashCode(),
                                                                       this.ImplicitConversion.GetHashCode()))));
        }
    }
}
