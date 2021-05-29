// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An IErrorTypeSymbol is used when the compiler cannot determine a symbol object to return because
    /// of an error. For example, if a field is declared "Goo x;", and the type "Goo" cannot be
    /// found, an IErrorTypeSymbol is returned when asking the field "x" what it's type is.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IErrorTypeSymbol : INamedTypeSymbol
    {
        /// <summary>
        /// When constructing this type, there may have been symbols that seemed to
        /// be what the user intended, but were unsuitable. For example, a type might have been
        /// inaccessible, or ambiguous. This property returns the possible symbols that the user
        /// might have intended. It will return no symbols if no possible symbols were found.
        /// See the CandidateReason property to understand why the symbols were unsuitable.
        /// </summary>
        /// <remarks>
        /// This only applies if this INamedTypeSymbol has TypeKind TypeKind.Error.
        /// If not, an empty ImmutableArray is returned.
        /// </remarks>
        ImmutableArray<ISymbol> CandidateSymbols { get; }

        ///<summary>
        /// If CandidateSymbols returns one or more symbols, returns the reason that those
        /// symbols were not chosen. Otherwise, returns None.
        /// </summary>
        CandidateReason CandidateReason { get; }
    }
}
