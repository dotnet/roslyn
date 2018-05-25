// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents the common, language-agnostic elements of a conversion.
    /// </summary>
    /// <remarks>
    /// We reserve the right to change this struct in the future.
    /// </remarks>
    public struct CommonConversion
    {
        [Flags]
        private enum ConversionKind
        {
            None = 0b0000,
            Exists = 0b0001,
            IsIdentity = 0b0010,
            IsNumeric = 0b0100,
            IsReference = 0b1000
        }

        private readonly ConversionKind _conversionKind;

        internal CommonConversion(bool exists, bool isIdentity, bool isNumeric, bool isReference, IMethodSymbol methodSymbol)
        {
            _conversionKind = (exists ? ConversionKind.Exists : ConversionKind.None) |
                              (isIdentity ? ConversionKind.IsIdentity : ConversionKind.None) |
                              (isNumeric ? ConversionKind.IsNumeric : ConversionKind.None) |
                              (isReference ? ConversionKind.IsReference : ConversionKind.None);
            MethodSymbol = methodSymbol;
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
        /// Returns true if the conversion is a numeric conversion.
        /// </summary>
        public bool IsNumeric => (_conversionKind & ConversionKind.IsNumeric) == ConversionKind.IsNumeric;
        /// <summary>
        /// Returns true if the conversion is a reference conversion.
        /// </summary>
        public bool IsReference => (_conversionKind & ConversionKind.IsReference) == ConversionKind.IsReference;
        /// <summary>
        /// Returns true if the conversion is a user-defined conversion.
        /// </summary>
        public bool IsUserDefined => MethodSymbol != null;
        /// <summary>
        /// Returns the method used to perform the conversion for a user-defined conversion if <see cref="IsUserDefined"/> is true.
        /// Otherwise, returns null.
        /// </summary>
        public IMethodSymbol MethodSymbol { get; }
    }
}
