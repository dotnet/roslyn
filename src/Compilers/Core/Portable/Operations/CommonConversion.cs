// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents the common, language-agnostic elements of a conversion.
    /// </summary>
    /// <remarks>
    /// We reserve the right to change this struct in the future.
    /// </remarks>
    public readonly struct CommonConversion
    {
        [Flags]
        private enum ConversionKind
        {
            None = 0,
            Exists = 1 << 0,
            IsIdentity = 1 << 1,
            IsNumeric = 1 << 2,
            IsReference = 1 << 3,
            IsImplicit = 1 << 4,
            IsNullable = 1 << 5,
        }

        private readonly ConversionKind _conversionKind;

        internal CommonConversion(bool exists, bool isIdentity, bool isNumeric, bool isReference, bool isImplicit, bool isNullable, IMethodSymbol? methodSymbol, ITypeSymbol? constrainedToType)
        {
            _conversionKind = (exists ? ConversionKind.Exists : ConversionKind.None) |
                              (isIdentity ? ConversionKind.IsIdentity : ConversionKind.None) |
                              (isNumeric ? ConversionKind.IsNumeric : ConversionKind.None) |
                              (isReference ? ConversionKind.IsReference : ConversionKind.None) |
                              (isImplicit ? ConversionKind.IsImplicit : ConversionKind.None) |
                              (isNullable ? ConversionKind.IsNullable : ConversionKind.None);
            MethodSymbol = methodSymbol;
            ConstrainedToType = constrainedToType;
        }

        /// <summary>
        /// Returns true if the conversion exists, as defined by the target language.
        /// </summary>
        /// <remarks>
        /// The existence of a conversion does not necessarily imply that the conversion is valid.
        /// For example, an ambiguous user-defined conversion may exist but may not be valid.
        /// </remarks>
        public bool Exists => (_conversionKind & ConversionKind.Exists) == ConversionKind.Exists;
        /// <summary>
        /// Returns true if the conversion is an identity conversion.
        /// </summary>
        public bool IsIdentity => (_conversionKind & ConversionKind.IsIdentity) == ConversionKind.IsIdentity;
        /// <summary>
        /// Returns true if the conversion is an nullable conversion.
        /// </summary>
        public bool IsNullable => (_conversionKind & ConversionKind.IsNullable) == ConversionKind.IsNullable;
        /// <summary>
        /// Returns true if the conversion is a numeric conversion.
        /// </summary>
        public bool IsNumeric => (_conversionKind & ConversionKind.IsNumeric) == ConversionKind.IsNumeric;
        /// <summary>
        /// Returns true if the conversion is a reference conversion.
        /// </summary>
        public bool IsReference => (_conversionKind & ConversionKind.IsReference) == ConversionKind.IsReference;
        /// <summary>
        /// Returns true if the conversion is an implicit (C#) or widening (VB) conversion.
        /// </summary>
        public bool IsImplicit => (_conversionKind & ConversionKind.IsImplicit) == ConversionKind.IsImplicit;
        /// <summary>
        /// Returns true if the conversion is a user-defined conversion.
        /// </summary>
        [MemberNotNullWhen(true, nameof(MethodSymbol))]
        public bool IsUserDefined => MethodSymbol is { MethodKind: MethodKind.Conversion };
        /// <summary>
        /// Returns true if the conversion is a union conversion.
        /// </summary>
        [MemberNotNullWhen(true, nameof(MethodSymbol))]
        public bool IsUnion => MethodSymbol is { MethodKind: MethodKind.Constructor };
        /// <summary>
        /// Returns the method used to perform the conversion for a user-defined conversion if <see cref="IsUserDefined"/> is true,
        /// or to perform the conversion for union conversion if <see cref="IsUnion"/> is true.
        /// Otherwise, returns null.
        /// </summary>
        public IMethodSymbol? MethodSymbol { get; }
        /// <summary>
        /// Type parameter which runtime type will be used to resolve virtual invocation of the <see cref="MethodSymbol" />, if any.
        /// Null if <see cref="MethodSymbol" /> is resolved statically, or is null.
        /// </summary>
        public ITypeSymbol? ConstrainedToType { get; }
    }
}
