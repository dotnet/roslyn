// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a single variable declaration and initializer.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# variable declarator
    ///   (2) C# catch variable declaration
    ///   (3) VB single variable declaration
    ///   (4) VB catch variable declaration
    /// </para>
    /// <remarks>
    /// In VB, the initializer for this node is only ever used for explicit array bounds initializers.
    ///
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISingleVariableDeclarationOperation : IVariableDeclarationOperation
    {
        /// <summary>
        /// Symbol declared by this variable declaration
        /// </summary>
        ILocalSymbol Symbol { get; }
    }
}
