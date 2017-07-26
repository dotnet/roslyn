// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents the common, language-agnostic elements of a conversion.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public struct CommonConversion
    {
        internal CommonConversion(bool exists, bool isIdentity, bool isNumeric, bool isReference, bool isUserDefined, IMethodSymbol methodSymbol)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            IsNumeric = isNumeric;
            IsReference = isReference;
            IsUserDefined = isUserDefined;
            MethodSymbol = methodSymbol;
        }

        /// <summary>
        /// Returns true if the conversion exists, as defined by the target language.
        /// </summary>
        /// <remarks>
        /// The existence of a conversion does not necessarily imply that the conversion is valid.
        /// For example, an ambiguous user-defined conversion may exist but may not be valid.
        /// </remarks>
        public bool Exists { get; }
        /// <summary>
        /// Returns true if the conversion is an identity conversion.
        /// </summary>
        public bool IsIdentity { get; }
        /// <summary>
        /// Returns true if the conversion is a numeric conversion.
        /// </summary>
        public bool IsNumeric { get; }
        /// <summary>
        /// Returns true if the conversion is a reference conversion.
        /// </summary>
        public bool IsReference { get; }
        /// <summary>
        /// Returns true if the conversion is a user-defined conversion.
        /// </summary>
        public bool IsUserDefined { get; }
        /// <summary>
        /// Returns the method used to perform the conversion for a user-defined conversion if <see cref="IsUserDefined"/> is true.
        /// Otherwise, returns null.
        /// </summary>
        public IMethodSymbol MethodSymbol { get; }
    }
}
