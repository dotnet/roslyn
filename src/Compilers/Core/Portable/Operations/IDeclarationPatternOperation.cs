// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a pattern that declares a symbol.
    /// <para>
    /// Current usage:
    ///  (1) C# declaration pattern.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDeclarationPatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type explicitly specified, or null if it was inferred (e.g. using <code>var</code> in C#).
        /// </summary>
        ITypeSymbol MatchedType { get; }
        /// <summary>
        /// True if the pattern is of a form that accepts null.
        /// For example, in C# the pattern `var x` will match a null input,
        /// while the pattern `string x` will not.
        /// </summary>
        bool MatchesNull { get; }
        /// <summary>
        /// Symbol declared by the pattern, if any.
        /// </summary>
        ISymbol DeclaredSymbol { get; }
    }
}
