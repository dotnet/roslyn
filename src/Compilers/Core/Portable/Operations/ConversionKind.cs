// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of conversions.
    /// </summary>
    public enum ConversionKind
    {
        None = 0x0,
        /// <summary>
        /// Conversion is defined by the underlying type system and throws an exception if it fails.
        /// </summary>
        Cast = 0x1,
        /// <summary>
        /// Conversion is defined by the underlying type system and produces a null result if it fails.
        /// </summary>
        TryCast = 0x2,
        /// <summary>
        /// Conversion has VB-specific semantics.
        /// </summary>
        Basic = 0x3,
        /// <summary>
        /// Conversion has C#-specific semantics.
        /// </summary>
        CSharp = 0x4,
        /// <summary>
        /// Conversion is implemented by a conversion operator method.
        /// </summary>
        OperatorMethod = 0x5,
        /// <summary>
        /// Conversion is invalid.
        /// </summary>
        Invalid = 0xf
    }
}

