// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Symbols
{
    /// <summary>
    /// Synthesized symbol that implements a method body feature (iterator, async, lambda, etc.)
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal interface ISynthesizedMethodBodyImplementationSymbol : ISymbolInternal
    {
        /// <summary>
        /// The symbol whose body lowering produced this synthesized symbol, 
        /// or null if the symbol is synthesized based on declaration.
        /// </summary>
        IMethodSymbolInternal? Method { get; }

        /// <summary>
        /// True if this symbol body needs to be updated when the <see cref="Method"/> body is updated.
        /// False if <see cref="Method"/> is null.
        /// </summary>
        bool HasMethodBodyDependency { get; }
    }
}
