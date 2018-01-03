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
        /// Symbol declared by the pattern.
        /// </summary>
        ISymbol DeclaredSymbol { get; }

        // PROTOTYPE(patterns2): Need to define what else is needed for IOperation.
    }
}

