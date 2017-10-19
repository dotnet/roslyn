// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Symbol declared by the pattern.
        /// </summary>
        ISymbol DeclaredSymbol { get; }
    }
}

