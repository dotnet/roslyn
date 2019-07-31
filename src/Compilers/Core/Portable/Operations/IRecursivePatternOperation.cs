// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a C# recursive pattern.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IRecursivePatternOperation : IPatternOperation
    {
        /// <summary>
        /// The type accepted for the recursive pattern.
        /// </summary>
        ITypeSymbol MatchedType { get; }
        /// <summary>
        /// The symbol, if any, used for the fetching values for subpatterns. This is either a <code>Deconstruct</code>
        /// method, the type <code>System.Runtime.CompilerServices.ITuple</code>, or null (for example, in
        /// error cases or when matching a tuple type).
        /// </summary>
        ISymbol DeconstructSymbol { get; }
        /// <summary>
        /// This contains the patterns contained within a deconstruction or positional subpattern.
        /// </summary>
        ImmutableArray<IPatternOperation> DeconstructionSubpatterns { get; }
        /// <summary>
        /// This contains the (symbol, property) pairs within a property subpattern.
        /// </summary>
        ImmutableArray<IPropertySubpatternOperation> PropertySubpatterns { get; }
        /// <summary>
        /// Symbol declared by the pattern.
        /// </summary>
        ISymbol DeclaredSymbol { get; }
    }
}
