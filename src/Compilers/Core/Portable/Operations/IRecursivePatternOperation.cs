// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// The (explicit or implicit) type accepted for the recursive pattern.
        /// </summary>
        ITypeSymbol MatchedType { get; }
        /// <summary>
        /// The Deconstruct symbol, if any, used for the deconstruction subpatterns.
        /// </summary>
        ISymbol DeconstructSymbol { get; }
        /// <summary>
        /// If there is a deconstruction or positional subpattern, this contains the patterns contained within it.
        /// If there is no deconstruction subpattern, this is a default immutable array.
        /// </summary>
        ImmutableArray<IPatternOperation> DeconstructionSubpatterns { get; }
        /// <summary>
        /// If there is a property subpattern, this contains the
        /// <see cref="ISymbol"/>/<see cref="IPatternOperation"/> pairs within it.
        /// If there is no property subpattern, this is a default immutable array.
        /// </summary>
        ImmutableArray<(ISymbol, IPatternOperation)> PropertySubpatterns { get; }
        /// <summary>
        /// Symbol declared by the pattern.
        /// </summary>
        ISymbol DeclaredSymbol { get; }
    }
}

