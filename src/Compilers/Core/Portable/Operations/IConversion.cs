// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
                               // These crefs come from conversions defined in the C# and VB specific projects
    /// <summary>
    /// Represents a conversion that is convertible to a language-specific conversion. This can be either
    /// a <see cref="Microsoft.CodeAnalysis.CSharp.Conversion"/> or
    /// <see cref="Microsoft.CodeAnalysis.VisualBasic.Conversion"/>.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    public interface IConversion
    {
        /// <summary>
        /// Returns true if the conversion exists, as defined by the target language.
        /// </summary>
        /// <remarks>
        /// The existence of a conversion does not necessarily imply that the conversion is valid.
        /// For example, an ambiguous user-defined conversion may exist but may not be valid.
        /// </remarks>
        bool Exists { get; }
        /// <summary>
        /// Returns true if the conversion is an identity conversion.
        /// </summary>
        bool IsIdentity { get; }
        /// <summary>
        /// Returns true if the conversion is a numeric conversion.
        /// </summary>
        bool IsNumeric { get; }
        /// <summary>
        /// Returns true if the conversion is a reference conversion.
        /// </summary>
        bool IsReference { get; }
        /// <summary>
        /// Returns true if the conversion is a user-defined conversion.
        /// </summary>
        bool IsUserDefined { get; }
        /// <summary>
        /// Returns the method used to perform the conversion for a user-defined conversion if <see cref="IsUserDefined"/> is true.
        /// Otherwise, returns null.
        /// </summary>
        IMethodSymbol MethodSymbol { get; }
    }
}
